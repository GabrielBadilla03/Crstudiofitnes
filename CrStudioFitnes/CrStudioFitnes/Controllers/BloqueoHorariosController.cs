using CrStudioFitnes.Data;
using CrStudioFitnes.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

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

        public async Task<IActionResult> Index()
        {
            var data = await _context.BloqueosHorarios
                .AsNoTracking()
                .Where(b => b.Activo)
                .Include(b => b.HoraReserva)
                .OrderBy(b => b.Fecha)
                .ThenBy(b => b.HoraReserva != null
                    ? b.HoraReserva.Hora
                    : TimeSpan.Zero)
                .ToListAsync();

            return View(data);
        }

        public async Task<IActionResult> Create()
        {
            await CargarHorasAsync();
            return View(new BloqueoHorario { Activo = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("IdBloqueoHorario,Fecha,IdHora,Motivo,Activo")]
            BloqueoHorario bloqueoHorario)
        {
            NormalizarBloqueo(bloqueoHorario);

            await ValidarBloqueoAsync(
                bloqueoHorario,
                excluirId: null);

            if (ModelState.IsValid)
            {
                bloqueoHorario.Activo = true;

                try
                {
                    _context.BloqueosHorarios.Add(bloqueoHorario);
                    await _context.SaveChangesAsync();

                    TempData["Ok"] = "Bloqueo creado correctamente.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "No se pudo guardar el bloqueo. Ya existe un bloqueo activo equivalente o los datos entran en conflicto.");
                }
            }

            await CargarHorasAsync(bloqueoHorario.IdHora);
            return View(bloqueoHorario);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var bloqueoHorario = await _context.BloqueosHorarios
                .FirstOrDefaultAsync(b => b.IdBloqueoHorario == id.Value);

            if (bloqueoHorario == null)
                return NotFound();

            await CargarHorasAsync(
                bloqueoHorario.IdHora,
                incluirHoraId: bloqueoHorario.IdHora);

            return View(bloqueoHorario);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("IdBloqueoHorario,Fecha,IdHora,Motivo,Activo")]
            BloqueoHorario bloqueoHorario)
        {
            if (id != bloqueoHorario.IdBloqueoHorario)
                return NotFound();

            NormalizarBloqueo(bloqueoHorario);

            await ValidarBloqueoAsync(
                bloqueoHorario,
                excluirId: bloqueoHorario.IdBloqueoHorario);

            if (ModelState.IsValid)
            {
                var existente = await _context.BloqueosHorarios
                    .FirstOrDefaultAsync(
                        b => b.IdBloqueoHorario == id);

                if (existente == null)
                    return NotFound();

                existente.Fecha = bloqueoHorario.Fecha;
                existente.IdHora = bloqueoHorario.IdHora;
                existente.Motivo = bloqueoHorario.Motivo;
                existente.Activo = bloqueoHorario.Activo;

                try
                {
                    await _context.SaveChangesAsync();

                    TempData["Ok"] = "Bloqueo actualizado correctamente.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BloqueoHorarioExists(id))
                        return NotFound();

                    throw;
                }
                catch (DbUpdateException)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "No se pudo guardar el bloqueo. Ya existe un bloqueo activo equivalente o los datos entran en conflicto.");
                }
            }

            await CargarHorasAsync(
                bloqueoHorario.IdHora,
                incluirHoraId: bloqueoHorario.IdHora);

            return View(bloqueoHorario);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var bloqueoHorario = await _context.BloqueosHorarios
                .AsNoTracking()
                .Include(b => b.HoraReserva)
                .FirstOrDefaultAsync(
                    b => b.IdBloqueoHorario == id.Value);

            if (bloqueoHorario == null)
                return NotFound();

            return View(bloqueoHorario);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var bloqueoHorario = await _context.BloqueosHorarios
                .FirstOrDefaultAsync(
                    b => b.IdBloqueoHorario == id);

            if (bloqueoHorario == null)
                return NotFound();

            // Baja lógica para conservar trazabilidad.
            bloqueoHorario.Activo = false;
            await _context.SaveChangesAsync();

            TempData["Ok"] = "Bloqueo desactivado correctamente.";

            return RedirectToAction(nameof(Index));
        }

        private async Task ValidarBloqueoAsync(
            BloqueoHorario bloqueo,
            int? excluirId)
        {
            if (!bloqueo.Fecha.HasValue && !bloqueo.IdHora.HasValue)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Debés indicar una fecha, una hora o ambas.");
                return;
            }

            if (bloqueo.Fecha.HasValue)
                bloqueo.Fecha = bloqueo.Fecha.Value.Date;

            IQueryable<BloqueoHorario> query =
                _context.BloqueosHorarios
                    .AsNoTracking()
                    .Where(b => b.Activo);

            if (excluirId.HasValue)
            {
                query = query.Where(
                    b => b.IdBloqueoHorario != excluirId.Value);
            }

            bool existe;

            if (bloqueo.Fecha.HasValue && !bloqueo.IdHora.HasValue)
            {
                var fecha = bloqueo.Fecha.Value.Date;

                existe = await query.AnyAsync(b =>
                    b.Fecha != null
                    && b.Fecha.Value == fecha
                    && b.IdHora == null);
            }
            else if (!bloqueo.Fecha.HasValue && bloqueo.IdHora.HasValue)
            {
                int idHora = bloqueo.IdHora.Value;

                existe = await query.AnyAsync(b =>
                    b.Fecha == null
                    && b.IdHora == idHora);
            }
            else
            {
                var fecha = bloqueo.Fecha!.Value.Date;
                int idHora = bloqueo.IdHora!.Value;

                existe = await query.AnyAsync(b =>
                    b.Fecha != null
                    && b.Fecha.Value == fecha
                    && b.IdHora == idHora);
            }

            if (existe)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Ya existe un bloqueo activo con la misma fecha/hora.");
            }
        }

        private static void NormalizarBloqueo(
            BloqueoHorario bloqueo)
        {
            if (bloqueo.Fecha.HasValue)
                bloqueo.Fecha = bloqueo.Fecha.Value.Date;

            bloqueo.Motivo = string.IsNullOrWhiteSpace(bloqueo.Motivo)
                ? null
                : bloqueo.Motivo.Trim();
        }

        private async Task CargarHorasAsync(
            int? selectedId = null,
            int? incluirHoraId = null)
        {
            var horas = await _context.HorasReserva
                .AsNoTracking()
                .Where(h =>
                    h.Activo
                    || (incluirHoraId.HasValue
                        && h.IdHora == incluirHoraId.Value))
                .OrderBy(h => h.Hora)
                .ToListAsync();

            ViewData["IdHora"] = new SelectList(
                horas,
                "IdHora",
                "Etiqueta",
                selectedId);
        }

        private bool BloqueoHorarioExists(int id)
        {
            return _context.BloqueosHorarios
                .Any(e => e.IdBloqueoHorario == id);
        }
    }
}
