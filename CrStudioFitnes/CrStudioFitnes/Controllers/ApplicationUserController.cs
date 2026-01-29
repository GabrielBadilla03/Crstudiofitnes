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
        private readonly ApplicationDbContext _db;

        public ApplicationUserController(UserManager<ApplicationUser> userManager, ApplicationDbContext db)
        {
            _userManager = userManager;
            _db = db;
        }

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

            // paquetes para el modal
            ViewBag.PaquetesDisponibles = await _db.Paquetes
                .AsNoTracking()
                .Where(p => p.Activo)
                .OrderBy(p => p.CantDias)
                .ThenBy(p => p.Pago)
                .ToListAsync();

            return View(user);
        }

        // POST: ApplicationUser/CambiarPaquete
        // ✅ Solo reemplaza el paquete ligado (IdPaquete). No cambia lecciones ni fechas.
        [HttpPost]
        [ValidateAntiForgeryToken]
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

            await using var tx = await _db.Database.BeginTransactionAsync();

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
                CantLecciones = p.CantLecciones, // ✅ lecciones propias del paquete
                Pago = p.Pago,
                Detalle = p.Detalle
            });

            _db.PagosPaquete.Add(pago);

            // 2) AHORA sí aplicar paquete al usuario (reemplaza lecciones y fechas)
            pu.CantLecciones = p.CantLecciones; // ✅ reemplaza por las del paquete
            pu.FechaInicio = hoy;
            pu.FechaFin = CalcularFechaFin(hoy, p.CantDias);

            _db.PaquetesUsuario.Update(pu);

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

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

    }
}
