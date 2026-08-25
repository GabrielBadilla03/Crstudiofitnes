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
        private readonly ApplicationDbContext _context;

        public PaquetesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // Helpers de roles
        // =========================================================
        private bool CanManagePaquetes()
        {
            return User.IsInRole("Administrador")
                || User.IsInRole("Gestor de pagos")
                || User.IsInRole("GestorPagos");
        }

        private bool IsUsuarioOEntrenador()
        {
            return User.IsInRole("Usuario") || User.IsInRole("Entrenador");
        }

        // =========================================================
        // GET: Paquetes
        // =========================================================
        // soloActivos=true  => muestra únicamente paquetes visibles.
        // soloActivos=false => muestra activos e inactivos.
        // reset=true        => limpia filtros y muestra todos al administrador/gestor.
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

            // Usuario y entrenador siempre ven únicamente paquetes activos.
            if (!canManage && isViewer)
            {
                soloActivos = true;
                reset = false;
            }

            // Administrador y gestor pueden limpiar para mostrar todos.
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

            var model = await PaginatedList<Paquete>.CreateAsync(
                query,
                page,
                pageSize);

            return View(model);
        }

        // =========================================================
        // GET: Paquetes/Details/5
        // =========================================================
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var paquete = await _context.Paquetes
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.IdPaquete == id.Value);

            if (paquete == null)
                return NotFound();

            // Usuario y entrenador no pueden consultar paquetes ocultos.
            if (!CanManagePaquetes() && !paquete.Activo)
                return NotFound();

            ViewData["CanManage"] = CanManagePaquetes();
            return View(paquete);
        }

        // =========================================================
        // GET: Paquetes/Create
        // =========================================================
        [Authorize(Roles = "Administrador,Gestor de pagos,GestorPagos")]
        public IActionResult Create()
        {
            PopulateCantDiasDropDownList();
            return View(new Paquete { Activo = true });
        }

        // =========================================================
        // POST: Paquetes/Create
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador,Gestor de pagos,GestorPagos")]
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

        // =========================================================
        // GET: Paquetes/Edit/5
        // =========================================================
        [Authorize(Roles = "Administrador,Gestor de pagos,GestorPagos")]
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

        // =========================================================
        // POST: Paquetes/Edit/5
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador,Gestor de pagos,GestorPagos")]
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

        // =========================================================
        // GET: Paquetes/Delete/5
        // =========================================================
        [Authorize(Roles = "Administrador,Gestor de pagos,GestorPagos")]
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

        // =========================================================
        // POST: Paquetes/Delete/5
        // =========================================================
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador,Gestor de pagos,GestorPagos")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var paquete = await _context.Paquetes.FindAsync(id);

            if (paquete == null)
                return NotFound();

            _context.Paquetes.Remove(paquete);
            await _context.SaveChangesAsync();

            TempData["Ok"] = "Paquete eliminado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // Helper enum
        // =========================================================
        private void PopulateCantDiasDropDownList(TipoPlanDias? selectedValue = null)
        {
            var items = Enum.GetValues(typeof(TipoPlanDias))
                .Cast<TipoPlanDias>()
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
