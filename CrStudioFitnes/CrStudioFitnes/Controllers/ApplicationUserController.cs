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
                .Where(p => p.Activo)
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
                .FirstOrDefaultAsync(p => p.IdPaquete == idPaquete && p.Activo);

            if (paquete == null)
            {
                TempData["ErrorPaquete"] = "El paquete seleccionado no existe o está inactivo.";
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

        // POST: ApplicationUser/PagarPaquete
        // ✅ Registra pago + copia detalle del paquete + AHORA sí aplica lecciones y fechas.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Gestor de Pagos,Administrador")]
        public async Task<IActionResult> PagarPaquete(string idUsuario)
        {
            if (string.IsNullOrWhiteSpace(idUsuario))
                return NotFound();

            // Traer el PaqueteUsuario actual con el Paquete
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
            var hoy = DateTime.Today;

            // 1) Guardar pago + detalle (snapshot del paquete)
            var pago = new PagoPaquete
            {
                IdUsuario = idUsuario,
                Fecha = DateTime.Now,
                Monto = p.Pago
            };

            pago.Detalles.Add(new PagoPaqueteDetalle
            {
                CantDias = p.CantDias,
                CantLecciones = p.CantLecciones,
                Pago = p.Pago,
                Detalle = p.Detalle
            });

            _db.PagosPaquete.Add(pago);

            // 2) Aplicar paquete al usuario
            pu.CantLecciones = p.CantLecciones;
            pu.FechaInicio = hoy;
            pu.FechaFin = CalcularFechaFin(hoy, p.CantDias);

            // No hace falta Update(pu) si ya viene trackeado, pero no estorba.
            _db.PaquetesUsuario.Update(pu);

            await _db.SaveChangesAsync();

            TempData["OkPago"] = "Pago registrado. Lecciones y fechas actualizadas.";
            return RedirectToAction(nameof(Details), new { id = idUsuario });
        }

        private static DateTime CalcularFechaFin(DateTime inicio, TipoPlanDias tipo)
        {
            // inclusivo
            return tipo switch
            {
                TipoPlanDias.Diario => inicio,
                TipoPlanDias.Semanal => inicio.AddDays(6),
                TipoPlanDias.Quincenal => inicio.AddDays(14),
                TipoPlanDias.Mensual => inicio.AddMonths(1).AddDays(-1),
                _ => inicio
            };
        }

        // GET: ApplicationUser/HistorialPagos/{id}
        [Authorize(Roles = "Gestor de Pagos,Administrador,Usuario")]
        public async Task<IActionResult> HistorialPagos(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return NotFound();

            // Solo para mostrar nombre del usuario en la vista (opcional)
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
                .OrderByDescending(p => p.Fecha)
                .ToListAsync();

            return View(pagos);
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
                .OrderByDescending(p => p.Fecha)
                .Take(80)
                .ToListAsync();

            return PartialView("_MiHistorialPagosPartial", pagos);
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
