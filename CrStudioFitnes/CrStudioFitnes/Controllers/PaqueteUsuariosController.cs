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
    public class PaqueteUsuariosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PaqueteUsuariosController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: PaqueteUsuarios
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.PaquetesUsuario.Include(p => p.Paquete).Include(p => p.Usuario);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: PaqueteUsuarios/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var paqueteUsuario = await _context.PaquetesUsuario
                .Include(p => p.Paquete)
                .Include(p => p.Usuario)
                .FirstOrDefaultAsync(m => m.IdPaqueteUsuario == id);
            if (paqueteUsuario == null)
            {
                return NotFound();
            }

            return View(paqueteUsuario);
        }

        // GET: PaqueteUsuarios/Create
        public IActionResult Create()
        {
            ViewData["IdPaquete"] = new SelectList(_context.Paquetes, "IdPaquete", "IdPaquete");
            ViewData["IdUsuario"] = new SelectList(_context.Users, "Id", "Id");
            return View();
        }

        // POST: PaqueteUsuarios/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("IdPaqueteUsuario,IdPaquete,IdUsuario,CantLecciones,Fecha")] PaqueteUsuario paqueteUsuario)
        {
            if (ModelState.IsValid)
            {
                _context.Add(paqueteUsuario);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["IdPaquete"] = new SelectList(_context.Paquetes, "IdPaquete", "IdPaquete", paqueteUsuario.IdPaquete);
            ViewData["IdUsuario"] = new SelectList(_context.Users, "Id", "Id", paqueteUsuario.IdUsuario);
            return View(paqueteUsuario);
        }

        // GET: PaqueteUsuarios/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var paqueteUsuario = await _context.PaquetesUsuario.FindAsync(id);
            if (paqueteUsuario == null)
            {
                return NotFound();
            }
            ViewData["IdPaquete"] = new SelectList(_context.Paquetes, "IdPaquete", "IdPaquete", paqueteUsuario.IdPaquete);
            ViewData["IdUsuario"] = new SelectList(_context.Users, "Id", "Id", paqueteUsuario.IdUsuario);
            return View(paqueteUsuario);
        }

        // POST: PaqueteUsuarios/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("IdPaqueteUsuario,IdPaquete,IdUsuario,CantLecciones,Fecha")] PaqueteUsuario paqueteUsuario)
        {
            if (id != paqueteUsuario.IdPaqueteUsuario)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(paqueteUsuario);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PaqueteUsuarioExists(paqueteUsuario.IdPaqueteUsuario))
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
            ViewData["IdPaquete"] = new SelectList(_context.Paquetes, "IdPaquete", "IdPaquete", paqueteUsuario.IdPaquete);
            ViewData["IdUsuario"] = new SelectList(_context.Users, "Id", "Id", paqueteUsuario.IdUsuario);
            return View(paqueteUsuario);
        }

        // GET: PaqueteUsuarios/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var paqueteUsuario = await _context.PaquetesUsuario
                .Include(p => p.Paquete)
                .Include(p => p.Usuario)
                .FirstOrDefaultAsync(m => m.IdPaqueteUsuario == id);
            if (paqueteUsuario == null)
            {
                return NotFound();
            }

            return View(paqueteUsuario);
        }

        // POST: PaqueteUsuarios/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var paqueteUsuario = await _context.PaquetesUsuario.FindAsync(id);
            if (paqueteUsuario != null)
            {
                _context.PaquetesUsuario.Remove(paqueteUsuario);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PaqueteUsuarioExists(int id)
        {
            return _context.PaquetesUsuario.Any(e => e.IdPaqueteUsuario == id);
        }
    }
}
