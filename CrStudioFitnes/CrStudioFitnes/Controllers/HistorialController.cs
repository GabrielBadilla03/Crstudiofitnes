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
    public class HistorialController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HistorialController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ===============================
        // Helpers
        // ===============================
        private void CargarFrecuencias(TipoPlanDias? selected = null)
        {
            // Dropdown para Frecuencia (enum TipoPlanDias)
            ViewBag.Frecuencias = Enum.GetValues(typeof(TipoPlanDias))
                .Cast<TipoPlanDias>()
                .Select(v => new SelectListItem
                {
                    Value = v.ToString(), // guardará como string si usás HasConversion<string>()
                    Text = v.ToString(),
                    Selected = selected.HasValue && v == selected.Value
                })
                .ToList();
        }

        private async Task<string> GetUserTextAsync(string? idUsuario)
        {
            idUsuario = (idUsuario ?? "").Trim();
            if (string.IsNullOrWhiteSpace(idUsuario)) return "";

            var u = await _context.Users.AsNoTracking()
                .Where(x => x.Id == idUsuario)
                .Select(x => new { x.Cedula, x.Nombre, x.Apellidos, x.Email })
                .FirstOrDefaultAsync();

            if (u == null) return "";

            var nombre = $"{u.Nombre} {u.Apellidos}".Trim();
            var ced = string.IsNullOrWhiteSpace(u.Cedula) ? "—" : u.Cedula;
            var email = string.IsNullOrWhiteSpace(u.Email) ? "" : u.Email;

            return $"{ced} · {nombre}" + (string.IsNullOrWhiteSpace(email) ? "" : $" · {email}");
        }

        // GET: Historial
        [Authorize(Roles = "Usuario,Administrador,Entrenador")]
        public async Task<IActionResult> Index(string? idUsuario)
        {
            idUsuario = (idUsuario ?? "").Trim();

            ViewData["Title"] = "Historiales";
            ViewData["CurrentIdUsuario"] = idUsuario;

            ViewBag.SelectedUserText = await GetUserTextAsync(idUsuario);

            if (string.IsNullOrWhiteSpace(idUsuario))
                return View(new List<Historial>());

            var historiales = await _context.Historiales
                .AsNoTracking()
                .Include(h => h.Usuario)
                .Where(h => h.IdUsuario == idUsuario)
                .OrderByDescending(h => h.FechaInicio)
                .ToListAsync();

            return View(historiales);
        }

        // GET: Historial/Details/5
        [Authorize(Roles = "Usuario,Administrador,Entrenador")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var historial = await _context.Historiales
                .Include(h => h.Usuario)
                .Include(h => h.Pesajes)
                    .ThenInclude(p => p.MedidasCuerpo)
                        .ThenInclude(mc => mc.Cuerpo)
                .FirstOrDefaultAsync(m => m.IdHistorial == id);

            if (historial == null) return NotFound();

            ViewBag.Cuerpos = await _context.Cuerpos
                .AsNoTracking()
                .OrderBy(c => c.Nombre)
                .ToListAsync();

            return View(historial);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador,Entrenador")]
        public async Task<IActionResult> AgregarPesaje(int idHistorial, string idUsuario, DateTime fecha, decimal peso)
        {
            if (peso < 1 || peso > 500)
            {
                TempData["Error"] = "El peso debe estar entre 1 y 500.";
                return RedirectToAction(nameof(Details), new { id = idHistorial });
            }

            var existe = await _context.Historiales.AnyAsync(h => h.IdHistorial == idHistorial);
            if (!existe) return NotFound();

            var nuevo = new Pesaje
            {
                IdHistorial = idHistorial,
                Fecha = fecha.Date,
                Peso = peso
            };

            _context.Pesajes.Add(nuevo);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = idHistorial });
        }

        // AJAX: obtener medidas de un pesaje
        [HttpGet]
        [Authorize(Roles = "Usuario,Administrador,Entrenador")]
        public async Task<IActionResult> PesajeMedidas(int idPesaje)
        {
            var pesaje = await _context.Pesajes
                .AsNoTracking()
                .Where(p => p.IdPesaje == idPesaje)
                .Select(p => new { p.IdPesaje, p.IdHistorial, p.Fecha, p.Peso })
                .FirstOrDefaultAsync();

            if (pesaje == null) return NotFound();

            var items = await _context.PesajesCuerpo
                .AsNoTracking()
                .Where(pc => pc.IdPesaje == idPesaje)
                .OrderBy(pc => pc.Cuerpo.Nombre)
                .Select(pc => new
                {
                    idCuerpo = pc.IdCuerpo,
                    cuerpo = pc.Cuerpo.Nombre,
                    detalle = pc.Cuerpo.Detalle,
                    medida = pc.Medida
                })
                .ToListAsync();

            return Json(new { ok = true, pesaje, items });
        }

        // AJAX: agregar/actualizar una medida (upsert)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador,Entrenador")]
        public async Task<IActionResult> GuardarPesajeCuerpo(int idPesaje, int idCuerpo, decimal medida)
        {
            if (idPesaje <= 0 || idCuerpo <= 0) return BadRequest(new { ok = false, message = "Datos inválidos." });
            if (medida < 0 || medida > 500) return BadRequest(new { ok = false, message = "Medida fuera de rango (0-500)." });

            var existePesaje = await _context.Pesajes.AnyAsync(p => p.IdPesaje == idPesaje);
            if (!existePesaje) return NotFound(new { ok = false, message = "Pesaje no existe." });

            var existeCuerpo = await _context.Cuerpos.AnyAsync(c => c.IdCuerpo == idCuerpo);
            if (!existeCuerpo) return NotFound(new { ok = false, message = "Medida (Cuerpo) no existe." });

            var row = await _context.PesajesCuerpo.FindAsync(idPesaje, idCuerpo);
            if (row == null)
            {
                row = new PesajeCuerpo
                {
                    IdPesaje = idPesaje,
                    IdCuerpo = idCuerpo,
                    Medida = medida
                };
                _context.PesajesCuerpo.Add(row);
            }
            else
            {
                row.Medida = medida;
                _context.PesajesCuerpo.Update(row);
            }

            await _context.SaveChangesAsync();
            return Json(new { ok = true });
        }

        // ========= GET: Historial/Create =========
        [Authorize(Roles = "Administrador,Entrenador")]
        public async Task<IActionResult> Create(string? idUsuario)
        {
            idUsuario = (idUsuario ?? "").Trim();

            var model = new Historial
            {
                IdUsuario = idUsuario,
                FechaInicio = DateTime.Today
            };

            ViewBag.SelectedUserText = await GetUserTextAsync(idUsuario);

            // ✅ dropdown de Frecuencia (TipoPlanDias)
            CargarFrecuencias(model.Frecuencia);

            return View(model);
        }

        // ========= POST: Historial/Create =========
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador,Entrenador")]
        public async Task<IActionResult> Create([Bind("IdUsuario,FechaInicio,FechaFin,Estatura,Peso,Edad,Estado,Actividad,Frecuencia,Objetivo")] Historial historial)
        {
            historial.IdUsuario = (historial.IdUsuario ?? "").Trim();

            if (ModelState.IsValid)
            {
                _context.Add(historial);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index), new { idUsuario = historial.IdUsuario });
            }

            ViewBag.SelectedUserText = await GetUserTextAsync(historial.IdUsuario);

            // ✅ si falla validación, recargar dropdown
            CargarFrecuencias(historial.Frecuencia);

            return View(historial);
        }

        // GET: Historial/Delete/5
        [Authorize(Roles = "Administrador,Entrenador")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var historial = await _context.Historiales
                .Include(h => h.Usuario)
                .FirstOrDefaultAsync(m => m.IdHistorial == id);

            if (historial == null) return NotFound();

            return View(historial);
        }

        // POST: Historial/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador,Entrenador")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var historial = await _context.Historiales.FindAsync(id);
            if (historial != null)
                _context.Historiales.Remove(historial);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool HistorialExists(int id) => _context.Historiales.Any(e => e.IdHistorial == id);

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarPesajeCuerpo(int idPesaje, int idCuerpo)
        {
            if (idPesaje <= 0 || idCuerpo <= 0)
                return BadRequest(new { ok = false, message = "Datos inválidos." });

            var row = await _context.PesajesCuerpo.FindAsync(idPesaje, idCuerpo);
            if (row == null)
                return NotFound(new { ok = false, message = "La medida no existe." });

            _context.PesajesCuerpo.Remove(row);
            await _context.SaveChangesAsync();

            return Json(new { ok = true });
        }
    }
}
