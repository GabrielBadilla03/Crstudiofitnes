using CrStudioFitnes.Data;
using CrStudioFitnes.Models;
using CrStudioFitnes.ViewModels;
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
                query = query.Where(u => u.LockoutEnd == null || u.LockoutEnd <= now);
            }

            query = query.OrderBy(u => u.Apellidos).ThenBy(u => u.Nombre);

            ViewData["CurrentBuscar"] = buscar;
            ViewData["CurrentSoloActivos"] = soloActivos;

            var model = await PaginatedList<ApplicationUser>.CreateAsync(query, page, pageSize);
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

            // "Activo" solo controla la visibilidad en la pantalla/catálogo de paquetes.
            // Para asignar paquetes se muestran todos, activos e inactivos.
            ViewBag.PaquetesDisponibles = await _db.Paquetes
                .AsNoTracking()
                .OrderBy(p => p.EsGrupal)
                .ThenBy(p => p.CantDias)
                .ThenBy(p => p.PagoPorUsuario)
                .ToListAsync();

            var grupoActual = await CargarGrupoDetalleAsync(id);
            ViewBag.GrupoActual = grupoActual;

            if (grupoActual != null)
            {
                ViewBag.PaquetesGrupalesCompatibles = await _db.Paquetes
                    .AsNoTracking()
                    .Where(p => p.EsGrupal
                        && p.CantidadUsuarios == grupoActual.CantidadUsuarios)
                    .OrderBy(p => p.Detalle)
                    .ThenBy(p => p.IdPaquete)
                    .ToListAsync();
            }
            else
            {
                ViewBag.PaquetesGrupalesCompatibles = new List<Paquete>();
            }

            var rolesUsuario = (await _userManager.GetRolesAsync(user)).ToList();
            var todosRoles = await _roleManager.Roles
                .AsNoTracking()
                .Select(r => r.Name!)
                .Where(n => n != null && n != "")
                .OrderBy(n => n)
                .ToListAsync();

            ViewBag.RolesUsuario = rolesUsuario;
            ViewBag.RolesNoTiene = todosRoles
                .Except(rolesUsuario, StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x)
                .ToList();

            return View(user);
        }

        [HttpGet]
        [Authorize(Roles = ROL_GESTOR_PAGOS + "," + ROL_ADMIN + "," + ROL_ENTRENADOR)]
        public async Task<IActionResult> BuscarUsuariosGrupo(string termino)
        {
            termino = (termino ?? string.Empty).Trim();

            if (termino.Length < 2)
                return BadRequest(new { message = "Ingresá al menos 2 caracteres." });

            var usuariosEnGrupo = _db.GruposPaqueteUsuario
                .AsNoTracking()
                .Where(m => m.Activo && m.GrupoPaquete.Activo)
                .Select(m => m.IdUsuario);

            var ahora = DateTimeOffset.UtcNow;

            var usuariosConDeuda = _db.PagosPaquete
                .AsNoTracking()
                .Where(p => p.Activo && p.Monto > 0)
                .Select(p => p.IdUsuario);

            // Tener lecciones vigentes NO impide incorporarse a un grupo.
            // Al crear la nueva asignación grupal, las asignaciones anteriores
            // se cierran y sus lecciones quedan en 0.
            var usuarios = await _db.Users
                .AsNoTracking()
                .Where(u => !usuariosEnGrupo.Contains(u.Id)
                    && !usuariosConDeuda.Contains(u.Id)
                    && (u.LockoutEnd == null || u.LockoutEnd <= ahora)
                    && (u.Cedula.Contains(termino)
                        || (u.Email != null && u.Email.Contains(termino))
                        || u.Nombre.Contains(termino)
                        || u.Apellidos.Contains(termino)
                        || (u.Nombre + " " + u.Apellidos).Contains(termino)))
                .OrderByDescending(u => u.Cedula == termino || u.Email == termino)
                .ThenBy(u => u.Apellidos)
                .ThenBy(u => u.Nombre)
                .Take(15)
                .Select(u => new
                {
                    idUsuario = u.Id,
                    cedula = u.Cedula,
                    nombre = (u.Nombre + " " + u.Apellidos).Trim(),
                    email = u.Email ?? string.Empty,
                    familiar = u.Familiar,
                    cantidadFamilia = u.CantidadFamilia,
                    cupos = u.Familiar && u.CantidadFamilia.HasValue && u.CantidadFamilia.Value > 0
                        ? u.CantidadFamilia.Value
                        : 1
                })
                .ToListAsync();

            return Json(new { total = usuarios.Count, usuarios });
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

                if (!await _userManager.IsInRoleAsync(user, addRole))
                {
                    var result = await _userManager.AddToRoleAsync(user, addRole);
                    if (!result.Succeeded)
                    {
                        TempData["ErrorRoles"] = string.Join(" | ", result.Errors.Select(e => e.Description));
                        return RedirectToAction(nameof(Details), new { id = idUsuario });
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
                        TempData["ErrorRoles"] = string.Join(" | ", result.Errors.Select(e => e.Description));
                        return RedirectToAction(nameof(Details), new { id = idUsuario });
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
            int idPaquete,
            List<string>? idsUsuariosGrupo)
        {
            if (string.IsNullOrWhiteSpace(idUsuario) || idPaquete <= 0)
                return NotFound();

            var strategy = _db.Database.CreateExecutionStrategy();

            try
            {
                await strategy.ExecuteAsync(async () =>
                {
                    await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

                    var paquete = await _db.Paquetes
                        .FirstOrDefaultAsync(p => p.IdPaquete == idPaquete);

                    if (paquete == null)
                        throw new InvalidOperationException("El paquete seleccionado no existe.");

                    bool usuarioExiste = await _db.Users.AnyAsync(u => u.Id == idUsuario);
                    if (!usuarioExiste)
                        throw new KeyNotFoundException();

                    var grupoExistente = await _db.GruposPaqueteUsuario
                        .AsNoTracking()
                        .AnyAsync(m => m.IdUsuario == idUsuario && m.Activo && m.GrupoPaquete.Activo);

                    if (grupoExistente)
                    {
                        throw new InvalidOperationException(
                            "El usuario ya pertenece a un grupo activo. Use la opción Editar grupo para cambiar integrantes o paquete.");
                    }

                    if (!paquete.EsGrupal)
                    {
                        await AsignarPaqueteIndividualPendienteAsync(idUsuario, paquete.IdPaquete);
                    }
                    else
                    {
                        await CrearGrupoYAsignarPaqueteAsync(idUsuario, paquete, idsUsuariosGrupo);
                    }

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
                TempData["ErrorPaquete"] = ex.Message;
                return RedirectToAction(nameof(Details), new { id = idUsuario });
            }
            catch (DbUpdateException)
            {
                TempData["ErrorPaquete"] = "No se pudo asignar el paquete. Verifique que ninguno de los usuarios seleccionados pertenezca ya a otro grupo.";
                return RedirectToAction(nameof(Details), new { id = idUsuario });
            }

            TempData["OkPaquete"] = "Paquete asignado correctamente. Las lecciones y fechas se aplicarán al registrar el pago.";
            return RedirectToAction(nameof(Details), new { id = idUsuario });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = ROL_GESTOR_PAGOS + "," + ROL_ADMIN + "," + ROL_ENTRENADOR)]
        public async Task<IActionResult> ReemplazarMiembroGrupo(
            int idGrupoPaquete,
            string idUsuarioSale,
            string idUsuarioEntra,
            string idUsuarioRetorno)
        {
            if (idGrupoPaquete <= 0
                || string.IsNullOrWhiteSpace(idUsuarioSale)
                || string.IsNullOrWhiteSpace(idUsuarioEntra))
            {
                TempData["ErrorGrupo"] = "Debe seleccionar el integrante que sale y el nuevo integrante.";
                return RedirectToAction(nameof(Details), new { id = idUsuarioRetorno });
            }

            var strategy = _db.Database.CreateExecutionStrategy();

            try
            {
                await strategy.ExecuteAsync(async () =>
                {
                    await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

                    var grupo = await _db.GruposPaquete
                        .Include(g => g.Paquete)
                        .Include(g => g.Miembros)
                            .ThenInclude(m => m.PaqueteUsuario)
                        .Include(g => g.Miembros)
                            .ThenInclude(m => m.Usuario)
                        .FirstOrDefaultAsync(g => g.IdGrupoPaquete == idGrupoPaquete && g.Activo);

                    if (grupo == null)
                        throw new InvalidOperationException("El grupo ya no está activo.");

                    await ValidarGrupoSinDeudaAsync(grupo.IdGrupoPaquete);

                    var sale = grupo.Miembros.FirstOrDefault(m => m.Activo && m.IdUsuario == idUsuarioSale);
                    if (sale == null)
                        throw new InvalidOperationException("El usuario que desea quitar no pertenece al grupo activo.");

                    if (grupo.Miembros.Any(m => m.Activo && m.IdUsuario == idUsuarioEntra))
                        throw new InvalidOperationException("El nuevo usuario ya pertenece a este grupo.");

                    var nuevoUsuario = await _db.Users
                        .FirstOrDefaultAsync(u => u.Id == idUsuarioEntra);

                    if (nuevoUsuario == null)
                        throw new InvalidOperationException("No se encontró el nuevo usuario.");

                    bool nuevoEnOtroGrupo = await _db.GruposPaqueteUsuario
                        .AnyAsync(m => m.Activo && m.IdUsuario == idUsuarioEntra && m.GrupoPaquete.Activo);
                    if (nuevoEnOtroGrupo)
                        throw new InvalidOperationException("El nuevo usuario ya pertenece a otro grupo activo.");

                    await ValidarUsuarioDisponibleParaGrupoAsync(idUsuarioEntra);

                    var miembrosActivos = grupo.Miembros
                        .Where(m => m.Activo)
                        .ToList();

                    var cuposActuales = miembrosActivos.Sum(m => ObtenerCuposGrupo(m.Usuario));
                    var cuposSale = ObtenerCuposGrupo(sale.Usuario);
                    var cuposEntra = ObtenerCuposGrupo(nuevoUsuario);
                    var cuposLuegoDelCambio = cuposActuales - cuposSale + cuposEntra;

                    if (cuposLuegoDelCambio != grupo.Paquete.CantidadUsuarios)
                    {
                        throw new InvalidOperationException(
                            $"No se puede hacer el reemplazo porque el grupo debe completar exactamente {grupo.Paquete.CantidadUsuarios} cupos. " +
                            $"El integrante que sale ocupa {cuposSale} y el nuevo ocupa {cuposEntra}; el grupo quedaría con {cuposLuegoDelCambio} cupos.");
                    }

                    var asignacionesPreviasNuevo = await _db.PaquetesUsuario
                        .Where(pu => pu.IdUsuario == idUsuarioEntra && pu.Activo)
                        .ToListAsync();

                    foreach (var asignacionPrevia in asignacionesPreviasNuevo)
                    {
                        asignacionPrevia.CantLecciones = 0;
                        asignacionPrevia.Activo = false;
                    }

                    sale.Activo = false;
                    sale.FechaSalida = DateTime.Now;

                    if (sale.PaqueteUsuario != null)
                    {
                        sale.PaqueteUsuario.Activo = false;
                        sale.PaqueteUsuario.CantLecciones = 0;
                    }

                    var nuevoPu = new PaqueteUsuario
                    {
                        IdUsuario = idUsuarioEntra,
                        IdPaquete = grupo.IdPaquete,
                        CantLecciones = 0,
                        FechaInicio = DateTime.Today,
                        FechaFin = DateTime.Today,
                        Activo = true
                    };
                    _db.PaquetesUsuario.Add(nuevoPu);
                    await _db.SaveChangesAsync();

                    _db.GruposPaqueteUsuario.Add(new GrupoPaqueteUsuario
                    {
                        IdGrupoPaquete = grupo.IdGrupoPaquete,
                        IdUsuario = idUsuarioEntra,
                        IdPaqueteUsuario = nuevoPu.IdPaqueteUsuario,
                        Activo = true,
                        FechaIngreso = DateTime.Now
                    });

                    await _db.SaveChangesAsync();
                    await tx.CommitAsync();
                });
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorGrupo"] = ex.Message;
                return RedirectToAction(nameof(Details), new { id = idUsuarioRetorno });
            }

            TempData["OkGrupo"] = "Integrante reemplazado correctamente. El nuevo usuario recibirá lecciones cuando se registre la próxima renovación del grupo.";
            return RedirectToAction(nameof(Details), new { id = idUsuarioRetorno });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = ROL_GESTOR_PAGOS + "," + ROL_ADMIN + "," + ROL_ENTRENADOR)]
        public async Task<IActionResult> CambiarPaqueteGrupo(
            int idGrupoPaquete,
            int idPaqueteNuevo,
            string idUsuarioRetorno)
        {
            var strategy = _db.Database.CreateExecutionStrategy();

            try
            {
                await strategy.ExecuteAsync(async () =>
                {
                    await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

                    var grupo = await _db.GruposPaquete
                        .Include(g => g.Miembros)
                            .ThenInclude(m => m.PaqueteUsuario)
                        .Include(g => g.Miembros)
                            .ThenInclude(m => m.Usuario)
                        .FirstOrDefaultAsync(g => g.IdGrupoPaquete == idGrupoPaquete && g.Activo);

                    if (grupo == null)
                        throw new InvalidOperationException("El grupo ya no está activo.");

                    var nuevoPaquete = await _db.Paquetes
                        .FirstOrDefaultAsync(p => p.IdPaquete == idPaqueteNuevo);

                    if (nuevoPaquete == null || !nuevoPaquete.EsGrupal)
                        throw new InvalidOperationException("Debe seleccionar un paquete grupal.");

                    var miembrosActivos = grupo.Miembros
                        .Where(m => m.Activo)
                        .ToList();

                    var cuposGrupo = miembrosActivos.Sum(m => ObtenerCuposGrupo(m.Usuario));

                    if (nuevoPaquete.CantidadUsuarios != cuposGrupo)
                    {
                        throw new InvalidOperationException(
                            $"El nuevo paquete requiere {nuevoPaquete.CantidadUsuarios} cupos y el grupo actualmente ocupa {cuposGrupo}.");
                    }

                    await ValidarGrupoSinDeudaAsync(grupo.IdGrupoPaquete);

                    bool hayLecciones = grupo.Miembros
                        .Where(m => m.Activo && m.PaqueteUsuario != null)
                        .Any(m => m.PaqueteUsuario!.Activo && m.PaqueteUsuario.CantLecciones > 0);

                    if (hayLecciones)
                    {
                        throw new InvalidOperationException(
                            "No se puede cambiar el paquete del grupo mientras alguno de sus integrantes todavía tenga lecciones disponibles.");
                    }

                    grupo.IdPaquete = nuevoPaquete.IdPaquete;
                    foreach (var miembro in grupo.Miembros.Where(m => m.Activo))
                    {
                        miembro.PaqueteUsuario.IdPaquete = nuevoPaquete.IdPaquete;
                        miembro.PaqueteUsuario.CantLecciones = 0;
                        miembro.PaqueteUsuario.FechaInicio = DateTime.Today;
                        miembro.PaqueteUsuario.FechaFin = DateTime.Today;
                        miembro.PaqueteUsuario.Activo = true;
                    }

                    await _db.SaveChangesAsync();
                    await tx.CommitAsync();
                });
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorGrupo"] = ex.Message;
                return RedirectToAction(nameof(Details), new { id = idUsuarioRetorno });
            }

            TempData["OkGrupo"] = "Paquete del grupo actualizado correctamente.";
            return RedirectToAction(nameof(Details), new { id = idUsuarioRetorno });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = ROL_GESTOR_PAGOS + "," + ROL_ADMIN + "," + ROL_ENTRENADOR)]
        public async Task<IActionResult> DeshacerGrupo(
            int idGrupoPaquete,
            string idUsuarioRetorno)
        {
            var strategy = _db.Database.CreateExecutionStrategy();

            try
            {
                await strategy.ExecuteAsync(async () =>
                {
                    await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

                    var grupo = await _db.GruposPaquete
                        .Include(g => g.Miembros)
                            .ThenInclude(m => m.PaqueteUsuario)
                        .FirstOrDefaultAsync(g => g.IdGrupoPaquete == idGrupoPaquete && g.Activo);

                    if (grupo == null)
                        throw new InvalidOperationException("El grupo ya no está activo.");

                    await ValidarGrupoSinDeudaAsync(grupo.IdGrupoPaquete);

                    grupo.Activo = false;
                    grupo.FechaDesactivacion = DateTime.Now;
                    grupo.MotivoDesactivacion = "Grupo deshecho manualmente";

                    foreach (var miembro in grupo.Miembros.Where(m => m.Activo))
                    {
                        miembro.Activo = false;
                        miembro.FechaSalida = DateTime.Now;

                        if (miembro.PaqueteUsuario != null)
                        {
                            miembro.PaqueteUsuario.Activo = false;
                            miembro.PaqueteUsuario.CantLecciones = 0;
                        }
                    }

                    await _db.SaveChangesAsync();
                    await tx.CommitAsync();
                });
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorGrupo"] = ex.Message;
                return RedirectToAction(nameof(Details), new { id = idUsuarioRetorno });
            }

            TempData["OkGrupo"] = "Grupo deshecho correctamente. Los integrantes quedaron sin la asignación grupal activa.";
            return RedirectToAction(nameof(Details), new { id = idUsuarioRetorno });
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
            var strategy = _db.Database.CreateExecutionStrategy();
            bool fueGrupal = false;
            int integrantes = 1;

            try
            {
                await strategy.ExecuteAsync(async () =>
                {
                    await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

                    if (!await _db.Users.AnyAsync(u => u.Id == idUsuario))
                        throw new KeyNotFoundException();

                    var membresia = await _db.GruposPaqueteUsuario
                        .Include(m => m.GrupoPaquete)
                            .ThenInclude(g => g.Paquete)
                        .FirstOrDefaultAsync(m =>
                            m.IdUsuario == idUsuario
                            && m.Activo
                            && m.GrupoPaquete.Activo);

                    if (membresia == null)
                    {
                        await RegistrarPagoIndividualAsync(idUsuario, tipoPago, fecha);
                    }
                    else
                    {
                        fueGrupal = true;
                        integrantes = await RegistrarPagoGrupoAsync(membresia.IdGrupoPaquete, tipoPago, fecha);
                    }

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
                TempData["ErrorPago"] = "Ocurrió un error registrando el pago. No se aplicaron cambios parciales.";
                return RedirectToAction(nameof(Details), new { id = idUsuario });
            }

            if (fueGrupal)
            {
                TempData["OkPago"] = tipoPago == "CONTADO"
                    ? $"Pago grupal registrado correctamente para los {integrantes} integrantes. Cada usuario quedó pagado y recibió sus lecciones."
                    : $"Renovación grupal a crédito registrada para los {integrantes} integrantes. Cada usuario tiene ahora su deuda individual.";
            }
            else
            {
                TempData["OkPago"] = tipoPago == "CONTADO"
                    ? "Pago contado registrado correctamente."
                    : "Pago a crédito registrado correctamente.";
            }

            return RedirectToAction(nameof(Details), new { id = idUsuario });
        }

        private async Task RegistrarPagoIndividualAsync(string idUsuario, string tipoPago, DateTime fecha)
        {
            var pagoPendiente = await _db.PagosPaquete
                .AsNoTracking()
                .AnyAsync(p => p.IdUsuario == idUsuario && p.Activo && p.Monto > 0);

            if (pagoPendiente)
                throw new InvalidOperationException("Este usuario tiene un pago pendiente. Debe cancelar la deuda antes de renovar.");

            var pu = await _db.PaquetesUsuario
                .Include(x => x.Paquete)
                .Where(x => x.IdUsuario == idUsuario && x.Activo)
                .OrderByDescending(x => x.IdPaqueteUsuario)
                .FirstOrDefaultAsync();

            if (pu == null || pu.Paquete == null)
                throw new InvalidOperationException("El usuario no tiene un paquete activo asignado.");

            if (pu.Paquete.EsGrupal)
                throw new InvalidOperationException("Este paquete es grupal pero el usuario no tiene una membresía grupal válida. Revise la asignación antes de pagar.");

            CrearPagoYAplicarPaquete(
                idUsuario,
                pu,
                pu.Paquete,
                tipoPago,
                fecha,
                idGrupoPaquete: null,
                idOperacionGrupo: null);
        }

        private async Task<int> RegistrarPagoGrupoAsync(int idGrupoPaquete, string tipoPago, DateTime fecha)
        {
            var grupo = await _db.GruposPaquete
                .Include(g => g.Paquete)
                .Include(g => g.Miembros.Where(m => m.Activo))
                    .ThenInclude(m => m.Usuario)
                .Include(g => g.Miembros.Where(m => m.Activo))
                    .ThenInclude(m => m.PaqueteUsuario)
                .FirstOrDefaultAsync(g => g.IdGrupoPaquete == idGrupoPaquete && g.Activo);

            if (grupo == null || !grupo.Paquete.EsGrupal)
                throw new InvalidOperationException("No se encontró un grupo válido para realizar el pago.");

            var miembros = grupo.Miembros.Where(m => m.Activo).ToList();
            var cuposGrupo = miembros.Sum(m => ObtenerCuposGrupo(m.Usuario));

            if (cuposGrupo != grupo.Paquete.CantidadUsuarios)
            {
                throw new InvalidOperationException(
                    $"El grupo debe completar exactamente {grupo.Paquete.CantidadUsuarios} cupos y actualmente ocupa {cuposGrupo}. " +
                    "Deshaga o corrija el grupo antes de pagar.");
            }

            var ids = miembros.Select(m => m.IdUsuario).ToList();
            var pendientes = await _db.PagosPaquete
                .AsNoTracking()
                .Where(p => ids.Contains(p.IdUsuario) && p.Activo && p.Monto > 0)
                .GroupBy(p => p.IdUsuario)
                .Select(g => new { IdUsuario = g.Key, Saldo = g.Sum(x => x.Monto) })
                .ToListAsync();

            if (pendientes.Count > 0)
            {
                var nombres = miembros
                    .Where(m => pendientes.Any(p => p.IdUsuario == m.IdUsuario))
                    .Select(m => $"{m.Usuario.Nombre} {m.Usuario.Apellidos}".Trim())
                    .ToList();

                throw new InvalidOperationException(
                    "No se puede renovar el grupo porque todavía hay saldo pendiente de: " + string.Join(", ", nombres) + ".");
            }

            foreach (var miembro in miembros)
            {
                if (miembro.Usuario.LockoutEnd.HasValue
                    && miembro.Usuario.LockoutEnd.Value > DateTimeOffset.UtcNow)
                {
                    throw new InvalidOperationException(
                        $"{miembro.Usuario.Nombre} {miembro.Usuario.Apellidos} está desactivado o bloqueado. Reemplace ese integrante antes de renovar el grupo.");
                }

                if (miembro.PaqueteUsuario == null
                    || !miembro.PaqueteUsuario.Activo
                    || miembro.PaqueteUsuario.IdPaquete != grupo.IdPaquete)
                {
                    throw new InvalidOperationException(
                        $"La asignación de paquete de {miembro.Usuario.Nombre} {miembro.Usuario.Apellidos} no coincide con el grupo.");
                }
            }

            var idOperacion = Guid.NewGuid();
            foreach (var miembro in miembros)
            {
                CrearPagoYAplicarPaquete(
                    miembro.IdUsuario,
                    miembro.PaqueteUsuario!,
                    grupo.Paquete,
                    tipoPago,
                    fecha,
                    grupo.IdGrupoPaquete,
                    idOperacion,
                    ObtenerCuposGrupo(miembro.Usuario));
            }

            return miembros.Count;
        }

        private void CrearPagoYAplicarPaquete(
            string idUsuario,
            PaqueteUsuario pu,
            Paquete paquete,
            string tipoPago,
            DateTime fecha,
            int? idGrupoPaquete,
            Guid? idOperacionGrupo,
            int cantidadCupos = 1)
        {
            if (cantidadCupos <= 0)
                throw new InvalidOperationException("La cantidad de cupos del usuario es inválida.");

            decimal montoPorCupo = paquete.PagoPorUsuario > 0
                ? paquete.PagoPorUsuario
                : paquete.Pago;

            int leccionesPorCupo = paquete.CantLeccionesPorUsuario > 0
                ? paquete.CantLeccionesPorUsuario
                : paquete.CantLecciones;

            decimal montoUsuario = montoPorCupo * cantidadCupos;
            int leccionesUsuario = leccionesPorCupo * cantidadCupos;

            if (montoUsuario <= 0 || leccionesUsuario <= 0)
                throw new InvalidOperationException("El paquete tiene valores por cupo inválidos.");

            bool contado = tipoPago == "CONTADO";

            var pago = new PagoPaquete
            {
                IdUsuario = idUsuario,
                IdGrupoPaquete = idGrupoPaquete,
                IdOperacionGrupo = idOperacionGrupo,
                IdPaqueteUsuario = pu.IdPaqueteUsuario,
                Fecha = fecha,
                TipoPago = tipoPago,
                Activo = true,
                MotivoAnulacion = null,
                Monto = contado ? 0m : montoUsuario
            };

            pago.Detalles.Add(new PagoPaqueteDetalle
            {
                CantDias = paquete.CantDias,
                CantLecciones = leccionesUsuario,
                Pago = montoUsuario,
                Detalle = paquete.Detalle
            });

            if (contado)
            {
                pago.Abonos.Add(new PagoPaqueteAbono
                {
                    Fecha = fecha,
                    Monto = montoUsuario
                });
            }

            _db.PagosPaquete.Add(pago);

            pu.CantLecciones = leccionesUsuario;
            pu.FechaInicio = fecha;
            pu.FechaFin = CalcularFechaFin(fecha, paquete.CantDias);
            pu.Activo = true;
        }

        private static DateTime CalcularFechaFin(DateTime fechaPago, TipoPlanDias tipo)
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

        private static DateTime SumarDiasSinContarDia31(DateTime fechaPago, int dias)
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

            bool puedeVerOtros = User.IsInRole(ROL_ADMIN) || User.IsInRole(ROL_GESTOR_PAGOS);
            if (!puedeVerOtros)
            {
                if (!string.IsNullOrWhiteSpace(id) && !string.Equals(id, currentUserId, StringComparison.Ordinal))
                    return Forbid();
                id = currentUserId;
            }

            if (string.IsNullOrWhiteSpace(id))
                return NotFound();

            var user = await _userManager.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
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
        [Authorize(Roles = ROL_GESTOR_PAGOS + "," + ROL_ADMIN)]
        public async Task<IActionResult> AgregarAbonoPaquete(
            int idPagoPaquete,
            DateTime? fechaAbono,
            decimal montoAbono)
        {
            if (idPagoPaquete <= 0)
                return NotFound();

            if (!fechaAbono.HasValue || montoAbono <= 0)
            {
                TempData["ErrorAbono"] = !fechaAbono.HasValue
                    ? "Debe seleccionar la fecha del abono."
                    : "El monto del abono debe ser mayor a 0.";
                return RedirectToAction(nameof(Index));
            }

            string? idUsuario = null;
            decimal saldoFinal = 0;
            var strategy = _db.Database.CreateExecutionStrategy();

            try
            {
                await strategy.ExecuteAsync(async () =>
                {
                    await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

                    var pago = await _db.PagosPaquete
                        .Include(p => p.Abonos)
                        .FirstOrDefaultAsync(p => p.IdPagoPaquete == idPagoPaquete);

                    if (pago == null)
                        throw new KeyNotFoundException();

                    idUsuario = pago.IdUsuario;

                    if (!pago.Activo)
                        throw new InvalidOperationException("No se pueden registrar abonos en un pago anulado.");
                    if (pago.Monto <= 0)
                        throw new InvalidOperationException("Este paquete ya fue totalmente pagado.");
                    if (montoAbono > pago.Monto)
                        throw new InvalidOperationException($"El abono no puede ser mayor al restante por pagar. Restante actual: {pago.Monto:N2}.");

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
                    : RedirectToAction(nameof(HistorialPagos), new { id = idUsuario });
            }
            catch
            {
                TempData["ErrorAbono"] = "Ocurrió un error registrando el abono. No se aplicaron cambios parciales.";
                return string.IsNullOrWhiteSpace(idUsuario)
                    ? RedirectToAction(nameof(Index))
                    : RedirectToAction(nameof(HistorialPagos), new { id = idUsuario });
            }

            TempData["OkAbono"] = saldoFinal == 0
                ? "Abono registrado correctamente. La deuda de este integrante quedó saldada."
                : "Abono registrado correctamente.";

            return RedirectToAction(nameof(HistorialPagos), new { id = idUsuario });
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
                TempData["ErrorLecciones"] = "La cantidad de lecciones debe estar entre 1 y 1000.";
                return RedirectToAction(nameof(HistorialPagos), new { id = idUsuario });
            }

            if (monto <= 0 || monto > 1_000_000)
            {
                TempData["ErrorLecciones"] = "El monto debe ser mayor a 0 y no puede superar 1 000 000.";
                return RedirectToAction(nameof(HistorialPagos), new { id = idUsuario });
            }

            if (!await _db.Users.AsNoTracking().AnyAsync(u => u.Id == idUsuario))
                return NotFound();

            var strategy = _db.Database.CreateExecutionStrategy();

            try
            {
                await strategy.ExecuteAsync(async () =>
                {
                    await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

                    var paqueteUsuario = await _db.PaquetesUsuario
                        .Where(pu => pu.IdUsuario == idUsuario && pu.Activo)
                        .OrderByDescending(pu => pu.IdPaqueteUsuario)
                        .FirstOrDefaultAsync();

                    if (paqueteUsuario == null)
                        throw new InvalidOperationException("El usuario no tiene un paquete activo para agregarle lecciones.");

                    if (paqueteUsuario.CantLecciones + cantLecciones > 1000)
                        throw new InvalidOperationException("La cantidad total de lecciones del usuario no puede superar 1000.");

                    var fechaRegistro = DateTime.Now;
                    var pago = new PagoPaquete
                    {
                        IdUsuario = idUsuario,
                        IdPaqueteUsuario = paqueteUsuario.IdPaqueteUsuario,
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
                return RedirectToAction(nameof(HistorialPagos), new { id = idUsuario });
            }
            catch
            {
                TempData["ErrorLecciones"] = "Ocurrió un error agregando las lecciones.";
                return RedirectToAction(nameof(HistorialPagos), new { id = idUsuario });
            }

            TempData["OkLecciones"] = $"Se agregaron {cantLecciones} lección(es) y se registró el pago correctamente.";
            return RedirectToAction(nameof(HistorialPagos), new { id = idUsuario });
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

            var pagoBase = await _db.PagosPaquete
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.IdPagoPaquete == idPagoPaquete);

            if (pagoBase == null)
                return NotFound();

            string idUsuarioPago = pagoBase.IdUsuario;

            if (string.IsNullOrWhiteSpace(motivoAnulacion))
            {
                TempData["ErrorAnulacion"] = "Debe indicar el motivo de la anulación.";
                return RedirectToAction(nameof(HistorialPagos), new { id = idUsuarioPago });
            }

            if (motivoAnulacion.Length > 300)
            {
                TempData["ErrorAnulacion"] = "El motivo de anulación no puede superar los 300 caracteres.";
                return RedirectToAction(nameof(HistorialPagos), new { id = idUsuarioPago });
            }

            var ultimoPagoUsuario = await _db.PagosPaquete
                .AsNoTracking()
                .Where(p => p.IdUsuario == idUsuarioPago)
                .OrderByDescending(p => p.Fecha)
                .ThenByDescending(p => p.IdPagoPaquete)
                .Select(p => new
                {
                    p.IdPagoPaquete,
                    p.Activo
                })
                .FirstOrDefaultAsync();

            if (ultimoPagoUsuario == null || ultimoPagoUsuario.IdPagoPaquete != idPagoPaquete)
            {
                TempData["ErrorAnulacion"] =
                    "Solo se puede anular el pago más reciente del usuario. Los pagos anteriores son históricos y no se pueden anular.";
                return RedirectToAction(nameof(HistorialPagos), new { id = idUsuarioPago });
            }

            if (!pagoBase.Activo)
            {
                TempData["ErrorAnulacion"] = "El pago más reciente ya se encuentra anulado.";
                return RedirectToAction(nameof(HistorialPagos), new { id = idUsuarioPago });
            }

            bool anulacionGrupal = pagoBase.IdOperacionGrupo.HasValue;
            var strategy = _db.Database.CreateExecutionStrategy();

            try
            {
                await strategy.ExecuteAsync(async () =>
                {
                    await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

                    List<PagoPaquete> pagos;
                    if (pagoBase.IdOperacionGrupo.HasValue)
                    {
                        var op = pagoBase.IdOperacionGrupo.Value;

                        pagos = await _db.PagosPaquete
                            .Where(p => p.IdOperacionGrupo == op)
                            .ToListAsync();

                        if (pagos.Count == 0)
                            throw new InvalidOperationException("No se encontraron los pagos de la operación grupal.");

                        var usuariosOperacion = pagos
                            .Select(p => p.IdUsuario)
                            .Distinct()
                            .ToList();

                        foreach (var idUsuario in usuariosOperacion)
                        {
                            var ultimo = await _db.PagosPaquete
                                .AsNoTracking()
                                .Where(p => p.IdUsuario == idUsuario)
                                .OrderByDescending(p => p.Fecha)
                                .ThenByDescending(p => p.IdPagoPaquete)
                                .Select(p => new
                                {
                                    p.IdOperacionGrupo,
                                    p.IdPagoPaquete
                                })
                                .FirstOrDefaultAsync();

                            if (ultimo == null || ultimo.IdOperacionGrupo != op)
                            {
                                throw new InvalidOperationException(
                                    "No se puede anular esta operación grupal porque al menos uno de sus integrantes ya tiene un pago posterior.");
                            }
                        }
                    }
                    else
                    {
                        pagos = await _db.PagosPaquete
                            .Where(p => p.IdPagoPaquete == idPagoPaquete)
                            .ToListAsync();
                    }

                    if (pagos.Count == 0 || pagos.All(p => !p.Activo))
                        throw new InvalidOperationException("Este pago ya se encuentra anulado.");

                    foreach (var pago in pagos)
                    {
                        pago.Activo = false;
                        pago.MotivoAnulacion = motivoAnulacion;
                    }

                    var idsPaqueteUsuario = pagos
                        .Where(p => p.IdPaqueteUsuario.HasValue)
                        .Select(p => p.IdPaqueteUsuario!.Value)
                        .Distinct()
                        .ToList();

                    if (idsPaqueteUsuario.Count > 0)
                    {
                        var asignaciones = await _db.PaquetesUsuario
                            .Where(pu => idsPaqueteUsuario.Contains(pu.IdPaqueteUsuario))
                            .ToListAsync();

                        foreach (var asignacion in asignaciones)
                            asignacion.CantLecciones = 0;
                    }

                    await _db.SaveChangesAsync();
                    await tx.CommitAsync();
                });
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorAnulacion"] = ex.Message;
                return RedirectToAction(nameof(HistorialPagos), new { id = idUsuarioPago });
            }

            TempData["OkAnulacion"] = anulacionGrupal
                ? "La operación grupal más reciente fue anulada para todos sus integrantes y sus lecciones quedaron en 0."
                : "El pago más reciente fue anulado correctamente y las lecciones quedaron en 0.";

            return RedirectToAction(nameof(HistorialPagos), new { id = idUsuarioPago });
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
                var enabled = await _userManager.SetLockoutEnabledAsync(user, true);
                if (!enabled.Succeeded)
                {
                    TempData["ErrorEstado"] = string.Join(" | ", enabled.Errors.Select(e => e.Description));
                    return RedirectToAction(nameof(Details), new { id = idUsuario });
                }
            }

            var result = await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));
            if (!result.Succeeded)
            {
                TempData["ErrorEstado"] = string.Join(" | ", result.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(Details), new { id = idUsuario });
            }

            TempData["OkEstado"] = "Usuario desactivado correctamente.";
            return RedirectToAction(nameof(Details), new { id = idUsuario });
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
                TempData["ErrorEstado"] = string.Join(" | ", result.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(Details), new { id = idUsuario });
            }

            await _userManager.ResetAccessFailedCountAsync(user);
            TempData["OkEstado"] = "Usuario activado correctamente.";
            return RedirectToAction(nameof(Details), new { id = idUsuario });
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> MiPerfil()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(userId))
                return Challenge();

            var user = await _userManager.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
            return user == null ? Challenge() : View(user);
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

            user.TelefonoPersonal = LimpiarOpcional(telefonoPersonal);
            user.TelefonoEmergencia = LimpiarOpcional(telefonoEmergencia);
            user.LesionOperacion = LimpiarOpcional(lesionOperacion);
            user.Patologia = LimpiarOpcional(patologia);

            if (!string.IsNullOrWhiteSpace(user.TelefonoPersonal))
                user.PhoneNumber = user.TelefonoPersonal;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                TempData["ErrorMiPerfil"] = string.Join(" | ", result.Errors.Select(e => e.Description));
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

            int cuposNuevos;
            if (familiar)
            {
                if (!cantidadFamilia.HasValue || cantidadFamilia.Value < 2 || cantidadFamilia.Value > 6)
                {
                    TempData["ErrorFamiliar"] = "Si el usuario es familiar, la cantidad debe estar entre 2 y 6 personas.";
                    return RedirectToAction(nameof(Details), new { id = idUsuario });
                }

                cuposNuevos = cantidadFamilia.Value;
            }
            else
            {
                cuposNuevos = 1;
            }

            var perteneceGrupoActivo = await _db.GruposPaqueteUsuario
                .AsNoTracking()
                .AnyAsync(m => m.IdUsuario == idUsuario
                    && m.Activo
                    && m.GrupoPaquete.Activo);

            var cuposActuales = ObtenerCuposGrupo(user);

            if (perteneceGrupoActivo && cuposNuevos != cuposActuales)
            {
                TempData["ErrorFamiliar"] =
                    "No se puede cambiar la cantidad de familiares mientras el usuario pertenezca a un grupo activo. Primero debe deshacer el grupo y luego volver a armarlo con la nueva cantidad.";
                return RedirectToAction(nameof(Details), new { id = idUsuario });
            }

            if (familiar)
            {
                user.Familiar = true;
                user.CantidadFamilia = cantidadFamilia!.Value;
            }
            else
            {
                user.Familiar = false;
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
                return PartialView("_MiHistorialPesajePartial", new List<Pesaje>());
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

        private async Task AsignarPaqueteIndividualPendienteAsync(string idUsuario, int idPaquete)
        {
            var actuales = await _db.PaquetesUsuario
                .Where(pu => pu.IdUsuario == idUsuario && pu.Activo)
                .OrderByDescending(pu => pu.IdPaqueteUsuario)
                .ToListAsync();

            var actual = actuales.FirstOrDefault();
            if (actual != null
                && actual.IdPaquete != idPaquete
                && actual.CantLecciones > 0
                && actual.FechaFin.Date >= DateTime.Today)
            {
                throw new InvalidOperationException(
                    "El usuario todavía tiene lecciones disponibles en su paquete actual. Debe agotarlas antes de cambiar a otro paquete.");
            }

            foreach (var viejo in actuales.Skip(1))
                viejo.Activo = false;

            if (actual == null)
            {
                _db.PaquetesUsuario.Add(new PaqueteUsuario
                {
                    IdUsuario = idUsuario,
                    IdPaquete = idPaquete,
                    CantLecciones = 0,
                    FechaInicio = DateTime.Today,
                    FechaFin = DateTime.Today,
                    Activo = true
                });
            }
            else
            {
                actual.IdPaquete = idPaquete;
                actual.CantLecciones = 0;
                actual.FechaInicio = DateTime.Today;
                actual.FechaFin = DateTime.Today;
                actual.Activo = true;
            }
        }

        private async Task CrearGrupoYAsignarPaqueteAsync(
            string idUsuarioPrincipal,
            Paquete paquete,
            List<string>? idsUsuariosGrupo)
        {
            var otros = (idsUsuariosGrupo ?? new List<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Where(x => x != idUsuarioPrincipal)
                .Distinct(StringComparer.Ordinal)
                .ToList();

            var ids = new List<string> { idUsuarioPrincipal };
            ids.AddRange(otros);

            var usuarios = await _db.Users
                .Where(u => ids.Contains(u.Id))
                .ToListAsync();

            if (usuarios.Count != ids.Count)
                throw new InvalidOperationException("Uno o más usuarios seleccionados ya no existen.");

            var cuposSeleccionados = usuarios.Sum(ObtenerCuposGrupo);
            if (cuposSeleccionados != paquete.CantidadUsuarios)
            {
                throw new InvalidOperationException(
                    $"Este paquete requiere exactamente {paquete.CantidadUsuarios} cupos y la selección actual suma {cuposSeleccionados}. " +
                    "Los usuarios familiares cuentan según su cantidad de familiares.");
            }

            var ocupados = await _db.GruposPaqueteUsuario
                .AsNoTracking()
                .Where(m => ids.Contains(m.IdUsuario) && m.Activo && m.GrupoPaquete.Activo)
                .Select(m => m.IdUsuario)
                .ToListAsync();

            if (ocupados.Count > 0)
                throw new InvalidOperationException("Uno o más usuarios seleccionados ya pertenecen a otro grupo activo.");

            foreach (var id in ids)
                await ValidarUsuarioDisponibleParaGrupoAsync(id);

            var grupo = new GrupoPaquete
            {
                IdPaquete = paquete.IdPaquete,
                FechaCreacion = DateTime.Now,
                Activo = true
            };
            _db.GruposPaquete.Add(grupo);
            await _db.SaveChangesAsync();

            foreach (var id in ids)
            {
                var anteriores = await _db.PaquetesUsuario
                    .Where(pu => pu.IdUsuario == id && pu.Activo)
                    .ToListAsync();

                foreach (var anterior in anteriores)
                {
                    anterior.CantLecciones = 0;
                    anterior.Activo = false;
                }

                var pu = new PaqueteUsuario
                {
                    IdUsuario = id,
                    IdPaquete = paquete.IdPaquete,
                    CantLecciones = 0,
                    FechaInicio = DateTime.Today,
                    FechaFin = DateTime.Today,
                    Activo = true
                };

                _db.PaquetesUsuario.Add(pu);
                await _db.SaveChangesAsync();

                _db.GruposPaqueteUsuario.Add(new GrupoPaqueteUsuario
                {
                    IdGrupoPaquete = grupo.IdGrupoPaquete,
                    IdUsuario = id,
                    IdPaqueteUsuario = pu.IdPaqueteUsuario,
                    Activo = true,
                    FechaIngreso = DateTime.Now
                });
            }
        }

        private async Task ValidarUsuarioDisponibleParaGrupoAsync(string idUsuario)
        {
            var usuario = await _db.Users
                .AsNoTracking()
                .Where(u => u.Id == idUsuario)
                .Select(u => new { u.LockoutEnd })
                .FirstOrDefaultAsync();

            if (usuario == null)
                throw new InvalidOperationException("Uno de los usuarios seleccionados no existe.");

            if (usuario.LockoutEnd.HasValue && usuario.LockoutEnd.Value > DateTimeOffset.UtcNow)
                throw new InvalidOperationException("Uno de los usuarios seleccionados está desactivado o bloqueado.");

            bool deuda = await _db.PagosPaquete
                .AsNoTracking()
                .AnyAsync(p => p.IdUsuario == idUsuario && p.Activo && p.Monto > 0);

            if (deuda)
                throw new InvalidOperationException("Uno de los usuarios seleccionados tiene una deuda pendiente y no puede incorporarse al grupo todavía.");

        }

        private async Task ValidarGrupoSinDeudaAsync(int idGrupoPaquete)
        {
            var miembros = await _db.GruposPaqueteUsuario
                .AsNoTracking()
                .Where(m => m.IdGrupoPaquete == idGrupoPaquete && m.Activo)
                .Select(m => new
                {
                    m.IdUsuario,
                    Nombre = (m.Usuario.Nombre + " " + m.Usuario.Apellidos).Trim()
                })
                .ToListAsync();

            var ids = miembros.Select(m => m.IdUsuario).ToList();
            var conDeuda = await _db.PagosPaquete
                .AsNoTracking()
                .Where(p => ids.Contains(p.IdUsuario) && p.Activo && p.Monto > 0)
                .Select(p => p.IdUsuario)
                .Distinct()
                .ToListAsync();

            if (conDeuda.Count > 0)
            {
                var nombres = miembros.Where(m => conDeuda.Contains(m.IdUsuario)).Select(m => m.Nombre);
                throw new InvalidOperationException(
                    "No se puede modificar el grupo mientras haya deuda pendiente. Deben pagar primero: " + string.Join(", ", nombres) + ".");
            }
        }

        private async Task<GrupoPaqueteDetalleVM?> CargarGrupoDetalleAsync(string idUsuario)
        {
            var membresia = await _db.GruposPaqueteUsuario
                .AsNoTracking()
                .Where(m => m.IdUsuario == idUsuario && m.Activo && m.GrupoPaquete.Activo)
                .Select(m => new
                {
                    m.IdGrupoPaquete,
                    m.GrupoPaquete.IdPaquete,
                    m.GrupoPaquete.Paquete.Detalle,
                    m.GrupoPaquete.Paquete.CantidadUsuarios,
                    m.GrupoPaquete.Activo
                })
                .FirstOrDefaultAsync();

            if (membresia == null)
                return null;

            var miembros = await _db.GruposPaqueteUsuario
                .AsNoTracking()
                .Where(m => m.IdGrupoPaquete == membresia.IdGrupoPaquete && m.Activo)
                .Select(m => new
                {
                    m.IdUsuario,
                    Nombre = (m.Usuario.Nombre + " " + m.Usuario.Apellidos).Trim(),
                    m.Usuario.Cedula,
                    m.Usuario.Familiar,
                    m.Usuario.CantidadFamilia,
                    m.IdPaqueteUsuario,
                    CantLecciones = m.PaqueteUsuario.CantLecciones,
                    FechaInicio = (DateTime?)m.PaqueteUsuario.FechaInicio,
                    FechaFin = (DateTime?)m.PaqueteUsuario.FechaFin
                })
                .OrderBy(m => m.Nombre)
                .ToListAsync();

            var ids = miembros.Select(m => m.IdUsuario).ToList();
            var saldos = await _db.PagosPaquete
                .AsNoTracking()
                .Where(p => ids.Contains(p.IdUsuario) && p.Activo && p.Monto > 0)
                .GroupBy(p => p.IdUsuario)
                .Select(g => new { IdUsuario = g.Key, Saldo = g.Sum(p => p.Monto) })
                .ToDictionaryAsync(x => x.IdUsuario, x => x.Saldo);

            return new GrupoPaqueteDetalleVM
            {
                IdGrupoPaquete = membresia.IdGrupoPaquete,
                IdPaquete = membresia.IdPaquete,
                NombrePaquete = string.IsNullOrWhiteSpace(membresia.Detalle)
                    ? $"Paquete #{membresia.IdPaquete}"
                    : membresia.Detalle,
                CantidadUsuarios = membresia.CantidadUsuarios,
                Activo = membresia.Activo,
                Miembros = miembros.Select(m => new GrupoPaqueteMiembroVM
                {
                    IdUsuario = m.IdUsuario,
                    NombreCompleto = string.IsNullOrWhiteSpace(m.Nombre) ? "Usuario" : m.Nombre,
                    Cedula = m.Cedula,
                    Cupos = m.Familiar && m.CantidadFamilia.HasValue && m.CantidadFamilia.Value > 0
                        ? m.CantidadFamilia.Value
                        : 1,
                    IdPaqueteUsuario = m.IdPaqueteUsuario,
                    CantLecciones = m.CantLecciones,
                    FechaInicio = m.FechaInicio,
                    FechaFin = m.FechaFin,
                    SaldoPendiente = saldos.TryGetValue(m.IdUsuario, out var saldo) ? saldo : 0,
                    EsUsuarioActual = m.IdUsuario == idUsuario
                }).ToList()
            };
        }

        private static int ObtenerCuposGrupo(ApplicationUser usuario)
        {
            if (usuario.Familiar
                && usuario.CantidadFamilia.HasValue
                && usuario.CantidadFamilia.Value > 0)
            {
                return usuario.CantidadFamilia.Value;
            }

            return 1;
        }

        private static string? LimpiarOpcional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
