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
    public class PagoPaquetesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PagoPaquetesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: PagoPaquetes
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.PagosPaquete.Include(p => p.Usuario);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: PagoPaquetes/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var pagoPaquete = await _context.PagosPaquete
                .Include(p => p.Usuario)
                .FirstOrDefaultAsync(m => m.IdPagoPaquete == id);
            if (pagoPaquete == null)
            {
                return NotFound();
            }

            return View(pagoPaquete);
        }

        // GET: PagoPaquetes/Create
        public IActionResult Create()
        {
            ViewData["IdUsuario"] = new SelectList(_context.Users, "Id", "Id");
            return View();
        }

        // POST: PagoPaquetes/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("IdPagoPaquete,IdUsuario,Fecha,Monto")] PagoPaquete pagoPaquete)
        {
            if (ModelState.IsValid)
            {
                _context.Add(pagoPaquete);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["IdUsuario"] = new SelectList(_context.Users, "Id", "Id", pagoPaquete.IdUsuario);
            return View(pagoPaquete);
        }

        // GET: PagoPaquetes/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var pagoPaquete = await _context.PagosPaquete.FindAsync(id);
            if (pagoPaquete == null)
            {
                return NotFound();
            }
            ViewData["IdUsuario"] = new SelectList(_context.Users, "Id", "Id", pagoPaquete.IdUsuario);
            return View(pagoPaquete);
        }

        // POST: PagoPaquetes/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("IdPagoPaquete,IdUsuario,Fecha,Monto")] PagoPaquete pagoPaquete)
        {
            if (id != pagoPaquete.IdPagoPaquete)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(pagoPaquete);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PagoPaqueteExists(pagoPaquete.IdPagoPaquete))
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
            ViewData["IdUsuario"] = new SelectList(_context.Users, "Id", "Id", pagoPaquete.IdUsuario);
            return View(pagoPaquete);
        }

        // GET: PagoPaquetes/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var pagoPaquete = await _context.PagosPaquete
                .Include(p => p.Usuario)
                .FirstOrDefaultAsync(m => m.IdPagoPaquete == id);
            if (pagoPaquete == null)
            {
                return NotFound();
            }

            return View(pagoPaquete);
        }

        // POST: PagoPaquetes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var pagoPaquete = await _context.PagosPaquete.FindAsync(id);
            if (pagoPaquete != null)
            {
                _context.PagosPaquete.Remove(pagoPaquete);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PagoPaqueteExists(int id)
        {
            return _context.PagosPaquete.Any(e => e.IdPagoPaquete == id);
        }
    }
}
