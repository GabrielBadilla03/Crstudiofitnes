using CrStudioFitnes.Data;
using CrStudioFitnes.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CrStudioFitnes.Controllers
{
    public class HistorialController : Controller
    {
        private const string ROL_ADMIN = "Administrador";
        private const string ROL_ENTRENADOR = "Entrenador";
        private const string ROL_USUARIO = "Usuario";

        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public HistorialController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private bool PuedeGestionarHistorial()
        {
            return User.IsInRole(ROL_ADMIN)
                || User.IsInRole(ROL_ENTRENADOR);
        }

        private void CargarFrecuencias(TipoPlanDias? selected = null)
        {
            ViewBag.Frecuencias = Enum
                .GetValues(typeof(TipoPlanDias))
                .Cast<TipoPlanDias>()
                .Where(v => v != TipoPlanDias.ClasesExtra)
                .Select(v => new SelectListItem
                {
                    Value = v.ToString(),
                    Text = v.ToString(),
                    Selected = selected.HasValue && v == selected.Value
                })
                .ToList();
        }

        private async Task<string> GetUserTextAsync(string? idUsuario)
        {
            idUsuario = (idUsuario ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(idUsuario))
                return string.Empty;

            var u = await _context.Users
                .AsNoTracking()
                .Where(x => x.Id == idUsuario)
                .Select(x => new
                {
                    x.Cedula,
                    x.Nombre,
                    x.Apellidos,
                    x.Email
                })
                .FirstOrDefaultAsync();

            if (u == null)
                return string.Empty;

            var nombre = $"{u.Nombre} {u.Apellidos}".Trim();
            var cedula = string.IsNullOrWhiteSpace(u.Cedula)
                ? "—"
                : u.Cedula;

            return $"{cedula} · {nombre}"
                + (string.IsNullOrWhiteSpace(u.Email)
                    ? string.Empty
                    : $" · {u.Email}");
        }

        private async Task<bool> PuedeVerHistorialAsync(string idUsuario)
        {
            if (PuedeGestionarHistorial())
                return true;

            var userId = _userManager.GetUserId(User);

            return !string.IsNullOrWhiteSpace(userId)
                && string.Equals(
                    idUsuario,
                    userId,
                    StringComparison.Ordinal);
        }

        [Authorize(Roles = ROL_USUARIO + "," + ROL_ADMIN + "," + ROL_ENTRENADOR)]
        public async Task<IActionResult> Index(string? idUsuario)
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(userId))
                return Challenge();

            if (!PuedeGestionarHistorial())
            {
                idUsuario = userId;
            }
            else
            {
                idUsuario = (idUsuario ?? string.Empty).Trim();
            }

            ViewData["Title"] = "Historiales";
            ViewData["CurrentIdUsuario"] = idUsuario;
            ViewData["CanManageHistorial"] = PuedeGestionarHistorial();

            ViewBag.SelectedUserText = await GetUserTextAsync(idUsuario);

            if (string.IsNullOrWhiteSpace(idUsuario))
                return View(new List<Historial>());

            var usuarioExiste = await _context.Users
                .AsNoTracking()
                .AnyAsync(u => u.Id == idUsuario);

            if (!usuarioExiste)
                return NotFound();

            var historiales = await _context.Historiales
                .AsNoTracking()
                .Include(h => h.Usuario)
                .Where(h => h.IdUsuario == idUsuario)
                .OrderByDescending(h => h.FechaInicio)
                .ThenByDescending(h => h.IdHistorial)
                .ToListAsync();

            return View(historiales);
        }

        [Authorize(Roles = ROL_USUARIO + "," + ROL_ADMIN + "," + ROL_ENTRENADOR)]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var historial = await _context.Historiales
                .AsNoTracking()
                .Include(h => h.Usuario)
                .Include(h => h.Pesajes)
                    .ThenInclude(p => p.MedidasCuerpo)
                        .ThenInclude(mc => mc.Cuerpo)
                .FirstOrDefaultAsync(h => h.IdHistorial == id.Value);

            if (historial == null)
                return NotFound();

            if (!await PuedeVerHistorialAsync(historial.IdUsuario))
                return Forbid();

            ViewData["CanManageHistorial"] = PuedeGestionarHistorial();

            ViewBag.Cuerpos = PuedeGestionarHistorial()
                ? await _context.Cuerpos
                    .AsNoTracking()
                    .OrderBy(c => c.Nombre)
                    .ToListAsync()
                : new List<Cuerpo>();

            return View(historial);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = ROL_ADMIN + "," + ROL_ENTRENADOR)]
        public async Task<IActionResult> AgregarPesaje(
            int idHistorial,
            DateTime fecha,
            decimal peso)
        {
            if (peso < 1 || peso > 500)
            {
                TempData["Error"] = "El peso debe estar entre 1 y 500.";
                return RedirectToAction(nameof(Details), new { id = idHistorial });
            }

            var historial = await _context.Historiales
                .AsNoTracking()
                .FirstOrDefaultAsync(h => h.IdHistorial == idHistorial);

            if (historial == null)
                return NotFound();

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

        [HttpGet]
        [Authorize(Roles = ROL_USUARIO + "," + ROL_ADMIN + "," + ROL_ENTRENADOR)]
        public async Task<IActionResult> PesajeMedidas(int idPesaje)
        {
            var pesaje = await _context.Pesajes
                .AsNoTracking()
                .Where(p => p.IdPesaje == idPesaje)
                .Select(p => new
                {
                    p.IdPesaje,
                    p.IdHistorial,
                    p.Fecha,
                    p.Peso,
                    IdUsuario = p.Historial.IdUsuario
                })
                .FirstOrDefaultAsync();

            if (pesaje == null)
                return NotFound();

            if (!await PuedeVerHistorialAsync(pesaje.IdUsuario))
                return Forbid();

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

            return Json(new
            {
                ok = true,
                pesaje = new
                {
                    pesaje.IdPesaje,
                    pesaje.IdHistorial,
                    pesaje.Fecha,
                    pesaje.Peso
                },
                items
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = ROL_ADMIN + "," + ROL_ENTRENADOR)]
        public async Task<IActionResult> GuardarPesajeCuerpo(
            int idPesaje,
            int idCuerpo,
            decimal medida)
        {
            if (idPesaje <= 0 || idCuerpo <= 0)
            {
                return BadRequest(new
                {
                    ok = false,
                    message = "Datos inválidos."
                });
            }

            if (medida < 0 || medida > 500)
            {
                return BadRequest(new
                {
                    ok = false,
                    message = "Medida fuera de rango (0-500)."
                });
            }

            var existePesaje = await _context.Pesajes
                .AnyAsync(p => p.IdPesaje == idPesaje);

            if (!existePesaje)
            {
                return NotFound(new
                {
                    ok = false,
                    message = "Pesaje no existe."
                });
            }

            var existeCuerpo = await _context.Cuerpos
                .AnyAsync(c => c.IdCuerpo == idCuerpo);

            if (!existeCuerpo)
            {
                return NotFound(new
                {
                    ok = false,
                    message = "Medida (Cuerpo) no existe."
                });
            }

            var row = await _context.PesajesCuerpo
                .FindAsync(idPesaje, idCuerpo);

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
            }

            await _context.SaveChangesAsync();

            return Json(new { ok = true });
        }

        [Authorize(Roles = ROL_ADMIN + "," + ROL_ENTRENADOR)]
        public async Task<IActionResult> Create(string? idUsuario)
        {
            idUsuario = (idUsuario ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(idUsuario))
                return BadRequest();

            var usuarioExiste = await _context.Users
                .AsNoTracking()
                .AnyAsync(u => u.Id == idUsuario);

            if (!usuarioExiste)
                return NotFound();

            var model = new Historial
            {
                IdUsuario = idUsuario,
                FechaInicio = DateTime.Today
            };

            ViewBag.SelectedUserText = await GetUserTextAsync(idUsuario);
            CargarFrecuencias(model.Frecuencia);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = ROL_ADMIN + "," + ROL_ENTRENADOR)]
        public async Task<IActionResult> Create(
            [Bind("IdUsuario,FechaInicio,FechaFin,Estatura,Peso,Edad,Estado,Actividad,Frecuencia,Objetivo")]
            Historial historial)
        {
            historial.IdUsuario = (historial.IdUsuario ?? string.Empty).Trim();

            var usuarioExiste = await _context.Users
                .AsNoTracking()
                .AnyAsync(u => u.Id == historial.IdUsuario);

            if (!usuarioExiste)
                ModelState.AddModelError("IdUsuario", "El usuario seleccionado no existe.");

            if (historial.FechaFin.HasValue
                && historial.FechaFin.Value.Date < historial.FechaInicio.Date)
            {
                ModelState.AddModelError(
                    "FechaFin",
                    "La fecha final no puede ser anterior a la fecha inicial.");
            }

            if (ModelState.IsValid)
            {
                _context.Historiales.Add(historial);
                await _context.SaveChangesAsync();

                return RedirectToAction(
                    nameof(Index),
                    new { idUsuario = historial.IdUsuario });
            }

            ViewBag.SelectedUserText =
                await GetUserTextAsync(historial.IdUsuario);

            CargarFrecuencias(historial.Frecuencia);

            return View(historial);
        }

        [Authorize(Roles = ROL_ADMIN + "," + ROL_ENTRENADOR)]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var historial = await _context.Historiales
                .AsNoTracking()
                .Include(h => h.Usuario)
                .FirstOrDefaultAsync(h => h.IdHistorial == id.Value);

            if (historial == null)
                return NotFound();

            return View(historial);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = ROL_ADMIN + "," + ROL_ENTRENADOR)]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var historial = await _context.Historiales
                .FirstOrDefaultAsync(h => h.IdHistorial == id);

            if (historial == null)
                return NotFound();

            var idUsuario = historial.IdUsuario;

            _context.Historiales.Remove(historial);
            await _context.SaveChangesAsync();

            return RedirectToAction(
                nameof(Index),
                new { idUsuario });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = ROL_ADMIN + "," + ROL_ENTRENADOR)]
        public async Task<IActionResult> EliminarPesajeCuerpo(
            int idPesaje,
            int idCuerpo)
        {
            if (idPesaje <= 0 || idCuerpo <= 0)
            {
                return BadRequest(new
                {
                    ok = false,
                    message = "Datos inválidos."
                });
            }

            var row = await _context.PesajesCuerpo
                .FindAsync(idPesaje, idCuerpo);

            if (row == null)
            {
                return NotFound(new
                {
                    ok = false,
                    message = "La medida no existe."
                });
            }

            _context.PesajesCuerpo.Remove(row);
            await _context.SaveChangesAsync();

            return Json(new { ok = true });
        }
    }
}
