using CrStudioFitnes.Data;
using CrStudioFitnes.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SIGE.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CrStudioFitnes.Controllers
{
    public class PaquetesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PaquetesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =======================
        // Helper para el enum
        // =======================
        private void PopulateCantDiasDropDownList(TipoPlanDias? selectedValue = null)
        {
            var items = Enum.GetValues(typeof(TipoPlanDias))
                            .Cast<TipoPlanDias>()
                            .Select(d => new SelectListItem
                            {
                                Value = d.ToString(),   // también podría ser ((int)d).ToString()
                                Text = d.ToString(),
                                Selected = selectedValue.HasValue && d == selectedValue.Value
                            })
                            .ToList();

            ViewBag.CantDiasList = items;
        }

        // GET: Paquetes
        public async Task<IActionResult> Index(int? pageNumber, string? buscar, bool? soloActivos)
        {
            const int pageSize = 8;
            int page = pageNumber.GetValueOrDefault(1);
            if (page < 1) page = 1;

            var query = _context.Paquetes.AsNoTracking();

            // ---- Filtros opcionales ----
            if (!string.IsNullOrWhiteSpace(buscar))
                query = query.Where(p => p.Detalle.Contains(buscar));

            if (soloActivos == true)
                query = query.Where(p => p.Activo);

            // Orden
            query = query.OrderBy(p => p.Detalle).ThenBy(p => p.IdPaquete);

            // Preservar valores para la vista
            ViewData["CurrentBuscar"] = buscar;
            ViewData["CurrentSoloActivos"] = soloActivos;

            var model = await PaginatedList<Paquete>.CreateAsync(query, page, pageSize);
            return View(model);
        }

        // GET: Paquetes/Create
        public IActionResult Create()
        {
            PopulateCantDiasDropDownList();   // llenar combo de enum
            return View();
        }

        // POST: Paquetes/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("IdPaquete,CantDias,CantLecciones,Pago,Detalle,Activo")] Paquete paquete)
        {
            if (ModelState.IsValid)
            {
                _context.Add(paquete);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            // Si hay error, volver a llenar el combo
            PopulateCantDiasDropDownList(paquete.CantDias);
            return View(paquete);
        }

        // GET: Paquetes/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var paquete = await _context.Paquetes.FindAsync(id);
            if (paquete == null)
            {
                return NotFound();
            }

            PopulateCantDiasDropDownList(paquete.CantDias);
            return View(paquete);
        }

        // POST: Paquetes/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("IdPaquete,CantDias,CantLecciones,Pago,Detalle,Activo")] Paquete paquete)
        {
            if (id != paquete.IdPaquete)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(paquete);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PaqueteExists(paquete.IdPaquete))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            // Si falla la validación, volver a llenar el combo con el valor seleccionado
            PopulateCantDiasDropDownList(paquete.CantDias);
            return View(paquete);
        }

        // GET: Paquetes/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var paquete = await _context.Paquetes
                .FirstOrDefaultAsync(m => m.IdPaquete == id);
            if (paquete == null)
            {
                return NotFound();
            }

            return View(paquete);
        }

        // POST: Paquetes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var paquete = await _context.Paquetes.FindAsync(id);
            if (paquete != null)
            {
                _context.Paquetes.Remove(paquete);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PaqueteExists(int id)
        {
            return _context.Paquetes.Any(e => e.IdPaquete == id);
        }
    }
}
