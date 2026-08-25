using CrStudioFitnes.Data;
using CrStudioFitnes.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIGE.Helpers;

namespace CrStudioFitnes.Controllers
{

    public class ApplicationUserController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _db;

        public ApplicationUserController(UserManager<ApplicationUser> userManager, ApplicationDbContext db, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _db = db;
            _roleManager = roleManager;
        }

        [Authorize(Roles = "Gestor de Pagos,Administrador,Entrenador")]
        // GET: ApplicationUser
        public async Task<IActionResult> Index(int? pageNumber, string? buscar, bool? soloActivos)
        {
            const int pageSize = 8;
            int page = pageNumber.GetValueOrDefault(1);
            if (page < 1) page = 1;

            var query = _userManager.Users.AsNoTracking();

            // ---- Filtros opcionales ----
            if (!string.IsNullOrWhiteSpace(buscar))
            {
                query = query.Where(u =>
                    u.Nombre.Contains(buscar) ||
                    u.Apellidos.Contains(buscar) ||
                    u.Cedula.Contains(buscar) ||
                    (u.Email != null && u.Email.Contains(buscar)) ||
                    (u.PhoneNumber != null && u.PhoneNumber.Contains(buscar))
                );
            }

            // "Activos" = NO bloqueados (LockoutEnd null o ya vencido)
            if (soloActivos == true)
            {
                var now = DateTimeOffset.UtcNow;
                query = query.Where(u => u.LockoutEnd == null || u.LockoutEnd <= now);
            }

            // Orden
            query = query.OrderBy(u => u.Apellidos).ThenBy(u => u.Nombre);

            // Preservar valores para la vista
            ViewData["CurrentBuscar"] = buscar;
            ViewData["CurrentSoloActivos"] = soloActivos;

            var model = await PaginatedList<ApplicationUser>.CreateAsync(query, page, pageSize);
            return View(model);
        }

        [Authorize(Roles = "Gestor de Pagos,Administrador,Entrenador")]
        // GET: ApplicationUser/Details/{id}
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

            // paquetes para el modal (ya lo tenías)
            ViewBag.PaquetesDisponibles = await _db.Paquetes
                .AsNoTracking()
                .OrderBy(p => p.CantDias)
                .ThenBy(p => p.Pago)
                .ToListAsync();

            // ✅ ROLES para el modal de roles
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

        [Authorize(Roles = "Administrador")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        // opcional (recomendado): solo admin gestiona roles
        //[Authorize(Roles = "Administrador")]
        public async Task<IActionResult> GestionarRoles(string idUsuario, string? addRole, string? removeRole)
        {
            if (string.IsNullOrWhiteSpace(idUsuario))
                return NotFound();

            var user = await _userManager.FindByIdAsync(idUsuario);
            if (user == null)
                return NotFound();

            bool wantsAdd = !string.IsNullOrWhiteSpace(addRole);
            bool wantsRemove = !string.IsNullOrWhiteSpace(removeRole);

            // ✅ regla: exactamente UNO
            if (wantsAdd == wantsRemove) // ambos true o ambos false
            {
                TempData["ErrorRoles"] = "Debés seleccionar un rol para agregar O un rol para quitar (no ambos).";
                return RedirectToAction(nameof(Details), new { id = idUsuario });
            }

            if (wantsAdd)
            {
                addRole = addRole!.Trim();

                if (!await _roleManager.RoleExistsAsync(addRole))
                {
                    TempData["ErrorRoles"] = $"El rol '{addRole}' no existe.";
                    return RedirectToAction(nameof(Details), new { id = idUsuario });
                }

                var yaLoTiene = await _userManager.IsInRoleAsync(user, addRole);
                if (!yaLoTiene)
                {
                    var res = await _userManager.AddToRoleAsync(user, addRole);
                    if (!res.Succeeded)
                    {
                        TempData["ErrorRoles"] = string.Join(" | ", res.Errors.Select(e => e.Description));
                        return RedirectToAction(nameof(Details), new { id = idUsuario });
                    }
                }

                TempData["OkRoles"] = $"Rol agregado: {addRole}";
                return RedirectToAction(nameof(Details), new { id = idUsuario });
            }
            else
            {
                removeRole = removeRole!.Trim();

                if (!await _roleManager.RoleExistsAsync(removeRole))
                {
                    TempData["ErrorRoles"] = $"El rol '{removeRole}' no existe.";
                    return RedirectToAction(nameof(Details), new { id = idUsuario });
                }

                var loTiene = await _userManager.IsInRoleAsync(user, removeRole);
                if (loTiene)
                {
                    var res = await _userManager.RemoveFromRoleAsync(user, removeRole);
                    if (!res.Succeeded)
                    {
                        TempData["ErrorRoles"] = string.Join(" | ", res.Errors.Select(e => e.Description));
                        return RedirectToAction(nameof(Details), new { id = idUsuario });
                    }
                }

                TempData["OkRoles"] = $"Rol removido: {removeRole}";
                return RedirectToAction(nameof(Details), new { id = idUsuario });
            }
        }


        // POST: ApplicationUser/CambiarPaquete
        // ✅ Solo reemplaza el paquete ligado (IdPaquete). No cambia lecciones ni fechas.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Gestor de Pagos,Administrador,Entrenador")]
        public async Task<IActionResult> CambiarPaquete(string idUsuario, int idPaquete)
        {
            if (string.IsNullOrWhiteSpace(idUsuario) || idPaquete <= 0)
                return NotFound();

            var paquete = await _db.Paquetes
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.IdPaquete == idPaquete);

            if (paquete == null)
            {
                TempData["ErrorPaquete"] = "El paquete seleccionado no existe.";
                return RedirectToAction(nameof(Details), new { id = idUsuario });
            }

            // Cargamos el registro PaqueteUsuario "actual" (como solo habrá 1, tomamos el más reciente)
            var pu = await _db.PaquetesUsuario
                .Where(x => x.IdUsuario == idUsuario)
                .OrderByDescending(x => x.FechaFin)
                .FirstOrDefaultAsync();

            if (pu == null)
            {
                // No tiene paquete: creamos el vínculo, pero SIN aplicar lecciones/fechas reales (se aplica al pagar)
                pu = new PaqueteUsuario
                {
                    IdUsuario = idUsuario,
                    IdPaquete = idPaquete,
                    CantLecciones = 0,          // ✅ no se aplican hasta pagar
                    FechaInicio = DateTime.Today, // requerido (podés dejarlo así)
                    FechaFin = DateTime.Today     // requerido (se recalcula al pagar)
                };
                _db.PaquetesUsuario.Add(pu);
            }
            else
            {
                // ✅ reemplaza el paquete ligado, pero mantiene lecciones y fechas como están
                pu.IdPaquete = idPaquete;
                _db.PaquetesUsuario.Update(pu);
            }

            await _db.SaveChangesAsync();
            TempData["OkPaquete"] = "Paquete cambiado. Se aplicará (lecciones y fechas) cuando se realice el pago.";
            return RedirectToAction(nameof(Details), new { id = idUsuario });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Gestor de Pagos,Administrador")]
        public async Task<IActionResult> PagarPaquete(string idUsuario, string tipoPago, DateTime? fechaPago)
        {
            if (string.IsNullOrWhiteSpace(idUsuario))
                return NotFound();

            tipoPago = (tipoPago ?? string.Empty).Trim().ToUpperInvariant();

            if (tipoPago != "CONTADO" && tipoPago != "CREDITO")
            {
                TempData["ErrorPago"] = "Debe seleccionar un tipo de pago válido: contado o crédito.";
                return RedirectToAction(nameof(Details), new { id = idUsuario });
            }

            if (!fechaPago.HasValue)
            {
                TempData["ErrorPago"] = "Debe seleccionar la fecha de pago.";
                return RedirectToAction(nameof(Details), new { id = idUsuario });
            }

            var fecha = fechaPago.Value.Date;

            // ✅ No permitir pagar otro paquete si tiene deuda pendiente
            // ✅ Buscar si tiene un pago pendiente.
            // Si no tiene pagos anteriores, pagoPendiente queda null y se deja continuar.
            var pagoPendiente = await _db.PagosPaquete
                .AsNoTracking()
                .Where(p => p.IdUsuario == idUsuario && p.Activo && p.Monto > 0)
                .OrderByDescending(p => p.Fecha)
                .FirstOrDefaultAsync();

            if (pagoPendiente != null)
            {
                TempData["ErrorPago"] = "Este usuario tiene un pago pendiente. No se puede registrar otro pago hasta cancelar la deuda actual.";
                return RedirectToAction(nameof(Details), new { id = idUsuario });
            }

            var pu = await _db.PaquetesUsuario
                .Include(x => x.Paquete)
                .Where(x => x.IdUsuario == idUsuario)
                .OrderByDescending(x => x.FechaFin)
                .FirstOrDefaultAsync();

            if (pu == null || pu.Paquete == null)
            {
                TempData["ErrorPago"] = "No se puede registrar el pago porque el usuario no tiene un paquete asignado.";
                return RedirectToAction(nameof(Details), new { id = idUsuario });
            }

            var p = pu.Paquete;

            // Se utilizan los valores por usuario. El respaldo con los valores totales
            // permite trabajar con paquetes antiguos que todavía no los tengan definidos.
            var montoOriginalPaquete = p.PagoPorUsuario > 0
                ? p.PagoPorUsuario
                : p.Pago;

            var leccionesUsuario = p.CantLeccionesPorUsuario > 0
                ? p.CantLeccionesPorUsuario
                : p.CantLecciones;

            if (montoOriginalPaquete <= 0)
            {
                TempData["ErrorPago"] = "El monto del paquete debe ser mayor a 0.";
                return RedirectToAction(nameof(Details), new { id = idUsuario });
            }

            if (leccionesUsuario <= 0)
            {
                TempData["ErrorPago"] = "La cantidad de lecciones del paquete debe ser mayor a 0.";
                return RedirectToAction(nameof(Details), new { id = idUsuario });
            }

            var esContado = tipoPago == "CONTADO";

            var pago = new PagoPaquete
            {
                IdUsuario = idUsuario,
                Fecha = fecha,
                TipoPago = tipoPago,
                Activo = true,
                MotivoAnulacion = null,

                // CONTADO: queda en 0 porque se cancela completo con abono.
                // CREDITO: queda el monto original porque queda pendiente.
                Monto = esContado ? 0m : montoOriginalPaquete
            };

            // El pago normal conserva el detalle completo del paquete asignado.
            // De esta forma el historial mantiene el plan, lecciones, monto y detalle.
            pago.Detalles.Add(new PagoPaqueteDetalle
            {
                CantDias = p.CantDias,
                CantLecciones = leccionesUsuario,
                Pago = montoOriginalPaquete,
                Detalle = p.Detalle
            });

            // Solo si es CONTADO se registra abono automático por el total del paquete.
            if (esContado)
            {
                pago.Abonos.Add(new PagoPaqueteAbono
                {
                    Fecha = fecha,
                    Monto = montoOriginalPaquete
                });
            }

            _db.PagosPaquete.Add(pago);

            // Aplicar paquete al usuario usando la fecha digitada.
            pu.CantLecciones = leccionesUsuario;
            pu.FechaInicio = fecha;
            pu.FechaFin = CalcularFechaFin(fecha, p.CantDias);

            _db.PaquetesUsuario.Update(pu);

            await _db.SaveChangesAsync();

            TempData["OkPago"] = esContado
                ? "Pago contado registrado correctamente. Se agregó un abono por el total del paquete."
                : "Pago a crédito registrado correctamente.";

            return RedirectToAction(nameof(Details), new { id = idUsuario });
        }

        private static DateTime CalcularFechaFin(DateTime fechaPago, TipoPlanDias tipo)
        {
            fechaPago = fechaPago.Date;

            return tipo switch
            {
                // Ejemplo: inicia 14 y termina 15.
                // Son 2 fechas incluidas: 14 y 15.
                TipoPlanDias.Diario => fechaPago.AddDays(1),

                // Ejemplo: lunes a lunes.
                // Son 8 fechas incluidas, pero 7 días de diferencia.
                TipoPlanDias.Semanal => fechaPago.AddDays(7),

                // Se conserva la lógica actual de la quincena.
                // Ejemplos:
                // 15 -> 30
                // 30 en un mes de 31 -> 15 del siguiente mes.
                TipoPlanDias.Quincenal => SumarDiasSinContarDia31(fechaPago, 15),

                // Conserva el mismo número de día del siguiente mes.
                // Ejemplo: 15/07 -> 15/08.
                // Si el día no existe, toma el último día disponible:
                // 31/01 -> 28/02 o 29/02.
                TipoPlanDias.Mensual => fechaPago.AddMonths(1),

                _ => fechaPago
            };
        }

      

        private static DateTime SumarDiasSinContarDia31(DateTime fechaPago, int dias)
        {
            var resultado = fechaPago.Date;
            var diasSumados = 0;

            while (diasSumados < dias)
            {
                resultado = resultado.AddDays(1);

                // Se ignora el día 31 para que:
                // 30 en mes de 31 + 15 días = 15 del siguiente mes
                if (resultado.Day == 31)
                    continue;

                diasSumados++;
            }

            return resultado;
        }

        [Authorize(Roles = "Gestor de Pagos,Administrador,Usuario")]
        public async Task<IActionResult> HistorialPagos(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return NotFound();

            var user = await _userManager.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
                return NotFound();

            ViewData["UsuarioNombre"] = $"{user.Nombre} {user.Apellidos}".Trim();
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
        [Authorize(Roles = "Gestor de Pagos,Administrador")]
        public async Task<IActionResult> AgregarAbonoPaquete(int idPagoPaquete, DateTime? fechaAbono, decimal montoAbono)
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

            var pago = await _db.PagosPaquete
                .Include(p => p.Abonos)
                .FirstOrDefaultAsync(p => p.IdPagoPaquete == idPagoPaquete);

            if (pago == null)
                return NotFound();

            if (!pago.Activo)
            {
                TempData["ErrorAbono"] = "No se pueden registrar abonos en un pago anulado.";
                return RedirectToAction(nameof(HistorialPagos), new { id = pago.IdUsuario });
            }

            if (pago.Monto <= 0)
            {
                TempData["OkAbono"] = "Este paquete ya fue totalmente pagado.";
                return RedirectToAction(nameof(HistorialPagos), new { id = pago.IdUsuario });
            }

            if (montoAbono > pago.Monto)
            {
                TempData["ErrorAbono"] = $"El abono no puede ser mayor al restante por pagar. Restante actual: {pago.Monto:N2}.";
                return RedirectToAction(nameof(HistorialPagos), new { id = pago.IdUsuario });
            }

            pago.Abonos.Add(new PagoPaqueteAbono
            {
                Fecha = fechaAbono.Value.Date,
                Monto = montoAbono
            });

            pago.Monto -= montoAbono;

            if (pago.Monto < 0)
                pago.Monto = 0;

            await _db.SaveChangesAsync();

            TempData["OkAbono"] = pago.Monto == 0
                ? "Abono registrado correctamente. El paquete ya fue totalmente pagado."
                : "Abono registrado correctamente.";

            return RedirectToAction(nameof(HistorialPagos), new { id = pago.IdUsuario });
        }

        // =====================================================
        // AGREGAR LECCIONES SIN CAMBIAR PAQUETE NI FECHAS
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Gestor de Pagos,Administrador")]
        public async Task<IActionResult> AgregarLeccionesPago(
            string idUsuario,
            int cantLecciones,
            decimal monto)
        {
            if (string.IsNullOrWhiteSpace(idUsuario))
                return NotFound();

            if (cantLecciones <= 0 || cantLecciones > 1000)
            {
                TempData["ErrorLecciones"] = "La cantidad de lecciones debe estar entre 1 y 1000.";
                return RedirectToAction(nameof(HistorialPagos), new { id = idUsuario });
            }

            if (monto <= 0 || monto > 1000000)
            {
                TempData["ErrorLecciones"] = "El monto debe ser mayor a 0 y no puede superar 1 000 000.";
                return RedirectToAction(nameof(HistorialPagos), new { id = idUsuario });
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
                        System.Data.IsolationLevel.Serializable);

                    // Se mantiene exactamente el mismo paquete y las mismas fechas.
                    var paqueteUsuario = await _db.PaquetesUsuario
                        .Where(pu => pu.IdUsuario == idUsuario)
                        .OrderByDescending(pu => pu.FechaFin)
                        .ThenByDescending(pu => pu.IdPaqueteUsuario)
                        .FirstOrDefaultAsync();

                    if (paqueteUsuario == null)
                        throw new InvalidOperationException(
                            "El usuario no tiene un paquete asignado para agregarle lecciones.");

                    if (paqueteUsuario.CantLecciones + cantLecciones > 1000)
                        throw new InvalidOperationException(
                            "La cantidad total de lecciones del usuario no puede superar 1000.");

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

                    // Este detalle representa únicamente lecciones adicionales.
                    // No reemplaza ni modifica el detalle histórico de los pagos normales.
                    pago.Detalles.Add(new PagoPaqueteDetalle
                    {
                        CantDias = TipoPlanDias.ClasesExtra,
                        CantLecciones = cantLecciones,
                        Pago = monto,
                        Detalle = "Clases extra"
                    });

                    // Como el monto digitado se recibe completo, se registra como abono.
                    pago.Abonos.Add(new PagoPaqueteAbono
                    {
                        Fecha = fechaRegistro,
                        Monto = monto
                    });

                    paqueteUsuario.CantLecciones += cantLecciones;

                    _db.PagosPaquete.Add(pago);
                    _db.PaquetesUsuario.Update(paqueteUsuario);

                    await _db.SaveChangesAsync();
                    await tx.CommitAsync();
                });
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorLecciones"] = ex.Message;
                return RedirectToAction(nameof(HistorialPagos), new { id = idUsuario });
            }
            catch
            {
                TempData["ErrorLecciones"] = "Ocurrió un error agregando las lecciones.";
                return RedirectToAction(nameof(HistorialPagos), new { id = idUsuario });
            }

            TempData["OkLecciones"] =
                $"Se agregaron {cantLecciones} lección(es) y se registró el pago correctamente.";

            return RedirectToAction(nameof(HistorialPagos), new { id = idUsuario });
        }

        // =====================================================
        // ANULAR PAGO SIN ELIMINAR EL REGISTRO
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Gestor de Pagos,Administrador")]
        public async Task<IActionResult> AnularPagoPaquete(
            int idPagoPaquete,
            string? motivoAnulacion)
        {
            if (idPagoPaquete <= 0)
                return NotFound();

            motivoAnulacion = motivoAnulacion?.Trim();

            if (string.IsNullOrWhiteSpace(motivoAnulacion))
            {
                TempData["ErrorAnulacion"] = "Debe indicar el motivo de la anulación.";

                var idUsuarioPago = await _db.PagosPaquete
                    .AsNoTracking()
                    .Where(p => p.IdPagoPaquete == idPagoPaquete)
                    .Select(p => p.IdUsuario)
                    .FirstOrDefaultAsync();

                return string.IsNullOrWhiteSpace(idUsuarioPago)
                    ? NotFound()
                    : RedirectToAction(nameof(HistorialPagos), new { id = idUsuarioPago });
            }

            if (motivoAnulacion.Length > 300)
            {
                TempData["ErrorAnulacion"] =
                    "El motivo de anulación no puede superar los 300 caracteres.";

                var idUsuarioPago = await _db.PagosPaquete
                    .AsNoTracking()
                    .Where(p => p.IdPagoPaquete == idPagoPaquete)
                    .Select(p => p.IdUsuario)
                    .FirstOrDefaultAsync();

                return string.IsNullOrWhiteSpace(idUsuarioPago)
                    ? NotFound()
                    : RedirectToAction(nameof(HistorialPagos), new { id = idUsuarioPago });
            }

            string? idUsuario = null;
            var strategy = _db.Database.CreateExecutionStrategy();

            try
            {
                await strategy.ExecuteAsync(async () =>
                {
                    await using var tx = await _db.Database.BeginTransactionAsync(
                        System.Data.IsolationLevel.Serializable);

                    var pago = await _db.PagosPaquete
                        .FirstOrDefaultAsync(p => p.IdPagoPaquete == idPagoPaquete);

                    if (pago == null)
                        throw new KeyNotFoundException("No se encontró el pago.");

                    idUsuario = pago.IdUsuario;

                    if (!pago.Activo)
                        throw new InvalidOperationException("Este pago ya se encuentra anulado.");

                    pago.Activo = false;
                    pago.MotivoAnulacion = motivoAnulacion;

                    // El sistema trabaja con el vínculo de paquete más reciente del usuario.
                    // Se conservan el paquete y las fechas; únicamente se ponen las lecciones en 0.
                    var paqueteUsuario = await _db.PaquetesUsuario
                        .Where(pu => pu.IdUsuario == pago.IdUsuario)
                        .OrderByDescending(pu => pu.FechaFin)
                        .ThenByDescending(pu => pu.IdPaqueteUsuario)
                        .FirstOrDefaultAsync();

                    if (paqueteUsuario != null)
                        paqueteUsuario.CantLecciones = 0;

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
                return string.IsNullOrWhiteSpace(idUsuario)
                    ? RedirectToAction(nameof(Index))
                    : RedirectToAction(nameof(HistorialPagos), new { id = idUsuario });
            }
            catch
            {
                TempData["ErrorAnulacion"] = "Ocurrió un error anulando el pago.";
                return string.IsNullOrWhiteSpace(idUsuario)
                    ? RedirectToAction(nameof(Index))
                    : RedirectToAction(nameof(HistorialPagos), new { id = idUsuario });
            }

            TempData["OkAnulacion"] =
                "Pago anulado correctamente. Las lecciones actuales del usuario se colocaron en 0.";

            return RedirectToAction(nameof(HistorialPagos), new { id = idUsuario });
        }

        [Authorize(Roles = "Administrador")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Desactivar(string idUsuario)
        {
            if (string.IsNullOrWhiteSpace(idUsuario))
                return NotFound();

            var user = await _userManager.FindByIdAsync(idUsuario);
            if (user == null)
                return NotFound();

            // Asegurar que Lockout está habilitado
            if (!user.LockoutEnabled)
                await _userManager.SetLockoutEnabledAsync(user, true);

            // Bloqueo "muy largo" = Inactivo
            var result = await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));

            if (!result.Succeeded)
            {
                TempData["ErrorEstado"] = string.Join(" | ", result.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(Details), new { id = idUsuario });
            }

            TempData["OkEstado"] = "Usuario desactivado correctamente.";
            return RedirectToAction(nameof(Details), new { id = idUsuario });
        }

        [Authorize(Roles = "Administrador")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Activar(string idUsuario)
        {
            if (string.IsNullOrWhiteSpace(idUsuario))
                return NotFound();

            var user = await _userManager.FindByIdAsync(idUsuario);
            if (user == null)
                return NotFound();

            // Quitar bloqueo = Activo
            var result = await _userManager.SetLockoutEndDateAsync(user, null);

            if (!result.Succeeded)
            {
                TempData["ErrorEstado"] = string.Join(" | ", result.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(Details), new { id = idUsuario });
            }

            TempData["OkEstado"] = "Usuario activado correctamente.";
            return RedirectToAction(nameof(Details), new { id = idUsuario });
        }


        // ==========================
        // MI PERFIL (usuario logueado)
        // ==========================
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> MiPerfil()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(userId)) return Challenge();

            var user = await _userManager.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) return Challenge();

            return View(user);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MiPerfil(string? telefonoPersonal, string? telefonoEmergencia, string? lesionOperacion, string? patologia)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            // ✅ solo campos permitidos
            user.TelefonoPersonal = string.IsNullOrWhiteSpace(telefonoPersonal) ? null : telefonoPersonal.Trim();
            user.TelefonoEmergencia = string.IsNullOrWhiteSpace(telefonoEmergencia) ? null : telefonoEmergencia.Trim();
            user.LesionOperacion = string.IsNullOrWhiteSpace(lesionOperacion) ? null : lesionOperacion.Trim();
            user.Patologia = string.IsNullOrWhiteSpace(patologia) ? null : patologia.Trim();

            var res = await _userManager.UpdateAsync(user);
            if (!res.Succeeded)
            {
                TempData["ErrorMiPerfil"] = string.Join(" | ", res.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(MiPerfil));
            }

            TempData["OkMiPerfil"] = "Datos actualizados correctamente.";
            return RedirectToAction(nameof(MiPerfil));
        }

        // ==========================
        // PARTIALS para MODALES
        // ==========================
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> MiHistorialPagosPartial()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(userId)) return Challenge();

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

        [Authorize(Roles = "Administrador")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActualizarFamiliar(string idUsuario, bool familiar, int? cantidadFamilia)
        {
            if (string.IsNullOrWhiteSpace(idUsuario))
                return NotFound();

            var user = await _userManager.FindByIdAsync(idUsuario);
            if (user == null)
                return NotFound();

            if (cantidadFamilia.HasValue && cantidadFamilia.Value < 0)
            {
                TempData["ErrorFamiliar"] = "La cantidad familiar no puede ser negativa.";
                return RedirectToAction(nameof(Details), new { id = idUsuario });
            }

            user.Familiar = familiar;

            if (familiar)
            {
                user.CantidadFamilia = cantidadFamilia;
            }
            else
            {
                user.CantidadFamilia = null;
            }

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                TempData["ErrorFamiliar"] = string.Join(" | ", result.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(Details), new { id = idUsuario });
            }

            TempData["OkFamiliar"] = "Configuración familiar actualizada correctamente.";
            return RedirectToAction(nameof(Details), new { id = idUsuario });
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> MiHistorialPesajePartial()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(userId)) return Challenge();

            // ✅ Historial más reciente por FechaInicio
            var historial = await _db.Historiales
                .AsNoTracking()
                .Where(h => h.IdUsuario == userId)
                .OrderByDescending(h => h.FechaInicio)
                .FirstOrDefaultAsync();

            if (historial == null)
            {
                ViewBag.HistorialPesaje = null;
                ViewBag.PesoActual = null;
                return PartialView("_MiHistorialPesajePartial", new List<Pesaje>());
            }

            // ✅ Pesajes SOLO de ese historial más reciente
            var pesajes = await _db.Pesajes
                .AsNoTracking()
                .Where(p => p.IdHistorial == historial.IdHistorial)
                .Include(p => p.MedidasCuerpo)
                    .ThenInclude(mc => mc.Cuerpo)
                .OrderByDescending(p => p.Fecha)
                .ThenByDescending(p => p.IdPesaje)
                .Take(80)
                .ToListAsync();

            // ✅ Para el resumen arriba de la tabla
            ViewBag.HistorialPesaje = historial;
            ViewBag.PesoActual = pesajes.FirstOrDefault()?.Peso; // último por el orden desc

            return PartialView("_MiHistorialPesajePartial", pesajes);
        }


    }
}
