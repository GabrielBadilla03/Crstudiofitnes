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
    public class PesajesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PesajesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Pesajes
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Pesajes.Include(p => p.Historial);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Pesajes/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var pesaje = await _context.Pesajes
                .Include(p => p.Historial)
                .FirstOrDefaultAsync(m => m.IdPesaje == id);
            if (pesaje == null)
            {
                return NotFound();
            }

            return View(pesaje);
        }

        // GET: Pesajes/Create
        public IActionResult Create()
        {
            ViewData["IdHistorial"] = new SelectList(_context.Historiales, "IdHistorial", "IdUsuario");
            return View();
        }

        // POST: Pesajes/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("IdPesaje,IdHistorial,Fecha,Peso")] Pesaje pesaje)
        {
            if (ModelState.IsValid)
            {
                _context.Add(pesaje);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["IdHistorial"] = new SelectList(_context.Historiales, "IdHistorial", "IdUsuario", pesaje.IdHistorial);
            return View(pesaje);
        }

        // GET: Pesajes/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var pesaje = await _context.Pesajes.FindAsync(id);
            if (pesaje == null)
            {
                return NotFound();
            }
            ViewData["IdHistorial"] = new SelectList(_context.Historiales, "IdHistorial", "IdUsuario", pesaje.IdHistorial);
            return View(pesaje);
        }

        // POST: Pesajes/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("IdPesaje,IdHistorial,Fecha,Peso")] Pesaje pesaje)
        {
            if (id != pesaje.IdPesaje)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(pesaje);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PesajeExists(pesaje.IdPesaje))
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
            ViewData["IdHistorial"] = new SelectList(_context.Historiales, "IdHistorial", "IdUsuario", pesaje.IdHistorial);
            return View(pesaje);
        }

        // GET: Pesajes/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var pesaje = await _context.Pesajes
                .Include(p => p.Historial)
                .FirstOrDefaultAsync(m => m.IdPesaje == id);
            if (pesaje == null)
            {
                return NotFound();
            }

            return View(pesaje);
        }

        // POST: Pesajes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var pesaje = await _context.Pesajes.FindAsync(id);
            if (pesaje != null)
            {
                _context.Pesajes.Remove(pesaje);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PesajeExists(int id)
        {
            return _context.Pesajes.Any(e => e.IdPesaje == id);
        }
    }
}
