using CrStudioFitnes.Data;
using CrStudioFitnes.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SIGE.Helpers;

namespace CrStudioFitnes.Controllers
{
    [Authorize(Roles = "Usuario,Entrenador,Administrador,Gestor de Pagos")]
    public class PaquetesController : Controller
    {
        private const string ROL_ADMIN = "Administrador";
        private const string ROL_GESTOR_PAGOS = "Gestor de Pagos";

        private readonly ApplicationDbContext _context;

        public PaquetesController(ApplicationDbContext context)
        {
            _context = context;
        }

        private bool CanManagePaquetes()
        {
            return User.IsInRole(ROL_ADMIN)
                || User.IsInRole(ROL_GESTOR_PAGOS);
        }

        public async Task<IActionResult> Index(
            int? pageNumber,
            string? buscar,
            bool soloActivos = true,
            bool reset = false)
        {
            const int pageSize = 8;

            int page = pageNumber.GetValueOrDefault(1);
            if (page < 1)
                page = 1;

            bool canManage = CanManagePaquetes();

            if (!canManage)
                soloActivos = true;

            if (canManage && reset)
            {
                soloActivos = false;
                buscar = null;
                page = 1;
            }

            IQueryable<Paquete> query = _context.Paquetes.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                string texto = buscar.Trim();
                query = query.Where(p => p.Detalle != null && p.Detalle.Contains(texto));
            }

            if (soloActivos)
                query = query.Where(p => p.Activo);

            query = query
                .OrderBy(p => p.Detalle)
                .ThenBy(p => p.IdPaquete);

            ViewData["CurrentBuscar"] = buscar ?? string.Empty;
            ViewData["CurrentSoloActivos"] = soloActivos;
            ViewData["CanManage"] = canManage;

            var model = await PaginatedList<Paquete>.CreateAsync(query, page, pageSize);
            return View(model);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var paquete = await _context.Paquetes
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.IdPaquete == id.Value);

            if (paquete == null)
                return NotFound();

            ViewData["CanManage"] = CanManagePaquetes();
            ViewBag.GruposActivos = await _context.GruposPaquete
                .AsNoTracking()
                .CountAsync(g => g.IdPaquete == paquete.IdPaquete && g.Activo);

            return View(paquete);
        }

        [Authorize(Roles = ROL_ADMIN + "," + ROL_GESTOR_PAGOS)]
        public IActionResult Create()
        {
            PopulateCantDiasDropDownList();
            return View(new Paquete
            {
                Activo = true,
                EsGrupal = false,
                CantidadUsuarios = 1
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = ROL_ADMIN + "," + ROL_GESTOR_PAGOS)]
        public async Task<IActionResult> Create(
            [Bind("IdPaquete,CantDias,CantLecciones,Pago,Detalle,Activo,EsGrupal,CantidadUsuarios")]
            Paquete paquete)
        {
            AplicarCalculosPaquete(paquete);

            if (ModelState.IsValid)
            {
                _context.Paquetes.Add(paquete);
                await _context.SaveChangesAsync();

                TempData["Ok"] = "Paquete creado correctamente.";
                return RedirectToAction(nameof(Index));
            }

            PopulateCantDiasDropDownList(paquete.CantDias);
            return View(paquete);
        }

        [Authorize(Roles = ROL_ADMIN + "," + ROL_GESTOR_PAGOS)]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var paquete = await _context.Paquetes.FindAsync(id.Value);
            if (paquete == null)
                return NotFound();

            PopulateCantDiasDropDownList(paquete.CantDias);
            ViewBag.TieneGruposActivos = await _context.GruposPaquete
                .AsNoTracking()
                .AnyAsync(g => g.IdPaquete == paquete.IdPaquete && g.Activo);

            return View(paquete);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = ROL_ADMIN + "," + ROL_GESTOR_PAGOS)]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("IdPaquete,CantDias,CantLecciones,Pago,Detalle,Activo,EsGrupal,CantidadUsuarios")]
            Paquete paquete)
        {
            if (id != paquete.IdPaquete)
                return NotFound();

            var actual = await _context.Paquetes
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.IdPaquete == id);

            if (actual == null)
                return NotFound();

            bool tieneGruposActivos = await _context.GruposPaquete
                .AsNoTracking()
                .AnyAsync(g => g.IdPaquete == id && g.Activo);

            if (tieneGruposActivos
                && (actual.EsGrupal != paquete.EsGrupal
                    || actual.CantidadUsuarios != paquete.CantidadUsuarios))
            {
                ModelState.AddModelError(
                    string.Empty,
                    "No se puede cambiar si el paquete es grupal ni su cantidad de cupos mientras existan grupos activos. Primero edite o deshaga esos grupos.");
            }

            AplicarCalculosPaquete(paquete);

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Paquetes.Update(paquete);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.Paquetes.AnyAsync(p => p.IdPaquete == paquete.IdPaquete))
                        return NotFound();
                    throw;
                }

                TempData["Ok"] = "Paquete actualizado correctamente.";
                return RedirectToAction(nameof(Index));
            }

            PopulateCantDiasDropDownList(paquete.CantDias);
            ViewBag.TieneGruposActivos = tieneGruposActivos;
            return View(paquete);
        }

        [Authorize(Roles = ROL_ADMIN + "," + ROL_GESTOR_PAGOS)]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var paquete = await _context.Paquetes
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.IdPaquete == id.Value);

            if (paquete == null)
                return NotFound();

            return View(paquete);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = ROL_ADMIN + "," + ROL_GESTOR_PAGOS)]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var paquete = await _context.Paquetes.FindAsync(id);
            if (paquete == null)
                return NotFound();

            // Activo controla únicamente si el paquete se muestra al aplicar
            // el filtro de paquetes activos. No afecta asignaciones, pagos ni grupos.
            paquete.Activo = false;
            await _context.SaveChangesAsync();

            TempData["Ok"] = "Paquete ocultado del filtro de activos correctamente. Las asignaciones, pagos y grupos existentes continúan funcionando.";
            return RedirectToAction(nameof(Index));
        }

        private void AplicarCalculosPaquete(Paquete paquete)
        {
            // Son campos derivados: cualquier valor enviado por el cliente se ignora
            // y se recalcula en servidor. Se limpian errores previos de validación
            // porque el model binder los valida antes de ejecutar este método.
            ModelState.Remove(nameof(Paquete.CantLeccionesPorUsuario));
            ModelState.Remove(nameof(Paquete.PagoPorUsuario));
            ModelState.Remove(nameof(Paquete.CantidadUsuarios));

            if (!paquete.EsGrupal)
                paquete.CantidadUsuarios = 1;

            if (paquete.EsGrupal && paquete.CantidadUsuarios < 2)
            {
                ModelState.AddModelError(
                    nameof(Paquete.CantidadUsuarios),
                    "Un paquete grupal debe tener al menos 2 cupos.");
                return;
            }

            if (paquete.CantidadUsuarios <= 0)
                return;

            if (paquete.CantLecciones > 0)
            {
                if (paquete.CantLecciones % paquete.CantidadUsuarios != 0)
                {
                    ModelState.AddModelError(
                        nameof(Paquete.CantLecciones),
                        "La cantidad total de lecciones debe poder dividirse exactamente entre la cantidad de cupos.");
                }
                else
                {
                    paquete.CantLeccionesPorUsuario =
                        paquete.CantLecciones / paquete.CantidadUsuarios;
                }
            }

            if (paquete.Pago > 0)
            {
                decimal centavos = paquete.Pago * 100m;

                if (centavos != decimal.Truncate(centavos))
                {
                    ModelState.AddModelError(
                        nameof(Paquete.Pago),
                        "El monto total solo puede tener 2 decimales.");
                }
                else if (centavos % paquete.CantidadUsuarios != 0)
                {
                    ModelState.AddModelError(
                        nameof(Paquete.Pago),
                        "El monto total debe poder dividirse exactamente entre la cantidad de cupos, hasta centavos.");
                }
                else
                {
                    paquete.PagoPorUsuario =
                        paquete.Pago / paquete.CantidadUsuarios;
                }
            }
        }

        private void PopulateCantDiasDropDownList(TipoPlanDias? selectedValue = null)
        {
            var items = Enum
                .GetValues(typeof(TipoPlanDias))
                .Cast<TipoPlanDias>()
                .Where(d => d != TipoPlanDias.ClasesExtra)
                .Select(d => new SelectListItem
                {
                    Value = d.ToString(),
                    Text = d.ToString(),
                    Selected = selectedValue.HasValue && d == selectedValue.Value
                })
                .ToList();

            ViewBag.CantDiasList = items;
        }
    }
}
