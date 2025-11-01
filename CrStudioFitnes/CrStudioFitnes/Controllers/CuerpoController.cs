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
    public class CuerpoController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CuerpoController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Cuerpo
        public async Task<IActionResult> Index()
        {
            return View(await _context.Cuerpos.ToListAsync());
        }

        // GET: Cuerpo/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var cuerpo = await _context.Cuerpos
                .FirstOrDefaultAsync(m => m.IdCuerpo == id);
            if (cuerpo == null)
            {
                return NotFound();
            }

            return View(cuerpo);
        }

        // GET: Cuerpo/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Cuerpo/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("IdCuerpo,Nombre,Detalle")] Cuerpo cuerpo)
        {
            if (ModelState.IsValid)
            {
                _context.Add(cuerpo);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(cuerpo);
        }

        // GET: Cuerpo/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var cuerpo = await _context.Cuerpos.FindAsync(id);
            if (cuerpo == null)
            {
                return NotFound();
            }
            return View(cuerpo);
        }

        // POST: Cuerpo/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("IdCuerpo,Nombre,Detalle")] Cuerpo cuerpo)
        {
            if (id != cuerpo.IdCuerpo)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(cuerpo);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CuerpoExists(cuerpo.IdCuerpo))
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
            return View(cuerpo);
        }

        // GET: Cuerpo/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var cuerpo = await _context.Cuerpos
                .FirstOrDefaultAsync(m => m.IdCuerpo == id);
            if (cuerpo == null)
            {
                return NotFound();
            }

            return View(cuerpo);
        }

        // POST: Cuerpo/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var cuerpo = await _context.Cuerpos.FindAsync(id);
            if (cuerpo != null)
            {
                _context.Cuerpos.Remove(cuerpo);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CuerpoExists(int id)
        {
            return _context.Cuerpos.Any(e => e.IdCuerpo == id);
        }
    }
}
