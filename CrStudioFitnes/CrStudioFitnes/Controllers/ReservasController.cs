using CrStudioFitnes.Data;
using CrStudioFitnes.Models;
using CrStudioFitnes.Views.Reservas;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using Microsoft.AspNetCore.Authorization;


namespace CrStudioFitnes.Controllers
{
    public class ReservasController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        private const int MAX_POR_HORA = 6;

        public ReservasController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [Authorize(Roles = "Usuario")]
        // ✅ INDEX = CALENDARIO
        public async Task<IActionResult> Index(int? year, int? month)
        {
            var hoy = DateTime.Today;
            int y = year ?? hoy.Year;
            int m = month ?? hoy.Month;

            var monthStart = new DateTime(y, m, 1);

            int diff = ((int)monthStart.DayOfWeek - (int)DayOfWeek.Monday);
            if (diff < 0) diff += 7;
            var gridStart = monthStart.AddDays(-diff);
            var gridEnd = gridStart.AddDays(42);

            // 1) Horas activas del catálogo (IDs)
            var horasActivasIds = await _context.HorasReserva
                .AsNoTracking()
                .Where(h => h.Activo)
                .OrderBy(h => h.Hora)
                .Select(h => h.IdHora)
                .ToListAsync();

            // 2) Bloqueos por día completo
            var bloqueosDia = await _context.BloqueosHorarios
                .AsNoTracking()
                .Where(b => b.Activo
                    && b.Fecha != null
                    && b.IdHora == null
                    && b.Fecha.Value >= gridStart
                    && b.Fecha.Value < gridEnd)
                .Select(b => b.Fecha!.Value.Date)
                .ToListAsync();

            var setBloqueosDia = bloqueosDia.ToHashSet();

            // 3) Bloqueos globales por hora (para todos los días)
            var bloqueosGlobales = await _context.BloqueosHorarios
                .AsNoTracking()
                .Where(b => b.Activo && b.Fecha == null && b.IdHora != null)
                .Select(b => b.IdHora!.Value)
                .Distinct()
                .ToListAsync();

            var setGlobal = bloqueosGlobales.ToHashSet();

            // 4) Bloqueos por día/hora dentro del rango
            var bloqueosPorDiaHora = await _context.BloqueosHorarios
                .AsNoTracking()
                .Where(b => b.Activo
                    && b.Fecha != null
                    && b.IdHora != null
                    && b.Fecha.Value >= gridStart
                    && b.Fecha.Value < gridEnd)
                .Select(b => new { Fecha = b.Fecha!.Value.Date, IdHora = b.IdHora!.Value })
                .ToListAsync();

            var dictBloqDiaHora = bloqueosPorDiaHora
                .GroupBy(x => x.Fecha)
                .ToDictionary(g => g.Key, g => g.Select(x => x.IdHora).ToHashSet());

            // 5) Conteo de reservas por (día, hora)
            var reservasCounts = await _context.Reservas
                .AsNoTracking()
                .Where(r => r.Fecha >= gridStart && r.Fecha < gridEnd)
                .GroupBy(r => new { Fecha = r.Fecha.Date, r.IdHora })
                .Select(g => new { g.Key.Fecha, g.Key.IdHora, Count = g.Count() })
                .ToListAsync();

            var dictResPorDiaHora = reservasCounts.ToDictionary(
                x => (x.Fecha, x.IdHora),
                x => x.Count
            );

            var dictResPorDia = reservasCounts
                .GroupBy(x => x.Fecha)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Count));

            // VM
            var cultura = CultureInfo.GetCultureInfo("es-CR");
            var vm = new ReservasCalendarioVM
            {
                Year = y,
                Month = m,
                MonthLabel = $"{cultura.DateTimeFormat.GetMonthName(m)} {y}".ToUpperInvariant()
            };

            for (int i = 0; i < 42; i++)
            {
                var d = gridStart.AddDays(i).Date;

                bool isBlocked = setBloqueosDia.Contains(d);

                // ✅ si no está bloqueado por admin, revisa si el día está FULL
                if (!isBlocked)
                {
                    var bloqueadasEseDia = dictBloqDiaHora.TryGetValue(d, out var hs) ? hs : new HashSet<int>();

                    bool hayAlgunaDisponible = false;

                    foreach (var idHora in horasActivasIds)
                    {
                        if (setGlobal.Contains(idHora)) continue;
                        if (bloqueadasEseDia.Contains(idHora)) continue;

                        var key = (d, idHora);
                        int count = dictResPorDiaHora.TryGetValue(key, out var c) ? c : 0;

                        if (count < MAX_POR_HORA)
                        {
                            hayAlgunaDisponible = true;
                            break;
                        }
                    }

                    // si no hay ninguna disponible => día lleno => deshabilitar
                    if (!hayAlgunaDisponible)
                        isBlocked = true;
                }

                vm.Days.Add(new DiaCalendarioVM
                {
                    Date = d,
                    IsCurrentMonth = (d.Month == m),
                    IsToday = (d == hoy),
                    IsBlockedDay = isBlocked, // ✅ bloqueado o lleno
                    ReservasCount = dictResPorDia.TryGetValue(d, out var total) ? total : 0
                });
            }

            return View(vm);
        }


        // ✅ API: devuelve horas para un día (marcando disponible / bloqueada / reservada)
        [HttpGet]
        public async Task<IActionResult> GetHorasDisponibles(string date)
        {
            if (string.IsNullOrWhiteSpace(date) ||
                !DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var fecha))
            {
                return BadRequest("Fecha inválida.");
            }

            fecha = fecha.Date;

            // 🔐 Usuario actual (para marcar sus horas)
            var userId = _userManager.GetUserId(User);

            // Día completo bloqueado?
            bool blockedDay = await _context.BloqueosHorarios
                .AsNoTracking()
                .AnyAsync(b => b.Activo && b.Fecha != null && b.IdHora == null && b.Fecha.Value == fecha);

            if (blockedDay)
            {
                return Json(new { blockedDay = true, date = fecha.ToString("yyyy-MM-dd"), horas = Array.Empty<object>() });
            }

            // Horas activas
            var horasActivas = await _context.HorasReserva
                .AsNoTracking()
                .Where(h => h.Activo)
                .OrderBy(h => h.Hora)
                .Select(h => new { h.IdHora, h.Hora, h.Etiqueta })
                .ToListAsync();

            // Bloqueos globales
            var bloqueosGlobales = await _context.BloqueosHorarios
                .AsNoTracking()
                .Where(b => b.Activo && b.Fecha == null && b.IdHora != null)
                .Select(b => b.IdHora!.Value)
                .Distinct()
                .ToListAsync();

            var setGlobal = bloqueosGlobales.ToHashSet();

            // Bloqueos por día/hora
            var bloqueosDelDia = await _context.BloqueosHorarios
                .AsNoTracking()
                .Where(b => b.Activo && b.Fecha != null && b.IdHora != null && b.Fecha.Value == fecha)
                .Select(b => b.IdHora!.Value)
                .Distinct()
                .ToListAsync();

            var setDia = bloqueosDelDia.ToHashSet();

            // ✅ Conteo de reservas por hora para ese día
            var counts = await _context.Reservas
                .AsNoTracking()
                .Where(r => r.Fecha == fecha)
                .GroupBy(r => r.IdHora)
                .Select(g => new { IdHora = g.Key, Count = g.Count() })
                .ToListAsync();

            var dictCount = counts.ToDictionary(x => x.IdHora, x => x.Count);

            // ✅ Horas reservadas por ESTE usuario en ese día
            var setMias = new HashSet<int>();
            if (!string.IsNullOrWhiteSpace(userId))
            {
                var mis = await _context.Reservas
                    .AsNoTracking()
                    .Where(r => r.IdUsuario == userId && r.Fecha == fecha)
                    .Select(r => r.IdHora)
                    .ToListAsync();

                setMias = mis.ToHashSet();
            }

            var result = horasActivas.Select(h =>
            {
                bool global = setGlobal.Contains(h.IdHora);
                bool dia = setDia.Contains(h.IdHora);

                int count = dictCount.TryGetValue(h.IdHora, out var c) ? c : 0;
                bool llena = count >= MAX_POR_HORA;

                bool esMia = setMias.Contains(h.IdHora);

                // Si ya es mía, NO la dejo como disponible (solo la pinto verde)
                bool disponible = !esMia && !(global || dia || llena);

                return new
                {
                    idHora = h.IdHora,
                    etiqueta = string.IsNullOrWhiteSpace(h.Etiqueta)
                        ? h.Hora.ToString(@"hh\:mm")
                        : h.Etiqueta,
                    disponible,
                    esMia
                };
            });

            return Json(new { blockedDay = false, date = fecha.ToString("yyyy-MM-dd"), horas = result });
        }

        [HttpGet]
        [Authorize(Roles = "Administrador,Entrenador")]
        public async Task<IActionResult> GetReservasPorHora(string date, int idHora)
        {
            if (string.IsNullOrWhiteSpace(date) ||
                !DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var fecha))
            {
                return BadRequest("Fecha inválida.");
            }

            fecha = fecha.Date;

            var hora = await _context.HorasReserva
                .AsNoTracking()
                .Where(h => h.IdHora == idHora)
                .Select(h => new { h.IdHora, h.Etiqueta, h.Hora })
                .FirstOrDefaultAsync();

            if (hora == null)
                return NotFound("Hora no existe.");

            var reservas = await _context.Reservas
                .AsNoTracking()
                .Where(r => r.Fecha == fecha && r.IdHora == idHora)
                .Join(_context.Users.AsNoTracking(),
                    r => r.IdUsuario,
                    u => u.Id,
                    (r, u) => new
                    {
                        Nombre = (u.Nombre + " " + u.Apellidos).Trim(),
                        u.Email,
                        Telefono = u.TelefonoPersonal
                    })
                .OrderBy(x => x.Nombre)
                .ToListAsync();

            var horaEtiqueta = string.IsNullOrWhiteSpace(hora.Etiqueta)
                ? hora.Hora.ToString(@"hh\:mm")
                : hora.Etiqueta;

            return Json(new
            {
                date = fecha.ToString("yyyy-MM-dd"),
                idHora,
                horaEtiqueta,
                total = reservas.Count,
                reservas = reservas.Select(x => new
                {
                    nombre = string.IsNullOrWhiteSpace(x.Nombre) ? "Usuario" : x.Nombre,
                    email = x.Email ?? "",
                    telefono = x.Telefono ?? ""
                })
            });
        }



        // ✅ POST: crear reserva desde el calendario
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Usuario")]
        public async Task<IActionResult> CrearDesdeCalendario(string date, int idHora)
        {
            if (string.IsNullOrWhiteSpace(date) ||
                !DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var fecha))
            {
                TempData["Error"] = "Fecha inválida.";
                return RedirectToAction(nameof(Index));
            }

            fecha = fecha.Date;

            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(userId))
            {
                TempData["Error"] = "No se pudo identificar el usuario.";
                return RedirectToAction(nameof(Index), new { year = fecha.Year, month = fecha.Month });
            }

            var redirect = new { year = fecha.Year, month = fecha.Month };

            var strategy = _context.Database.CreateExecutionStrategy();
            IActionResult? result = null;

            try
            {
                await strategy.ExecuteAsync(async () =>
                {
                    await using var tx = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

                    // Hora activa?
                    var hora = await _context.HorasReserva
                        .FirstOrDefaultAsync(h => h.IdHora == idHora && h.Activo);

                    if (hora == null)
                    {
                        TempData["Error"] = "La hora seleccionada no está disponible.";
                        result = RedirectToAction(nameof(Index), redirect);
                        return;
                    }

                    // ✅ No permitir reservar en horas que ya pasaron
                    var ahora = DateTime.Now;

                    // fecha pasada (cualquier hora)
                    if (fecha.Date < ahora.Date)
                    {
                        TempData["Error"] = "No se puede reservar en fechas pasadas.";
                        result = RedirectToAction(nameof(Index), redirect);
                        return;
                    }

                    // si es HOY, validar la hora exacta
                    if (fecha.Date == ahora.Date)
                    {
                        var fechaHoraSlot = fecha.Date.Add(hora.Hora); // hora.Hora es TimeSpan
                        if (fechaHoraSlot <= ahora.AddMinutes(10))
                        {
                            TempData["Error"] = "No se puede reservar una hora que ya pasó.";
                            result = RedirectToAction(nameof(Index), redirect);
                            return;
                        }
                    }

                    // Día bloqueado?
                    bool blockedDay = await _context.BloqueosHorarios
                        .AnyAsync(b => b.Activo && b.Fecha != null && b.IdHora == null && b.Fecha.Value == fecha);

                    if (blockedDay)
                    {
                        TempData["Error"] = "Ese día está bloqueado.";
                        result = RedirectToAction(nameof(Index), redirect);
                        return;
                    }

                    // Hora bloqueada (global o por día)?
                    bool blockedHour = await _context.BloqueosHorarios
                        .AnyAsync(b =>
                            b.Activo && b.IdHora != null && b.IdHora.Value == idHora &&
                            (b.Fecha == null || b.Fecha.Value == fecha));

                    if (blockedHour)
                    {
                        TempData["Error"] = "Esa hora está bloqueada.";
                        result = RedirectToAction(nameof(Index), redirect);
                        return;
                    }

                    // (Opcional pero recomendado) evitar duplicado del mismo usuario en la misma hora
                    bool yaTengo = await _context.Reservas
                        .AnyAsync(r => r.IdUsuario == userId && r.Fecha == fecha && r.IdHora == idHora);

                    if (yaTengo)
                    {
                        TempData["Error"] = "Ya tenés una reserva en esa hora.";
                        result = RedirectToAction(nameof(Index), redirect);
                        return;
                    }

                    // Capacidad: máximo 6 por (fecha, hora)
                    int count = await _context.Reservas
                        .Where(r => r.Fecha == fecha && r.IdHora == idHora)
                        .CountAsync();

                    if (count >= MAX_POR_HORA)
                    {
                        TempData["Error"] = "Esa hora ya está llena.";
                        result = RedirectToAction(nameof(Index), redirect);
                        return;
                    }

                    // Paquete activo con lecciones
                    var paqueteActivo = await _context.PaquetesUsuario
                        .Where(pu =>
                            pu.IdUsuario == userId &&
                            pu.CantLecciones > 0 &&
                            pu.FechaInicio.Date <= fecha &&
                            pu.FechaFin.Date >= fecha)
                        .OrderBy(pu => pu.FechaFin)
                        .FirstOrDefaultAsync();

                    if (paqueteActivo == null)
                    {
                        TempData["Error"] = "No tenés lecciones disponibles o tu paquete no está vigente.";
                        result = RedirectToAction(nameof(Index), redirect);
                        return;
                    }

                    // Crear reserva + rebajar lección
                    _context.Reservas.Add(new Reserva
                    {
                        IdUsuario = userId,
                        Fecha = fecha,
                        IdHora = idHora
                    });

                    paqueteActivo.CantLecciones -= 1;

                    await _context.SaveChangesAsync();
                    await tx.CommitAsync();

                    TempData["Ok"] = "Reserva creada correctamente.";
                    result = RedirectToAction(nameof(Index), redirect);
                });

                return result ?? RedirectToAction(nameof(Index), redirect);
            }
            catch
            {
                TempData["Error"] = "Ocurrió un error creando la reserva.";
                return RedirectToAction(nameof(Index), redirect);
            }
        }
    }
}
