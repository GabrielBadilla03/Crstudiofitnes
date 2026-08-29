using CrStudioFitnes.Data;
using CrStudioFitnes.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIGE.Helpers;
using System.Data;

namespace CrStudioFitnes.Controllers
{
    public class ApplicationUserController : Controller
    {
        private const string ROL_GESTOR_PAGOS = "Gestor de Pagos";
        private const string ROL_ADMIN = "Administrador";
        private const string ROL_ENTRENADOR = "Entrenador";
        private const string ROL_USUARIO = "Usuario";

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _db;

        public ApplicationUserController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext db,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _db = db;
            _roleManager = roleManager;
        }

        [Authorize(Roles = ROL_GESTOR_PAGOS + "," + ROL_ADMIN + "," + ROL_ENTRENADOR)]
        public async Task<IActionResult> Index(
            int? pageNumber,
            string? buscar,
            bool? soloActivos)
        {
            const int pageSize = 8;

            int page = pageNumber.GetValueOrDefault(1);
            if (page < 1)
                page = 1;

            var query = _userManager.Users.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                buscar = buscar.Trim();

                query = query.Where(u =>
                    u.Nombre.Contains(buscar)
                    || u.Apellidos.Contains(buscar)
                    || u.Cedula.Contains(buscar)
                    || (u.Email != null && u.Email.Contains(buscar))
                    || (u.PhoneNumber != null && u.PhoneNumber.Contains(buscar)));
            }

            if (soloActivos == true)
            {
                var now = DateTimeOffset.UtcNow;
                query = query.Where(u =>
                    u.LockoutEnd == null || u.LockoutEnd <= now);
            }

            query = query
                .OrderBy(u => u.Apellidos)
                .ThenBy(u => u.Nombre);

            ViewData["CurrentBuscar"] = buscar;
            ViewData["CurrentSoloActivos"] = soloActivos;

            var model = await PaginatedList<ApplicationUser>.CreateAsync(
                query,
                page,
                pageSize);

            return View(model);
        }

        [Authorize(Roles = ROL_GESTOR_PAGOS + "," + ROL_ADMIN + "," + ROL_ENTRENADOR)]
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return NotFound();

            var user = await _userManager.Users
                .AsNoTracking()
                .Include(u => u.PaquetesUsuario)
                    .ThenInclude(pu => pu.Paquete)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
                return NotFound();

            // Solo se ofrecen paquetes visibles para nuevas asignaciones.
            ViewBag.PaquetesDisponibles = await _db.Paquetes
                .AsNoTracking()
                .Where(p => p.Activo)
                .OrderBy(p => p.CantDias)
                .ThenBy(p => p.PagoPorUsuario > 0 ? p.PagoPorUsuario : p.Pago)
                .ToListAsync();

            var rolesUsuario = (await _userManager.GetRolesAsync(user)).ToList();

            var todosRoles = await _roleManager.Roles
                .AsNoTracking()
                .Select(r => r.Name!)
                .Where(n => n != null && n != "")
                .OrderBy(n => n)
                .ToListAsync();

            var rolesNoTiene = todosRoles
                .Except(rolesUsuario, StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x)
                .ToList();

            ViewBag.RolesUsuario = rolesUsuario;
            ViewBag.RolesNoTiene = rolesNoTiene;

            return View(user);
        }

        [Authorize(Roles = ROL_ADMIN)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GestionarRoles(
            string idUsuario,
            string? addRole,
            string? removeRole)
        {
            if (string.IsNullOrWhiteSpace(idUsuario))
                return NotFound();

            var user = await _userManager.FindByIdAsync(idUsuario);
            if (user == null)
                return NotFound();

            bool wantsAdd = !string.IsNullOrWhiteSpace(addRole);
            bool wantsRemove = !string.IsNullOrWhiteSpace(removeRole);

            if (wantsAdd == wantsRemove)
            {
                TempData["ErrorRoles"] =
                    "Debés seleccionar un rol para agregar O un rol para quitar (no ambos).";

                return RedirectToAction(
                    nameof(Details),
                    new { id = idUsuario });
            }

            if (wantsAdd)
            {
                addRole = addRole!.Trim();

                if (!await _roleManager.RoleExistsAsync(addRole))
                {
                    TempData["ErrorRoles"] = $"El rol '{addRole}' no existe.";
                    return RedirectToAction(nameof(Details), new { id = idUsuario });
                }

                if (!await _userManager.IsInRoleAsync(user, addRole))
                {
                    var result = await _userManager.AddToRoleAsync(user, addRole);

                    if (!result.Succeeded)
                    {
                        TempData["ErrorRoles"] =
                            string.Join(" | ", result.Errors.Select(e => e.Description));

                        return RedirectToAction(
                            nameof(Details),
                            new { id = idUsuario });
                    }
                }

                TempData["OkRoles"] = $"Rol agregado: {addRole}";
            }
            else
            {
                removeRole = removeRole!.Trim();

                if (!await _roleManager.RoleExistsAsync(removeRole))
                {
                    TempData["ErrorRoles"] = $"El rol '{removeRole}' no existe.";
                    return RedirectToAction(nameof(Details), new { id = idUsuario });
                }

                if (await _userManager.IsInRoleAsync(user, removeRole))
                {
                    var result = await _userManager.RemoveFromRoleAsync(user, removeRole);

                    if (!result.Succeeded)
                    {
                        TempData["ErrorRoles"] =
                            string.Join(" | ", result.Errors.Select(e => e.Description));

                        return RedirectToAction(
                            nameof(Details),
                            new { id = idUsuario });
                    }
                }

                TempData["OkRoles"] = $"Rol removido: {removeRole}";
            }

            return RedirectToAction(nameof(Details), new { id = idUsuario });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = ROL_GESTOR_PAGOS + "," + ROL_ADMIN + "," + ROL_ENTRENADOR)]
        public async Task<IActionResult> CambiarPaquete(
            string idUsuario,
            int idPaquete)
        {
            if (string.IsNullOrWhiteSpace(idUsuario) || idPaquete <= 0)
                return NotFound();

            var paquete = await _db.Paquetes
                .AsNoTracking()
                .FirstOrDefaultAsync(p =>
                    p.IdPaquete == idPaquete && p.Activo);

            if (paquete == null)
            {
                TempData["ErrorPaquete"] =
                    "El paquete seleccionado no existe o está inactivo.";

                return RedirectToAction(nameof(Details), new { id = idUsuario });
            }

            var usuarioExiste = await _db.Users
                .AsNoTracking()
                .AnyAsync(u => u.Id == idUsuario);

            if (!usuarioExiste)
                return NotFound();

            var pu = await _db.PaquetesUsuario
                .Where(x => x.IdUsuario == idUsuario)
                .OrderByDescending(x => x.FechaFin)
                .ThenByDescending(x => x.IdPaqueteUsuario)
                .FirstOrDefaultAsync();

            if (pu == null)
            {
                pu = new PaqueteUsuario
                {
                    IdUsuario = idUsuario,
                    IdPaquete = idPaquete,
                    CantLecciones = 0,
                    FechaInicio = DateTime.Today,
                    FechaFin = DateTime.Today
                };

                _db.PaquetesUsuario.Add(pu);
            }
            else
            {
                pu.IdPaquete = idPaquete;
            }

            await _db.SaveChangesAsync();

            TempData["OkPaquete"] =
                "Paquete cambiado. Las lecciones y fechas se aplicarán al registrar el pago.";

            return RedirectToAction(nameof(Details), new { id = idUsuario });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = ROL_GESTOR_PAGOS + "," + ROL_ADMIN)]
        public async Task<IActionResult> PagarPaquete(
            string idUsuario,
            string tipoPago,
            DateTime? fechaPago)
        {
            if (string.IsNullOrWhiteSpace(idUsuario))
                return NotFound();

            tipoPago = (tipoPago ?? string.Empty)
                .Trim()
                .ToUpperInvariant();

            if (tipoPago != "CONTADO" && tipoPago != "CREDITO")
            {
                TempData["ErrorPago"] =
                    "Debe seleccionar un tipo de pago válido: contado o crédito.";

                return RedirectToAction(nameof(Details), new { id = idUsuario });
            }

            if (!fechaPago.HasValue)
            {
                TempData["ErrorPago"] = "Debe seleccionar la fecha de pago.";
                return RedirectToAction(nameof(Details), new { id = idUsuario });
            }

            var fecha = fechaPago.Value.Date;
            var strategy = _db.Database.CreateExecutionStrategy();

            try
            {
                await strategy.ExecuteAsync(async () =>
                {
                    await using var tx = await _db.Database.BeginTransactionAsync(
                        IsolationLevel.Serializable);

                    var usuarioExiste = await _db.Users
                        .AsNoTracking()
                        .AnyAsync(u => u.Id == idUsuario);

                    if (!usuarioExiste)
                        throw new KeyNotFoundException("No se encontró el usuario.");

                    var pagoPendiente = await _db.PagosPaquete
                        .AsNoTracking()
                        .Where(p =>
                            p.IdUsuario == idUsuario
                            && p.Activo
                            && p.Monto > 0)
                        .OrderByDescending(p => p.Fecha)
                        .ThenByDescending(p => p.IdPagoPaquete)
                        .FirstOrDefaultAsync();

                    if (pagoPendiente != null)
                    {
                        throw new InvalidOperationException(
                            "Este usuario tiene un pago pendiente. " +
                            "No se puede registrar otro pago hasta cancelar la deuda actual.");
                    }

                    var pu = await _db.PaquetesUsuario
                        .Include(x => x.Paquete)
                        .Where(x => x.IdUsuario == idUsuario)
                        .OrderByDescending(x => x.FechaFin)
                        .ThenByDescending(x => x.IdPaqueteUsuario)
                        .FirstOrDefaultAsync();

                    if (pu == null || pu.Paquete == null)
                    {
                        throw new InvalidOperationException(
                            "No se puede registrar el pago porque el usuario no tiene un paquete asignado.");
                    }

                    if (!pu.Paquete.Activo)
                    {
                        throw new InvalidOperationException(
                            "El paquete asignado está inactivo. Seleccione un paquete activo antes de registrar el pago.");
                    }

                    var p = pu.Paquete;

                    var montoOriginalPaquete = p.PagoPorUsuario > 0
                        ? p.PagoPorUsuario
                        : p.Pago;

                    var leccionesUsuario = p.CantLeccionesPorUsuario > 0
                        ? p.CantLeccionesPorUsuario
                        : p.CantLecciones;

                    if (montoOriginalPaquete <= 0)
                        throw new InvalidOperationException(
                            "El monto del paquete debe ser mayor a 0.");

                    if (leccionesUsuario <= 0)
                        throw new InvalidOperationException(
                            "La cantidad de lecciones del paquete debe ser mayor a 0.");

                    var esContado = tipoPago == "CONTADO";

                    var pago = new PagoPaquete
                    {
                        IdUsuario = idUsuario,
                        Fecha = fecha,
                        TipoPago = tipoPago,
                        Activo = true,
                        MotivoAnulacion = null,
                        Monto = esContado ? 0m : montoOriginalPaquete
                    };

                    pago.Detalles.Add(new PagoPaqueteDetalle
                    {
                        CantDias = p.CantDias,
                        CantLecciones = leccionesUsuario,
                        Pago = montoOriginalPaquete,
                        Detalle = p.Detalle
                    });

                    if (esContado)
                    {
                        pago.Abonos.Add(new PagoPaqueteAbono
                        {
                            Fecha = fecha,
                            Monto = montoOriginalPaquete
                        });
                    }

                    _db.PagosPaquete.Add(pago);

                    pu.CantLecciones = leccionesUsuario;
                    pu.FechaInicio = fecha;
                    pu.FechaFin = CalcularFechaFin(fecha, p.CantDias);

                    await _db.SaveChangesAsync();
                    await tx.CommitAsync();
                });
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorPago"] = ex.Message;
                return RedirectToAction(nameof(Details), new { id = idUsuario });
            }
            catch
            {
                TempData["ErrorPago"] =
                    "Ocurrió un error registrando el pago. No se aplicaron cambios parciales.";

                return RedirectToAction(nameof(Details), new { id = idUsuario });
            }

            TempData["OkPago"] = tipoPago == "CONTADO"
                ? "Pago contado registrado correctamente. Se agregó un abono por el total del paquete."
                : "Pago a crédito registrado correctamente.";

            return RedirectToAction(nameof(Details), new { id = idUsuario });
        }

        private static DateTime CalcularFechaFin(
            DateTime fechaPago,
            TipoPlanDias tipo)
        {
            fechaPago = fechaPago.Date;

            return tipo switch
            {
                TipoPlanDias.Diario => fechaPago.AddDays(1),
                TipoPlanDias.Semanal => fechaPago.AddDays(7),
                TipoPlanDias.Quincenal => SumarDiasSinContarDia31(fechaPago, 15),
                TipoPlanDias.Mensual => fechaPago.AddMonths(1),
                _ => fechaPago
            };
        }

        private static DateTime SumarDiasSinContarDia31(
            DateTime fechaPago,
            int dias)
        {
            var resultado = fechaPago.Date;
            var diasSumados = 0;

            while (diasSumados < dias)
            {
                resultado = resultado.AddDays(1);

                if (resultado.Day == 31)
                    continue;

                diasSumados++;
            }

            return resultado;
        }

        [Authorize(Roles = ROL_GESTOR_PAGOS + "," + ROL_ADMIN + "," + ROL_USUARIO)]
        public async Task<IActionResult> HistorialPagos(string id)
        {
            var currentUserId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(currentUserId))
                return Challenge();

            bool puedeVerOtros =
                User.IsInRole(ROL_ADMIN)
                || User.IsInRole(ROL_GESTOR_PAGOS);

            if (!puedeVerOtros)
            {
                if (!string.IsNullOrWhiteSpace(id)
                    && !string.Equals(
                        id,
                        currentUserId,
                        StringComparison.Ordinal))
                {
                    return Forbid();
                }

                id = currentUserId;
            }

            if (string.IsNullOrWhiteSpace(id))
                return NotFound();

            var user = await _userManager.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
                return NotFound();

            ViewData["UsuarioNombre"] =
                $"{user.Nombre} {user.Apellidos}".Trim();

            ViewData["UsuarioId"] = user.Id;

            var pagos = await _db.PagosPaquete
                .AsNoTracking()
                .Where(p => p.IdUsuario == id)
                .Include(p => p.Usuario)
                .Include(p => p.Detalles)
                .Include(p => p.Abonos)
                .OrderByDescending(p => p.Fecha)
                .ThenByDescending(p => p.IdPagoPaquete)
                .ToListAsync();

            return View(pagos);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = ROL_GESTOR_PAGOS + "," + ROL_ADMIN)]
        public async Task<IActionResult> AgregarAbonoPaquete(
            int idPagoPaquete,
            DateTime? fechaAbono,
            decimal montoAbono)
        {
            if (idPagoPaquete <= 0)
                return NotFound();

            if (!fechaAbono.HasValue)
            {
                TempData["ErrorAbono"] = "Debe seleccionar la fecha del abono.";
                return RedirectToAction(nameof(Index));
            }

            if (montoAbono <= 0)
            {
                TempData["ErrorAbono"] = "El monto del abono debe ser mayor a 0.";
                return RedirectToAction(nameof(Index));
            }

            string? idUsuario = null;
            decimal saldoFinal = 0;
            var strategy = _db.Database.CreateExecutionStrategy();

            try
            {
                await strategy.ExecuteAsync(async () =>
                {
                    await using var tx = await _db.Database.BeginTransactionAsync(
                        IsolationLevel.Serializable);

                    var pago = await _db.PagosPaquete
                        .Include(p => p.Abonos)
                        .FirstOrDefaultAsync(
                            p => p.IdPagoPaquete == idPagoPaquete);

                    if (pago == null)
                        throw new KeyNotFoundException();

                    idUsuario = pago.IdUsuario;

                    if (!pago.Activo)
                        throw new InvalidOperationException(
                            "No se pueden registrar abonos en un pago anulado.");

                    if (pago.Monto <= 0)
                        throw new InvalidOperationException(
                            "Este paquete ya fue totalmente pagado.");

                    if (montoAbono > pago.Monto)
                    {
                        throw new InvalidOperationException(
                            $"El abono no puede ser mayor al restante por pagar. " +
                            $"Restante actual: {pago.Monto:N2}.");
                    }

                    pago.Abonos.Add(new PagoPaqueteAbono
                    {
                        Fecha = fechaAbono.Value.Date,
                        Monto = montoAbono
                    });

                    pago.Monto -= montoAbono;
                    if (pago.Monto < 0)
                        pago.Monto = 0;

                    saldoFinal = pago.Monto;

                    await _db.SaveChangesAsync();
                    await tx.CommitAsync();
                });
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorAbono"] = ex.Message;

                return string.IsNullOrWhiteSpace(idUsuario)
                    ? RedirectToAction(nameof(Index))
                    : RedirectToAction(
                        nameof(HistorialPagos),
                        new { id = idUsuario });
            }
            catch
            {
                TempData["ErrorAbono"] =
                    "Ocurrió un error registrando el abono. No se aplicaron cambios parciales.";

                return string.IsNullOrWhiteSpace(idUsuario)
                    ? RedirectToAction(nameof(Index))
                    : RedirectToAction(
                        nameof(HistorialPagos),
                        new { id = idUsuario });
            }

            TempData["OkAbono"] = saldoFinal == 0
                ? "Abono registrado correctamente. El paquete ya fue totalmente pagado."
                : "Abono registrado correctamente.";

            return RedirectToAction(
                nameof(HistorialPagos),
                new { id = idUsuario });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = ROL_GESTOR_PAGOS + "," + ROL_ADMIN)]
        public async Task<IActionResult> AgregarLeccionesPago(
            string idUsuario,
            int cantLecciones,
            decimal monto)
        {
            if (string.IsNullOrWhiteSpace(idUsuario))
                return NotFound();

            if (cantLecciones <= 0 || cantLecciones > 1000)
            {
                TempData["ErrorLecciones"] =
                    "La cantidad de lecciones debe estar entre 1 y 1000.";

                return RedirectToAction(
                    nameof(HistorialPagos),
                    new { id = idUsuario });
            }

            if (monto <= 0 || monto > 1_000_000)
            {
                TempData["ErrorLecciones"] =
                    "El monto debe ser mayor a 0 y no puede superar 1 000 000.";

                return RedirectToAction(
                    nameof(HistorialPagos),
                    new { id = idUsuario });
            }

            var usuarioExiste = await _db.Users
                .AsNoTracking()
                .AnyAsync(u => u.Id == idUsuario);

            if (!usuarioExiste)
                return NotFound();

            var strategy = _db.Database.CreateExecutionStrategy();

            try
            {
                await strategy.ExecuteAsync(async () =>
                {
                    await using var tx = await _db.Database.BeginTransactionAsync(
                        IsolationLevel.Serializable);

                    var paqueteUsuario = await _db.PaquetesUsuario
                        .Where(pu => pu.IdUsuario == idUsuario)
                        .OrderByDescending(pu => pu.FechaFin)
                        .ThenByDescending(pu => pu.IdPaqueteUsuario)
                        .FirstOrDefaultAsync();

                    if (paqueteUsuario == null)
                    {
                        throw new InvalidOperationException(
                            "El usuario no tiene un paquete asignado para agregarle lecciones.");
                    }

                    if (paqueteUsuario.CantLecciones + cantLecciones > 1000)
                    {
                        throw new InvalidOperationException(
                            "La cantidad total de lecciones del usuario no puede superar 1000.");
                    }

                    var fechaRegistro = DateTime.Now;

                    var pago = new PagoPaquete
                    {
                        IdUsuario = idUsuario,
                        Fecha = fechaRegistro,
                        TipoPago = "CONTADO",
                        Monto = 0m,
                        Activo = true,
                        MotivoAnulacion = null
                    };

                    pago.Detalles.Add(new PagoPaqueteDetalle
                    {
                        CantDias = TipoPlanDias.ClasesExtra,
                        CantLecciones = cantLecciones,
                        Pago = monto,
                        Detalle = "Clases extra"
                    });

                    pago.Abonos.Add(new PagoPaqueteAbono
                    {
                        Fecha = fechaRegistro,
                        Monto = monto
                    });

                    paqueteUsuario.CantLecciones += cantLecciones;

                    _db.PagosPaquete.Add(pago);

                    await _db.SaveChangesAsync();
                    await tx.CommitAsync();
                });
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorLecciones"] = ex.Message;

                return RedirectToAction(
                    nameof(HistorialPagos),
                    new { id = idUsuario });
            }
            catch
            {
                TempData["ErrorLecciones"] =
                    "Ocurrió un error agregando las lecciones.";

                return RedirectToAction(
                    nameof(HistorialPagos),
                    new { id = idUsuario });
            }

            TempData["OkLecciones"] =
                $"Se agregaron {cantLecciones} lección(es) y se registró el pago correctamente.";

            return RedirectToAction(
                nameof(HistorialPagos),
                new { id = idUsuario });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = ROL_GESTOR_PAGOS + "," + ROL_ADMIN)]
        public async Task<IActionResult> AnularPagoPaquete(
            int idPagoPaquete,
            string? motivoAnulacion)
        {
            if (idPagoPaquete <= 0)
                return NotFound();

            motivoAnulacion = motivoAnulacion?.Trim();

            string? idUsuarioPago = await _db.PagosPaquete
                .AsNoTracking()
                .Where(p => p.IdPagoPaquete == idPagoPaquete)
                .Select(p => p.IdUsuario)
                .FirstOrDefaultAsync();

            if (string.IsNullOrWhiteSpace(idUsuarioPago))
                return NotFound();

            if (string.IsNullOrWhiteSpace(motivoAnulacion))
            {
                TempData["ErrorAnulacion"] =
                    "Debe indicar el motivo de la anulación.";

                return RedirectToAction(
                    nameof(HistorialPagos),
                    new { id = idUsuarioPago });
            }

            if (motivoAnulacion.Length > 300)
            {
                TempData["ErrorAnulacion"] =
                    "El motivo de anulación no puede superar los 300 caracteres.";

                return RedirectToAction(
                    nameof(HistorialPagos),
                    new { id = idUsuarioPago });
            }

            var strategy = _db.Database.CreateExecutionStrategy();

            try
            {
                await strategy.ExecuteAsync(async () =>
                {
                    await using var tx = await _db.Database.BeginTransactionAsync(
                        IsolationLevel.Serializable);

                    var pago = await _db.PagosPaquete
                        .FirstOrDefaultAsync(
                            p => p.IdPagoPaquete == idPagoPaquete);

                    if (pago == null)
                        throw new KeyNotFoundException();

                    if (!pago.Activo)
                        throw new InvalidOperationException(
                            "Este pago ya se encuentra anulado.");

                    pago.Activo = false;
                    pago.MotivoAnulacion = motivoAnulacion;

                    // IMPORTANTE:
                    // No se modifican automáticamente las lecciones.
                    // El modelo actual no guarda qué PaqueteUsuario concreto fue
                    // afectado por cada pago histórico. Poner en cero el paquete
                    // más reciente podía borrar lecciones de una compra posterior.
                    await _db.SaveChangesAsync();
                    await tx.CommitAsync();
                });
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorAnulacion"] = ex.Message;

                return RedirectToAction(
                    nameof(HistorialPagos),
                    new { id = idUsuarioPago });
            }
            catch
            {
                TempData["ErrorAnulacion"] =
                    "Ocurrió un error anulando el pago.";

                return RedirectToAction(
                    nameof(HistorialPagos),
                    new { id = idUsuarioPago });
            }

            TempData["OkAnulacion"] =
                "Pago anulado correctamente. Las lecciones no se modificaron automáticamente para evitar afectar un paquete diferente.";

            return RedirectToAction(
                nameof(HistorialPagos),
                new { id = idUsuarioPago });
        }

        [Authorize(Roles = ROL_ADMIN)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Desactivar(string idUsuario)
        {
            if (string.IsNullOrWhiteSpace(idUsuario))
                return NotFound();

            var user = await _userManager.FindByIdAsync(idUsuario);
            if (user == null)
                return NotFound();

            if (!user.LockoutEnabled)
            {
                var enabled = await _userManager.SetLockoutEnabledAsync(
                    user,
                    true);

                if (!enabled.Succeeded)
                {
                    TempData["ErrorEstado"] =
                        string.Join(
                            " | ",
                            enabled.Errors.Select(e => e.Description));

                    return RedirectToAction(
                        nameof(Details),
                        new { id = idUsuario });
                }
            }

            var result = await _userManager.SetLockoutEndDateAsync(
                user,
                DateTimeOffset.UtcNow.AddYears(100));

            if (!result.Succeeded)
            {
                TempData["ErrorEstado"] =
                    string.Join(
                        " | ",
                        result.Errors.Select(e => e.Description));

                return RedirectToAction(
                    nameof(Details),
                    new { id = idUsuario });
            }

            TempData["OkEstado"] = "Usuario desactivado correctamente.";

            return RedirectToAction(
                nameof(Details),
                new { id = idUsuario });
        }

        [Authorize(Roles = ROL_ADMIN)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Activar(string idUsuario)
        {
            if (string.IsNullOrWhiteSpace(idUsuario))
                return NotFound();

            var user = await _userManager.FindByIdAsync(idUsuario);
            if (user == null)
                return NotFound();

            var result = await _userManager.SetLockoutEndDateAsync(user, null);

            if (!result.Succeeded)
            {
                TempData["ErrorEstado"] =
                    string.Join(
                        " | ",
                        result.Errors.Select(e => e.Description));

                return RedirectToAction(
                    nameof(Details),
                    new { id = idUsuario });
            }

            await _userManager.ResetAccessFailedCountAsync(user);

            TempData["OkEstado"] = "Usuario activado correctamente.";

            return RedirectToAction(
                nameof(Details),
                new { id = idUsuario });
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> MiPerfil()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(userId))
                return Challenge();

            var user = await _userManager.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return Challenge();

            return View(user);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MiPerfil(
            string? telefonoPersonal,
            string? telefonoEmergencia,
            string? lesionOperacion,
            string? patologia)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Challenge();

            user.TelefonoPersonal =
                LimpiarOpcional(telefonoPersonal);

            user.TelefonoEmergencia =
                LimpiarOpcional(telefonoEmergencia);

            user.LesionOperacion =
                LimpiarOpcional(lesionOperacion);

            user.Patologia =
                LimpiarOpcional(patologia);

            if (!string.IsNullOrWhiteSpace(user.TelefonoPersonal))
                user.PhoneNumber = user.TelefonoPersonal;

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                TempData["ErrorMiPerfil"] =
                    string.Join(
                        " | ",
                        result.Errors.Select(e => e.Description));

                return RedirectToAction(nameof(MiPerfil));
            }

            TempData["OkMiPerfil"] = "Datos actualizados correctamente.";
            return RedirectToAction(nameof(MiPerfil));
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> MiHistorialPagosPartial()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(userId))
                return Challenge();

            var pagos = await _db.PagosPaquete
                .AsNoTracking()
                .Where(p => p.IdUsuario == userId)
                .Include(p => p.Detalles)
                .Include(p => p.Abonos)
                .OrderByDescending(p => p.Fecha)
                .ThenByDescending(p => p.IdPagoPaquete)
                .Take(80)
                .ToListAsync();

            return PartialView("_MiHistorialPagosPartial", pagos);
        }

        [Authorize(Roles = ROL_ADMIN)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActualizarFamiliar(
            string idUsuario,
            bool familiar,
            int? cantidadFamilia)
        {
            if (string.IsNullOrWhiteSpace(idUsuario))
                return NotFound();

            var user = await _userManager.FindByIdAsync(idUsuario);
            if (user == null)
                return NotFound();

            if (familiar)
            {
                if (!cantidadFamilia.HasValue
                    || cantidadFamilia.Value < 2
                    || cantidadFamilia.Value > 6)
                {
                    TempData["ErrorFamiliar"] =
                        "Si el usuario es familiar, la cantidad debe estar entre 2 y 6 personas.";

                    return RedirectToAction(
                        nameof(Details),
                        new { id = idUsuario });
                }

                user.Familiar = true;
                user.CantidadFamilia = cantidadFamilia.Value;
            }
            else
            {
                user.Familiar = false;
                user.CantidadFamilia = null;
            }

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                TempData["ErrorFamiliar"] =
                    string.Join(
                        " | ",
                        result.Errors.Select(e => e.Description));

                return RedirectToAction(
                    nameof(Details),
                    new { id = idUsuario });
            }

            TempData["OkFamiliar"] =
                "Configuración familiar actualizada correctamente.";

            return RedirectToAction(
                nameof(Details),
                new { id = idUsuario });
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> MiHistorialPesajePartial()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(userId))
                return Challenge();

            var historial = await _db.Historiales
                .AsNoTracking()
                .Where(h => h.IdUsuario == userId)
                .OrderByDescending(h => h.FechaInicio)
                .ThenByDescending(h => h.IdHistorial)
                .FirstOrDefaultAsync();

            if (historial == null)
            {
                ViewBag.HistorialPesaje = null;
                ViewBag.PesoActual = null;

                return PartialView(
                    "_MiHistorialPesajePartial",
                    new List<Pesaje>());
            }

            var pesajes = await _db.Pesajes
                .AsNoTracking()
                .Where(p => p.IdHistorial == historial.IdHistorial)
                .Include(p => p.MedidasCuerpo)
                    .ThenInclude(mc => mc.Cuerpo)
                .OrderByDescending(p => p.Fecha)
                .ThenByDescending(p => p.IdPesaje)
                .Take(80)
                .ToListAsync();

            ViewBag.HistorialPesaje = historial;
            ViewBag.PesoActual = pesajes.FirstOrDefault()?.Peso;

            return PartialView("_MiHistorialPesajePartial", pesajes);
        }

        private static string? LimpiarOpcional(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }
    }
}
