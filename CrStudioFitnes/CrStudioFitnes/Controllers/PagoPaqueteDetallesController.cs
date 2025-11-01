using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using CrStudioFitnes.Data;
using CrStudioFitnes.Models;

namespace CrStudioFitnes.Controllers
{
    public class PagoPaqueteDetallesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PagoPaqueteDetallesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: PagoPaqueteDetalles
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.PagosPaqueteDetalle.Include(p => p.PagoPaquete);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: PagoPaqueteDetalles/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var pagoPaqueteDetalle = await _context.PagosPaqueteDetalle
                .Include(p => p.PagoPaquete)
                .FirstOrDefaultAsync(m => m.IdPagoPaqueteDetalle == id);
            if (pagoPaqueteDetalle == null)
            {
                return NotFound();
            }

            return View(pagoPaqueteDetalle);
        }

        // GET: PagoPaqueteDetalles/Create
        public IActionResult Create()
        {
            ViewData["IdPagoPaquete"] = new SelectList(_context.PagosPaquete, "IdPagoPaquete", "IdUsuario");
            return View();
        }

        // POST: PagoPaqueteDetalles/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("IdPagoPaqueteDetalle,IdPagoPaquete,CantDias,CantLecciones,Pago,Detalle")] PagoPaqueteDetalle pagoPaqueteDetalle)
        {
            if (ModelState.IsValid)
            {
                _context.Add(pagoPaqueteDetalle);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["IdPagoPaquete"] = new SelectList(_context.PagosPaquete, "IdPagoPaquete", "IdUsuario", pagoPaqueteDetalle.IdPagoPaquete);
            return View(pagoPaqueteDetalle);
        }

        // GET: PagoPaqueteDetalles/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var pagoPaqueteDetalle = await _context.PagosPaqueteDetalle.FindAsync(id);
            if (pagoPaqueteDetalle == null)
            {
                return NotFound();
            }
            ViewData["IdPagoPaquete"] = new SelectList(_context.PagosPaquete, "IdPagoPaquete", "IdUsuario", pagoPaqueteDetalle.IdPagoPaquete);
            return View(pagoPaqueteDetalle);
        }

        // POST: PagoPaqueteDetalles/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("IdPagoPaqueteDetalle,IdPagoPaquete,CantDias,CantLecciones,Pago,Detalle")] PagoPaqueteDetalle pagoPaqueteDetalle)
        {
            if (id != pagoPaqueteDetalle.IdPagoPaqueteDetalle)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(pagoPaqueteDetalle);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PagoPaqueteDetalleExists(pagoPaqueteDetalle.IdPagoPaqueteDetalle))
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
            ViewData["IdPagoPaquete"] = new SelectList(_context.PagosPaquete, "IdPagoPaquete", "IdUsuario", pagoPaqueteDetalle.IdPagoPaquete);
            return View(pagoPaqueteDetalle);
        }

        // GET: PagoPaqueteDetalles/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var pagoPaqueteDetalle = await _context.PagosPaqueteDetalle
                .Include(p => p.PagoPaquete)
                .FirstOrDefaultAsync(m => m.IdPagoPaqueteDetalle == id);
            if (pagoPaqueteDetalle == null)
            {
                return NotFound();
            }

            return View(pagoPaqueteDetalle);
        }

        // POST: PagoPaqueteDetalles/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var pagoPaqueteDetalle = await _context.PagosPaqueteDetalle.FindAsync(id);
            if (pagoPaqueteDetalle != null)
            {
                _context.PagosPaqueteDetalle.Remove(pagoPaqueteDetalle);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PagoPaqueteDetalleExists(int id)
        {
            return _context.PagosPaqueteDetalle.Any(e => e.IdPagoPaqueteDetalle == id);
        }
    }
}
