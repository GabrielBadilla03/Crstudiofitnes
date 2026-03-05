using CrStudioFitnes.Data;
using CrStudioFitnes.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SIGE.Helpers;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CrStudioFitnes.Controllers
{
    [Authorize] // asumiendo que esto requiere login
    public class PaquetesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PaquetesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =======================
        // Helpers de roles
        // =======================
        private bool CanManagePaquetes()
        {
            // soporta dos posibles nombres del rol "gestor de pagos"
            return User.IsInRole("Administrador")
                || User.IsInRole("Gestor de pagos")
                || User.IsInRole("GestorPagos");
        }

        private bool IsUsuarioOEntrenador()
        {
            return User.IsInRole("Usuario") || User.IsInRole("Entrenador");
        }

        // GET: Paquetes
        // - soloActivos=true => muestra SOLO activos
        // - soloActivos=false => muestra TODOS (activos + inactivos)
        // - reset=true (solo admin/gestor) => fuerza mostrar TODOS y limpia filtros
        public async Task<IActionResult> Index(int? pageNumber, string? buscar, bool soloActivos = true, bool reset = false)
        {
            const int pageSize = 8;
            int page = pageNumber.GetValueOrDefault(1);
            if (page < 1) page = 1;

            var canManage = CanManagePaquetes();
            var isViewer = IsUsuarioOEntrenador();

            // ✅ Usuario/Entrenador: SIEMPRE activos (aunque intenten manipular querystring)
            if (!canManage && isViewer)
            {
                soloActivos = true;
                reset = false;
            }

            // ✅ Admin/Gestor: el botón limpiar manda reset=true para mostrar TODOS
            if (canManage && reset)
            {
                soloActivos = false;
                buscar = null;
                page = 1;
            }

            var query = _context.Paquetes.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(buscar))
                query = query.Where(p => p.Detalle.Contains(buscar));

            if (soloActivos)
                query = query.Where(p => p.Activo);

            query = query.OrderBy(p => p.Detalle).ThenBy(p => p.IdPaquete);

            ViewData["CurrentBuscar"] = buscar ?? "";
            ViewData["CurrentSoloActivos"] = soloActivos;
            ViewData["CanManage"] = canManage;

            var model = await PaginatedList<Paquete>.CreateAsync(query, page, pageSize);
            return View(model);
        }

        // =======================
        // Create/Edit/Delete SOLO Admin/Gestor
        // =======================

        [Authorize(Roles = "Administrador,Gestor de pagos,GestorPagos")]
        public IActionResult Create()
        {
            PopulateCantDiasDropDownList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador,Gestor de pagos,GestorPagos")]
        public async Task<IActionResult> Create([Bind("IdPaquete,CantDias,CantLecciones,Pago,Detalle,Activo")] Paquete paquete)
        {
            if (ModelState.IsValid)
            {
                _context.Add(paquete);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            PopulateCantDiasDropDownList(paquete.CantDias);
            return View(paquete);
        }

        [Authorize(Roles = "Administrador,Gestor de pagos,GestorPagos")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var paquete = await _context.Paquetes.FindAsync(id);
            if (paquete == null) return NotFound();

            PopulateCantDiasDropDownList(paquete.CantDias);
            return View(paquete);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador,Gestor de pagos,GestorPagos")]
        public async Task<IActionResult> Edit(int id, [Bind("IdPaquete,CantDias,CantLecciones,Pago,Detalle,Activo")] Paquete paquete)
        {
            if (id != paquete.IdPaquete) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(paquete);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Paquetes.Any(e => e.IdPaquete == paquete.IdPaquete))
                        return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }

            PopulateCantDiasDropDownList(paquete.CantDias);
            return View(paquete);
        }

        [Authorize(Roles = "Administrador,Gestor de pagos,GestorPagos")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var paquete = await _context.Paquetes.FirstOrDefaultAsync(m => m.IdPaquete == id);
            if (paquete == null) return NotFound();

            return View(paquete);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador,Gestor de pagos,GestorPagos")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var paquete = await _context.Paquetes.FindAsync(id);
            if (paquete != null)
                _context.Paquetes.Remove(paquete);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // =======================
        // Helper enum (igual que tuyo)
        // =======================
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
