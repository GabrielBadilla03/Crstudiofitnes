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
    public class PesajeCuerpoController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PesajeCuerpoController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: PesajeCuerpo
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.PesajesCuerpo.Include(p => p.Cuerpo).Include(p => p.Pesaje);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: PesajeCuerpo/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var pesajeCuerpo = await _context.PesajesCuerpo
                .Include(p => p.Cuerpo)
                .Include(p => p.Pesaje)
                .FirstOrDefaultAsync(m => m.IdPesaje == id);
            if (pesajeCuerpo == null)
            {
                return NotFound();
            }

            return View(pesajeCuerpo);
        }

        // GET: PesajeCuerpo/Create
        public IActionResult Create()
        {
            ViewData["IdCuerpo"] = new SelectList(_context.Cuerpos, "IdCuerpo", "Nombre");
            ViewData["IdPesaje"] = new SelectList(_context.Pesajes, "IdPesaje", "IdPesaje");
            return View();
        }

        // POST: PesajeCuerpo/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("IdPesaje,IdCuerpo,Medida")] PesajeCuerpo pesajeCuerpo)
        {
            if (ModelState.IsValid)
            {
                _context.Add(pesajeCuerpo);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["IdCuerpo"] = new SelectList(_context.Cuerpos, "IdCuerpo", "Nombre", pesajeCuerpo.IdCuerpo);
            ViewData["IdPesaje"] = new SelectList(_context.Pesajes, "IdPesaje", "IdPesaje", pesajeCuerpo.IdPesaje);
            return View(pesajeCuerpo);
        }

        // GET: PesajeCuerpo/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var pesajeCuerpo = await _context.PesajesCuerpo.FindAsync(id);
            if (pesajeCuerpo == null)
            {
                return NotFound();
            }
            ViewData["IdCuerpo"] = new SelectList(_context.Cuerpos, "IdCuerpo", "Nombre", pesajeCuerpo.IdCuerpo);
            ViewData["IdPesaje"] = new SelectList(_context.Pesajes, "IdPesaje", "IdPesaje", pesajeCuerpo.IdPesaje);
            return View(pesajeCuerpo);
        }

        // POST: PesajeCuerpo/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("IdPesaje,IdCuerpo,Medida")] PesajeCuerpo pesajeCuerpo)
        {
            if (id != pesajeCuerpo.IdPesaje)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(pesajeCuerpo);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PesajeCuerpoExists(pesajeCuerpo.IdPesaje))
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
            ViewData["IdCuerpo"] = new SelectList(_context.Cuerpos, "IdCuerpo", "Nombre", pesajeCuerpo.IdCuerpo);
            ViewData["IdPesaje"] = new SelectList(_context.Pesajes, "IdPesaje", "IdPesaje", pesajeCuerpo.IdPesaje);
            return View(pesajeCuerpo);
        }

        // GET: PesajeCuerpo/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var pesajeCuerpo = await _context.PesajesCuerpo
                .Include(p => p.Cuerpo)
                .Include(p => p.Pesaje)
                .FirstOrDefaultAsync(m => m.IdPesaje == id);
            if (pesajeCuerpo == null)
            {
                return NotFound();
            }

            return View(pesajeCuerpo);
        }

        // POST: PesajeCuerpo/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var pesajeCuerpo = await _context.PesajesCuerpo.FindAsync(id);
            if (pesajeCuerpo != null)
            {
                _context.PesajesCuerpo.Remove(pesajeCuerpo);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PesajeCuerpoExists(int id)
        {
            return _context.PesajesCuerpo.Any(e => e.IdPesaje == id);
        }
    }
}
