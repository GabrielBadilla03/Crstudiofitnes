using CrStudioFitnes.Data;
using CrStudioFitnes.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CrStudioFitnes.Controllers
{
    [Authorize(Roles = "Gestor de Horarios,Administrador")]
    public class BloqueoHorariosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BloqueoHorariosController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: BloqueoHorarios
        public async Task<IActionResult> Index()
        {
            var data = await _context.BloqueosHorarios
                .AsNoTracking()
                .Where(b => b.Activo)
                .Include(b => b.HoraReserva)
                .ToListAsync();

            return View(data);
        }


        // GET: BloqueoHorarios/Create
        public IActionResult Create()
        {
            ViewData["IdHora"] = new SelectList(
                _context.HorasReserva
                    .AsNoTracking()
                    .Where(h => h.Activo)
                    .OrderBy(h => h.Hora),
                "IdHora",
                "Etiqueta"
            );

            return View();
        }

        // POST: BloqueoHorarios/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("IdBloqueoHorario,Fecha,IdHora,Motivo,Activo")] BloqueoHorario bloqueoHorario)
        {
            if (ModelState.IsValid)
            {
                _context.Add(bloqueoHorario);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["IdHora"] = new SelectList(
                _context.HorasReserva
                    .AsNoTracking()
                    .Where(h => h.Activo)
                    .OrderBy(h => h.Hora),
                "IdHora",
                "Etiqueta",
                bloqueoHorario.IdHora
            );

            return View(bloqueoHorario);
        }

        // GET: BloqueoHorarios/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var bloqueoHorario = await _context.BloqueosHorarios.FindAsync(id);
            if (bloqueoHorario == null) return NotFound();

            var horas = await _context.HorasReserva
                .AsNoTracking()
                .Where(h => h.Activo || h.IdHora == bloqueoHorario.IdHora) // ✅ incluye la seleccionada aunque esté inactiva
                .OrderBy(h => h.Hora)
                .ToListAsync();

            ViewData["IdHora"] = new SelectList(horas, "IdHora", "Etiqueta", bloqueoHorario.IdHora);

            return View(bloqueoHorario);
        }

        // POST: BloqueoHorarios/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("IdBloqueoHorario,Fecha,IdHora,Motivo,Activo")] BloqueoHorario bloqueoHorario)
        {
            if (id != bloqueoHorario.IdBloqueoHorario) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(bloqueoHorario);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BloqueoHorarioExists(bloqueoHorario.IdBloqueoHorario))
                        return NotFound();
                    throw;
                }

                return RedirectToAction(nameof(Index));
            }

            var horas = await _context.HorasReserva
                .AsNoTracking()
                .Where(h => h.Activo || h.IdHora == bloqueoHorario.IdHora) // ✅
                .OrderBy(h => h.Hora)
                .ToListAsync();

            ViewData["IdHora"] = new SelectList(horas, "IdHora", "Etiqueta", bloqueoHorario.IdHora);

            return View(bloqueoHorario);
        }

        // GET: BloqueoHorarios/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var bloqueoHorario = await _context.BloqueosHorarios
                .Include(b => b.HoraReserva)
                .FirstOrDefaultAsync(m => m.IdBloqueoHorario == id);
            if (bloqueoHorario == null)
            {
                return NotFound();
            }

            return View(bloqueoHorario);
        }

        // POST: BloqueoHorarios/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var bloqueoHorario = await _context.BloqueosHorarios.FindAsync(id);
            if (bloqueoHorario != null)
            {
                _context.BloqueosHorarios.Remove(bloqueoHorario);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool BloqueoHorarioExists(int id)
        {
            return _context.BloqueosHorarios.Any(e => e.IdBloqueoHorario == id);
        }
    }
}
