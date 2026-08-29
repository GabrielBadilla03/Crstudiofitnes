using CrStudioFitnes.Data;
using CrStudioFitnes.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SIGE.Helpers;

namespace CrStudioFitnes.Controllers
{
    [Authorize]
    public class PaquetesController : Controller
    {
        private const string ROL_ADMIN = "Administrador";
        private const string ROL_GESTOR_PAGOS = "Gestor de Pagos";
        private const string ROL_USUARIO = "Usuario";
        private const string ROL_ENTRENADOR = "Entrenador";

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

        private bool IsUsuarioOEntrenador()
        {
            return User.IsInRole(ROL_USUARIO)
                || User.IsInRole(ROL_ENTRENADOR);
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
            bool isViewer = IsUsuarioOEntrenador();

            if (!canManage && isViewer)
            {
                soloActivos = true;
                reset = false;
            }

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

                query = query.Where(p =>
                    p.Detalle != null
                    && p.Detalle.Contains(texto));
            }

            if (soloActivos)
                query = query.Where(p => p.Activo);

            query = query
                .OrderBy(p => p.Detalle)
                .ThenBy(p => p.IdPaquete);

            ViewData["CurrentBuscar"] = buscar ?? string.Empty;
            ViewData["CurrentSoloActivos"] = soloActivos;
            ViewData["CanManage"] = canManage;

            var model = await PaginatedList<Paquete>.CreateAsync(
                query,
                page,
                pageSize);

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

            if (!CanManagePaquetes() && !paquete.Activo)
                return NotFound();

            ViewData["CanManage"] = CanManagePaquetes();

            return View(paquete);
        }

        [Authorize(Roles = ROL_ADMIN + "," + ROL_GESTOR_PAGOS)]
        public IActionResult Create()
        {
            PopulateCantDiasDropDownList();
            return View(new Paquete { Activo = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = ROL_ADMIN + "," + ROL_GESTOR_PAGOS)]
        public async Task<IActionResult> Create(
            [Bind("IdPaquete,CantDias,CantLecciones,Pago,CantLeccionesPorUsuario,PagoPorUsuario,Detalle,Activo")]
            Paquete paquete)
        {
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

            return View(paquete);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = ROL_ADMIN + "," + ROL_GESTOR_PAGOS)]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("IdPaquete,CantDias,CantLecciones,Pago,CantLeccionesPorUsuario,PagoPorUsuario,Detalle,Activo")]
            Paquete paquete)
        {
            if (id != paquete.IdPaquete)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Paquetes.Update(paquete);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    bool existe = await _context.Paquetes
                        .AnyAsync(p => p.IdPaquete == paquete.IdPaquete);

                    if (!existe)
                        return NotFound();

                    throw;
                }

                TempData["Ok"] = "Paquete actualizado correctamente.";
                return RedirectToAction(nameof(Index));
            }

            PopulateCantDiasDropDownList(paquete.CantDias);
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

            if (!paquete.Activo)
            {
                TempData["Ok"] = "El paquete ya estaba inactivo.";
                return RedirectToAction(nameof(Index));
            }

            // Baja lógica: conserva pagos e historial que referencian este paquete.
            paquete.Activo = false;
            await _context.SaveChangesAsync();

            TempData["Ok"] =
                "Paquete desactivado correctamente. Se conserva su historial.";

            return RedirectToAction(nameof(Index));
        }

        private void PopulateCantDiasDropDownList(
            TipoPlanDias? selectedValue = null)
        {
            var items = Enum
                .GetValues(typeof(TipoPlanDias))
                .Cast<TipoPlanDias>()
                .Where(d => d != TipoPlanDias.ClasesExtra)
                .Select(d => new SelectListItem
                {
                    Value = d.ToString(),
                    Text = d.ToString(),
                    Selected = selectedValue.HasValue
                        && d == selectedValue.Value
                })
                .ToList();

            ViewBag.CantDiasList = items;
        }
    }
}
