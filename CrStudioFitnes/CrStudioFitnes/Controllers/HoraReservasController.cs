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
    public class HoraReservasController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HoraReservasController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: HoraReservas
        public async Task<IActionResult> Index()
        {
            return View(await _context.HorasReserva.ToListAsync());
        }

        // GET: HoraReservas/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var horaReserva = await _context.HorasReserva
                .FirstOrDefaultAsync(m => m.IdHora == id);
            if (horaReserva == null)
            {
                return NotFound();
            }

            return View(horaReserva);
        }

        // GET: HoraReservas/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: HoraReservas/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("IdHora,Hora,Etiqueta,Activo")] HoraReserva horaReserva)
        {
            if (ModelState.IsValid)
            {
                _context.Add(horaReserva);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(horaReserva);
        }

        // GET: HoraReservas/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var horaReserva = await _context.HorasReserva.FindAsync(id);
            if (horaReserva == null)
            {
                return NotFound();
            }
            return View(horaReserva);
        }

        // POST: HoraReservas/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("IdHora,Hora,Etiqueta,Activo")] HoraReserva horaReserva)
        {
            if (id != horaReserva.IdHora)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(horaReserva);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!HoraReservaExists(horaReserva.IdHora))
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
            return View(horaReserva);
        }

        // GET: HoraReservas/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var horaReserva = await _context.HorasReserva
                .FirstOrDefaultAsync(m => m.IdHora == id);
            if (horaReserva == null)
            {
                return NotFound();
            }

            return View(horaReserva);
        }

        // POST: HoraReservas/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var horaReserva = await _context.HorasReserva.FindAsync(id);
            if (horaReserva != null)
            {
                _context.HorasReserva.Remove(horaReserva);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool HoraReservaExists(int id)
        {
            return _context.HorasReserva.Any(e => e.IdHora == id);
        }
    }
}
