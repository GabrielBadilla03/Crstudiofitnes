using CrStudioFitnes.Data;
using CrStudioFitnes.Models;
using CrStudioFitnes.Views.Reservas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Globalization;

namespace CrStudioFitnes.Controllers
{
    public class ReservasController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        private const int MAX_POR_HORA = 6;
        private const int MAX_RESULTADOS_USUARIOS = 15;
        private const int MINUTOS_ANTICIPACION_RESERVA = 10;

        public ReservasController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // =========================================================
        // CALENDARIO
        // =========================================================
        [Authorize(Roles = "Usuario,Administrador,Entrenador")]
        public async Task<IActionResult> Index(int? year, int? month)
        {
            var hoy = DateTime.Today;

            var y = year ?? hoy.Year;
            var m = month ?? hoy.Month;

            if (m < 1 || m > 12 || y < 1900 || y > 9999)
            {
                y = hoy.Year;
                m = hoy.Month;
            }

            var monthStart = new DateTime(y, m, 1);

            var diff = (int)monthStart.DayOfWeek - (int)DayOfWeek.Monday;
            if (diff < 0)
                diff += 7;

            var gridStart = monthStart.AddDays(-diff);
            var gridEnd = gridStart.AddDays(42);

            var horasActivasIds = await _context.HorasReserva
                .AsNoTracking()
                .Where(h => h.Activo)
                .OrderBy(h => h.Hora)
                .Select(h => h.IdHora)
                .ToListAsync();

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

            var bloqueosGlobales = await _context.BloqueosHorarios
                .AsNoTracking()
                .Where(b => b.Activo && b.Fecha == null && b.IdHora != null)
                .Select(b => b.IdHora!.Value)
                .Distinct()
                .ToListAsync();

            var setGlobal = bloqueosGlobales.ToHashSet();

            var bloqueosPorDiaHora = await _context.BloqueosHorarios
                .AsNoTracking()
                .Where(b => b.Activo
                    && b.Fecha != null
                    && b.IdHora != null
                    && b.Fecha.Value >= gridStart
                    && b.Fecha.Value < gridEnd)
                .Select(b => new
                {
                    Fecha = b.Fecha!.Value.Date,
                    IdHora = b.IdHora!.Value
                })
                .ToListAsync();

            var dictBloqDiaHora = bloqueosPorDiaHora
                .GroupBy(x => x.Fecha)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => x.IdHora).ToHashSet());

            var reservasCounts = await _context.Reservas
                .AsNoTracking()
                .Where(r => r.Activa
                    && r.Fecha >= gridStart
                    && r.Fecha < gridEnd)
                .GroupBy(r => new
                {
                    Fecha = r.Fecha.Date,
                    r.IdHora
                })
                .Select(g => new
                {
                    g.Key.Fecha,
                    g.Key.IdHora,
                    Count = g.Count()
                })
                .ToListAsync();

            var dictResPorDiaHora = reservasCounts.ToDictionary(
                x => (x.Fecha, x.IdHora),
                x => x.Count);

            var dictResPorDia = reservasCounts
                .GroupBy(x => x.Fecha)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Count));

            var cultura = CultureInfo.GetCultureInfo("es-CR");

            var vm = new ReservasCalendarioVM
            {
                Year = y,
                Month = m,
                MonthLabel = $"{cultura.DateTimeFormat.GetMonthName(m)} {y}"
                    .ToUpperInvariant()
            };

            for (var i = 0; i < 42; i++)
            {
                var dia = gridStart.AddDays(i).Date;
                var bloqueado = setBloqueosDia.Contains(dia);

                if (!bloqueado)
                {
                    var bloqueadasEseDia =
                        dictBloqDiaHora.TryGetValue(dia, out var horasBloqueadas)
                            ? horasBloqueadas
                            : new HashSet<int>();

                    var hayAlgunaDisponible = false;

                    foreach (var idHora in horasActivasIds)
                    {
                        if (setGlobal.Contains(idHora)
                            || bloqueadasEseDia.Contains(idHora))
                        {
                            continue;
                        }

                        var cantidad =
                            dictResPorDiaHora.TryGetValue((dia, idHora), out var count)
                                ? count
                                : 0;

                        if (cantidad < MAX_POR_HORA)
                        {
                            hayAlgunaDisponible = true;
                            break;
                        }
                    }

                    if (!hayAlgunaDisponible)
                        bloqueado = true;
                }

                vm.Days.Add(new DiaCalendarioVM
                {
                    Date = dia,
                    IsCurrentMonth = dia.Month == m,
                    IsToday = dia == hoy,
                    IsBlockedDay = bloqueado,
                    ReservasCount =
                        dictResPorDia.TryGetValue(dia, out var total) ? total : 0
                });
            }

            return View(vm);
        }

        // =========================================================
        // HORAS DISPONIBLES
        // =========================================================
        [HttpGet]
        [Authorize(Roles = "Usuario,Administrador,Entrenador")]
        public async Task<IActionResult> GetHorasDisponibles(
            string date,
            string? idUsuarioObjetivo = null)
        {
            if (!TryParseFecha(date, out var fecha))
                return BadRequest(new { message = "Fecha inválida." });

            var usuarioActualId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(usuarioActualId))
                return Unauthorized(new { message = "No se pudo identificar el usuario." });

            var usuarioObjetivoId = usuarioActualId;

            if (!string.IsNullOrWhiteSpace(idUsuarioObjetivo))
            {
                if (!User.IsInRole("Administrador"))
                    return Forbid();

                usuarioObjetivoId = idUsuarioObjetivo.Trim();
            }

            var usuarioObjetivo = await _context.Users
                .AsNoTracking()
                .Where(u => u.Id == usuarioObjetivoId)
                .Select(u => new
                {
                    u.Id,
                    u.Nombre,
                    u.Apellidos,
                    u.Cedula,
                    u.Familiar,
                    u.CantidadFamilia
                })
                .FirstOrDefaultAsync();

            if (usuarioObjetivo == null)
                return NotFound(new { message = "No se encontró el usuario seleccionado." });

            var limiteUsuarioPorHora = ObtenerLimitePorHora(
                usuarioObjetivo.Familiar,
                usuarioObjetivo.CantidadFamilia);

            var blockedDay = await _context.BloqueosHorarios
                .AsNoTracking()
                .AnyAsync(b => b.Activo
                    && b.Fecha != null
                    && b.IdHora == null
                    && b.Fecha.Value == fecha);

            if (blockedDay)
            {
                return Json(new
                {
                    blockedDay = true,
                    date = fecha.ToString("yyyy-MM-dd"),
                    usuarioObjetivo = new
                    {
                        id = usuarioObjetivo.Id,
                        nombre = NombreCompleto(
                            usuarioObjetivo.Nombre,
                            usuarioObjetivo.Apellidos),
                        cedula = usuarioObjetivo.Cedula
                    },
                    horas = Array.Empty<object>()
                });
            }

            var horasActivas = await _context.HorasReserva
                .AsNoTracking()
                .Where(h => h.Activo)
                .OrderBy(h => h.Hora)
                .Select(h => new
                {
                    h.IdHora,
                    h.Hora,
                    h.Etiqueta
                })
                .ToListAsync();

            var bloqueosGlobales = await _context.BloqueosHorarios
                .AsNoTracking()
                .Where(b => b.Activo && b.Fecha == null && b.IdHora != null)
                .Select(b => b.IdHora!.Value)
                .Distinct()
                .ToListAsync();

            var setGlobal = bloqueosGlobales.ToHashSet();

            var bloqueosDelDia = await _context.BloqueosHorarios
                .AsNoTracking()
                .Where(b => b.Activo
                    && b.Fecha != null
                    && b.IdHora != null
                    && b.Fecha.Value == fecha)
                .Select(b => b.IdHora!.Value)
                .Distinct()
                .ToListAsync();

            var setDia = bloqueosDelDia.ToHashSet();

            var counts = await _context.Reservas
                .AsNoTracking()
                .Where(r => r.Activa && r.Fecha == fecha)
                .GroupBy(r => r.IdHora)
                .Select(g => new
                {
                    IdHora = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();

            var dictCount = counts.ToDictionary(x => x.IdHora, x => x.Count);

            var reservasUsuario = await _context.Reservas
                .AsNoTracking()
                .Where(r => r.Activa
                    && r.IdUsuario == usuarioObjetivoId
                    && r.Fecha == fecha)
                .GroupBy(r => r.IdHora)
                .Select(g => new
                {
                    IdHora = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();

            var dictUsuario = reservasUsuario.ToDictionary(
                x => x.IdHora,
                x => x.Count);

            var ahora = DateTime.Now;

            var result = horasActivas.Select(h =>
            {
                var bloqueoGlobal = setGlobal.Contains(h.IdHora);
                var bloqueoDia = setDia.Contains(h.IdHora);

                var totalReservasHora =
                    dictCount.TryGetValue(h.IdHora, out var total) ? total : 0;

                var reservasDelUsuario =
                    dictUsuario.TryGetValue(h.IdHora, out var propias) ? propias : 0;

                var llena = totalReservasHora >= MAX_POR_HORA;
                var limiteUsuarioLleno =
                    reservasDelUsuario >= limiteUsuarioPorHora;

                var fechaHora = fecha.Date.Add(h.Hora);
                var fueraDeTiempo =
                    fecha < ahora.Date
                    || (fecha == ahora.Date
                        && fechaHora <= ahora.AddMinutes(MINUTOS_ANTICIPACION_RESERVA));

                var disponible =
                    !(bloqueoGlobal
                      || bloqueoDia
                      || llena
                      || limiteUsuarioLleno
                      || fueraDeTiempo);

                return new
                {
                    idHora = h.IdHora,
                    etiqueta = string.IsNullOrWhiteSpace(h.Etiqueta)
                        ? h.Hora.ToString(@"hh\:mm")
                        : h.Etiqueta,
                    disponible,
                    esMia = reservasDelUsuario > 0,
                    misReservas = reservasDelUsuario,
                    limiteUsuario = limiteUsuarioPorHora,
                    cuposDisponibles =
                        Math.Max(0, MAX_POR_HORA - totalReservasHora),
                    fueraDeTiempo
                };
            });

            return Json(new
            {
                blockedDay = false,
                date = fecha.ToString("yyyy-MM-dd"),
                usuarioObjetivo = new
                {
                    id = usuarioObjetivo.Id,
                    nombre = NombreCompleto(
                        usuarioObjetivo.Nombre,
                        usuarioObjetivo.Apellidos),
                    cedula = usuarioObjetivo.Cedula
                },
                horas = result
            });
        }

        // =========================================================
        // CONSULTAS DE RESERVAS
        // =========================================================
        [HttpGet]
        [Authorize(Roles = "Administrador,Entrenador")]
        public async Task<IActionResult> GetReservasPorHora(
            string date,
            int idHora)
        {
            if (!TryParseFecha(date, out var fecha))
                return BadRequest(new { message = "Fecha inválida." });

            var hora = await _context.HorasReserva
                .AsNoTracking()
                .Where(h => h.IdHora == idHora)
                .Select(h => new
                {
                    h.IdHora,
                    h.Etiqueta,
                    h.Hora
                })
                .FirstOrDefaultAsync();

            if (hora == null)
                return NotFound(new { message = "La hora no existe." });

            var reservas = await _context.Reservas
                .AsNoTracking()
                .Where(r => r.Activa
                    && r.Fecha == fecha
                    && r.IdHora == idHora)
                .Join(
                    _context.Users.AsNoTracking(),
                    r => r.IdUsuario,
                    u => u.Id,
                    (r, u) => new
                    {
                        r.IdReserva,
                        Nombre = (u.Nombre + " " + u.Apellidos).Trim(),
                        u.Cedula,
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
                    idReserva = x.IdReserva,
                    nombre = string.IsNullOrWhiteSpace(x.Nombre)
                        ? "Usuario"
                        : x.Nombre,
                    cedula = x.Cedula ?? string.Empty,
                    email = x.Email ?? string.Empty,
                    telefono = x.Telefono ?? string.Empty
                })
            });
        }

        [HttpGet]
        [Authorize(Roles = "Administrador,Entrenador")]
        public async Task<IActionResult> GetReservasPorDia(string date)
        {
            if (!TryParseFecha(date, out var fecha))
                return BadRequest(new { message = "Fecha inválida." });

            var reservas = await _context.Reservas
                .AsNoTracking()
                .Where(r => r.Activa && r.Fecha == fecha)
                .Join(
                    _context.HorasReserva.AsNoTracking(),
                    r => r.IdHora,
                    h => h.IdHora,
                    (r, h) => new
                    {
                        r.IdReserva,
                        r.IdUsuario,
                        h.IdHora,
                        h.Hora,
                        h.Etiqueta
                    })
                .Join(
                    _context.Users.AsNoTracking(),
                    x => x.IdUsuario,
                    u => u.Id,
                    (x, u) => new
                    {
                        x.IdReserva,
                        x.IdHora,
                        x.Hora,
                        x.Etiqueta,
                        Nombre = (u.Nombre + " " + u.Apellidos).Trim(),
                        u.Cedula,
                        u.Email,
                        Telefono = u.TelefonoPersonal
                    })
                .OrderBy(x => x.Hora)
                .ThenBy(x => x.Nombre)
                .ToListAsync();

            var grupos = reservas
                .GroupBy(x => new
                {
                    x.IdHora,
                    x.Hora,
                    x.Etiqueta
                })
                .OrderBy(g => g.Key.Hora)
                .Select(g => new
                {
                    idHora = g.Key.IdHora,
                    hora = string.IsNullOrWhiteSpace(g.Key.Etiqueta)
                        ? g.Key.Hora.ToString(@"hh\:mm")
                        : g.Key.Etiqueta,
                    total = g.Count(),
                    reservas = g.Select(x => new
                    {
                        idReserva = x.IdReserva,
                        nombre = string.IsNullOrWhiteSpace(x.Nombre)
                            ? "Usuario"
                            : x.Nombre,
                        cedula = x.Cedula ?? string.Empty,
                        email = x.Email ?? string.Empty,
                        telefono = x.Telefono ?? string.Empty
                    })
                })
                .ToList();

            return Json(new
            {
                date = fecha.ToString("yyyy-MM-dd"),
                fechaTexto = fecha.ToString(
                    "dddd dd 'de' MMMM yyyy",
                    CultureInfo.GetCultureInfo("es-CR")),
                total = reservas.Count,
                grupos
            });
        }

        // =========================================================
        // CREACIÓN
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Usuario")]
        public async Task<IActionResult> CrearDesdeCalendario(
            string date,
            int idHora)
        {
            if (!TryParseFecha(date, out var fecha))
            {
                TempData["Error"] = "Fecha inválida.";
                return RedirectToAction(nameof(Index));
            }

            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(userId))
            {
                TempData["Error"] = "No se pudo identificar el usuario.";
                return RedirectToAction(nameof(Index), new
                {
                    year = fecha.Year,
                    month = fecha.Month
                });
            }

            try
            {
                var resultado = await CrearReservaParaUsuarioAsync(
                    fecha,
                    idHora,
                    userId,
                    userId);

                TempData[resultado.Ok ? "Ok" : "Error"] = resultado.Message;
            }
            catch
            {
                TempData["Error"] =
                    "Ocurrió un error creando la reserva. No se consumieron lecciones parcialmente.";
            }

            return RedirectToAction(nameof(Index), new
            {
                year = fecha.Year,
                month = fecha.Month
            });
        }

        [HttpGet]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> BuscarUsuarios(string termino)
        {
            termino = termino?.Trim() ?? string.Empty;

            if (termino.Length < 2)
            {
                return BadRequest(new
                {
                    message =
                        "Ingresá al menos 2 caracteres del nombre, correo o cédula."
                });
            }

            var usuarios = await _context.Users
                .AsNoTracking()
                .Where(u =>
                    u.Cedula.Contains(termino)
                    || (u.Email != null && u.Email.Contains(termino))
                    || u.Nombre.Contains(termino)
                    || u.Apellidos.Contains(termino)
                    || (u.Nombre + " " + u.Apellidos).Contains(termino))
                .OrderByDescending(u =>
                    u.Cedula == termino
                    || u.Email == termino
                    || (u.Nombre + " " + u.Apellidos) == termino)
                .ThenBy(u => u.Apellidos)
                .ThenBy(u => u.Nombre)
                .Take(MAX_RESULTADOS_USUARIOS)
                .Select(u => new
                {
                    idUsuario = u.Id,
                    cedula = u.Cedula,
                    nombre = (u.Nombre + " " + u.Apellidos).Trim(),
                    email = u.Email,
                    telefono = u.TelefonoPersonal,
                    familiar = u.Familiar,
                    cantidadFamilia = u.CantidadFamilia
                })
                .ToListAsync();

            return Json(new
            {
                total = usuarios.Count,
                usuarios
            });
        }

        [HttpGet]
        [Authorize(Roles = "Administrador")]
        public Task<IActionResult> BuscarUsuariosPorCedula(string cedula)
            => BuscarUsuarios(cedula);

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> CrearReservaAdministrador(
            string date,
            int idHora,
            string idUsuarioObjetivo)
        {
            if (!TryParseFecha(date, out var fecha))
            {
                TempData["Error"] = "Fecha inválida.";
                return RedirectToAction(nameof(Index));
            }

            var administradorId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(administradorId))
            {
                TempData["Error"] = "No se pudo identificar al administrador.";
                return RedirectToAction(nameof(Index), new
                {
                    year = fecha.Year,
                    month = fecha.Month
                });
            }

            if (string.IsNullOrWhiteSpace(idUsuarioObjetivo))
            {
                TempData["Error"] =
                    "Debés seleccionar el usuario para quien se hará la reserva.";

                return RedirectToAction(nameof(Index), new
                {
                    year = fecha.Year,
                    month = fecha.Month
                });
            }

            try
            {
                var resultado = await CrearReservaParaUsuarioAsync(
                    fecha,
                    idHora,
                    idUsuarioObjetivo.Trim(),
                    administradorId);

                TempData[resultado.Ok ? "Ok" : "Error"] = resultado.Message;
            }
            catch
            {
                TempData["Error"] =
                    "Ocurrió un error creando la reserva para el usuario.";
            }

            return RedirectToAction(nameof(Index), new
            {
                year = fecha.Year,
                month = fecha.Month
            });
        }

        // =========================================================
        // MIS RESERVAS / CANCELACIÓN
        // =========================================================
        [HttpGet]
        [Authorize(Roles = "Usuario,Administrador")]
        public async Task<IActionResult> GetMisReservas(
            string? idUsuarioObjetivo = null,
            bool anteriores = false)
        {
            var usuarioActualId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(usuarioActualId))
                return Unauthorized(new { message = "No se pudo identificar el usuario." });

            var esAdministrador = User.IsInRole("Administrador");
            var usuarioObjetivoId = usuarioActualId;

            if (!string.IsNullOrWhiteSpace(idUsuarioObjetivo))
            {
                if (!esAdministrador
                    && !string.Equals(
                        idUsuarioObjetivo,
                        usuarioActualId,
                        StringComparison.Ordinal))
                {
                    return Forbid();
                }

                usuarioObjetivoId = idUsuarioObjetivo.Trim();
            }

            var usuarioObjetivo = await _context.Users
                .AsNoTracking()
                .Where(u => u.Id == usuarioObjetivoId)
                .Select(u => new
                {
                    u.Id,
                    u.Nombre,
                    u.Apellidos,
                    u.Cedula
                })
                .FirstOrDefaultAsync();

            if (usuarioObjetivo == null)
                return NotFound(new { message = "No se encontró el usuario seleccionado." });

            var ahora = DateTime.Now;
            var hoy = DateTime.Today;

            var paqueteVigente = await _context.PaquetesUsuario
                .AsNoTracking()
                .Where(pu => pu.IdUsuario == usuarioObjetivoId
                    && pu.Activo
                    && pu.FechaInicio.Date <= hoy
                    && pu.FechaFin.Date >= hoy)
                .OrderByDescending(pu => pu.CantLecciones)
                .ThenBy(pu => pu.FechaFin)
                .ThenByDescending(pu => pu.IdPaqueteUsuario)
                .Select(pu => new
                {
                    pu.IdPaqueteUsuario,
                    pu.CantLecciones,
                    pu.FechaInicio,
                    pu.FechaFin
                })
                .FirstOrDefaultAsync();

            var reservasDb = await _context.Reservas
                .AsNoTracking()
                .Where(r => r.IdUsuario == usuarioObjetivoId)
                .Include(r => r.HoraReserva)
                .ToListAsync();

            var reservasFiltradas = reservasDb
                .Select(r => new
                {
                    Reserva = r,
                    FechaHora = r.Fecha.Date.Add(r.HoraReserva.Hora)
                })
                .Where(x => anteriores
                    ? x.FechaHora < ahora
                    : x.FechaHora >= ahora)
                .ToList();

            reservasFiltradas = anteriores
                ? reservasFiltradas
                    .OrderByDescending(x => x.FechaHora)
                    .ToList()
                : reservasFiltradas
                    .OrderBy(x => x.FechaHora)
                    .ToList();

            var cultura = CultureInfo.GetCultureInfo("es-CR");

            var resultado = reservasFiltradas.Select(x =>
            {
                var r = x.Reserva;
                var yaPaso = x.FechaHora < ahora;
                var puedeCancelar = r.Activa
                    && !yaPaso
                    && x.FechaHora >= ahora.AddHours(1);

                var estado = !r.Activa
                    ? "Cancelada"
                    : yaPaso
                        ? "Reserva realizada"
                        : puedeCancelar
                            ? "Activa"
                            : "Activa - ya no se puede cancelar";

                var horaEtiqueta = string.IsNullOrWhiteSpace(r.HoraReserva.Etiqueta)
                    ? r.HoraReserva.Hora.ToString(@"hh\:mm")
                    : r.HoraReserva.Etiqueta;

                return new
                {
                    idReserva = r.IdReserva,
                    fecha = r.Fecha.ToString("yyyy-MM-dd"),
                    fechaTexto = r.Fecha.ToString(
                        "dddd dd 'de' MMMM yyyy",
                        cultura),
                    idHora = r.IdHora,
                    hora = horaEtiqueta,
                    activa = r.Activa,
                    yaPaso,
                    puedeCancelar,
                    estado,
                    motivoCancelacion = r.MotivoCancelacion ?? string.Empty
                };
            }).ToList();

            return Json(new
            {
                usuario = new
                {
                    idUsuario = usuarioObjetivo.Id,
                    nombre = NombreCompleto(
                        usuarioObjetivo.Nombre,
                        usuarioObjetivo.Apellidos),
                    cedula = usuarioObjetivo.Cedula ?? string.Empty
                },
                anteriores,
                paqueteActivo = paqueteVigente == null
                    ? null
                    : new
                    {
                        idPaqueteUsuario = paqueteVigente.IdPaqueteUsuario,
                        leccionesDisponibles = paqueteVigente.CantLecciones,
                        fechaInicio = paqueteVigente.FechaInicio.ToString("yyyy-MM-dd"),
                        fechaInicioTexto = paqueteVigente.FechaInicio.ToString("dd/MM/yyyy", cultura),
                        fechaFin = paqueteVigente.FechaFin.ToString("yyyy-MM-dd"),
                        fechaFinTexto = paqueteVigente.FechaFin.ToString("dd/MM/yyyy", cultura)
                    },
                total = resultado.Count,
                reservas = resultado
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Usuario,Administrador")]
        public async Task<IActionResult> CancelarMiReserva(
            int idReserva,
            string motivoCancelacion)
        {
            motivoCancelacion = motivoCancelacion?.Trim() ?? string.Empty;

            if (motivoCancelacion.Length == 0)
            {
                return BadRequest(new
                {
                    ok = false,
                    message = "Debés indicar el motivo de la cancelación."
                });
            }

            if (motivoCancelacion.Length > 300)
            {
                return BadRequest(new
                {
                    ok = false,
                    message = "El motivo no puede superar los 300 caracteres."
                });
            }

            var usuarioActualId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(usuarioActualId))
            {
                return Unauthorized(new
                {
                    ok = false,
                    message = "No se pudo identificar el usuario."
                });
            }

            var esAdministrador = User.IsInRole("Administrador");

            try
            {
                var strategy = _context.Database.CreateExecutionStrategy();
                IActionResult? result = null;

                await strategy.ExecuteAsync(async () =>
                {
                    await using var tx = await _context.Database.BeginTransactionAsync(
                        IsolationLevel.Serializable);

                    var queryReserva = _context.Reservas
                        .Include(r => r.HoraReserva)
                        .Where(r => r.IdReserva == idReserva && r.Activa);

                    if (!esAdministrador)
                    {
                        queryReserva = queryReserva
                            .Where(r => r.IdUsuario == usuarioActualId);
                    }

                    var reserva = await queryReserva.FirstOrDefaultAsync();

                    if (reserva == null)
                    {
                        result = NotFound(new
                        {
                            ok = false,
                            message = "No se encontró una reserva activa."
                        });
                        return;
                    }

                    var fechaHoraReserva =
                        reserva.Fecha.Date.Add(reserva.HoraReserva.Hora);

                    if (fechaHoraReserva < DateTime.Now.AddHours(1))
                    {
                        result = BadRequest(new
                        {
                            ok = false,
                            message =
                                "Solo se puede cancelar una reserva con 1 hora o más de anticipación."
                        });
                        return;
                    }

                    // Reserva no guarda IdPaqueteUsuario.
                    // Se resuelve la asignación individual que cubría la fecha
                    // de la reserva, priorizando una asignación activa.
                    var paquete = await _context.PaquetesUsuario
                        .Where(pu => pu.IdUsuario == reserva.IdUsuario
                            && pu.FechaInicio.Date <= reserva.Fecha.Date
                            && pu.FechaFin.Date >= reserva.Fecha.Date)
                        .OrderByDescending(pu => pu.Activo)
                        .ThenByDescending(pu => pu.IdPaqueteUsuario)
                        .FirstOrDefaultAsync();

                    if (paquete == null)
                    {
                        result = BadRequest(new
                        {
                            ok = false,
                            message =
                                "No se encontró de forma segura el paquete del cual se descontó la lección. La reserva no fue modificada."
                        });
                        return;
                    }

                    reserva.Activa = false;
                    reserva.MotivoCancelacion = motivoCancelacion;
                    paquete.CantLecciones += 1;

                    await _context.SaveChangesAsync();
                    await tx.CommitAsync();

                    result = Json(new
                    {
                        ok = true,
                        message =
                            "Reserva cancelada correctamente. Se devolvió 1 lección al paquete correspondiente."
                    });
                });

                return result ?? BadRequest(new
                {
                    ok = false,
                    message = "No se pudo cancelar la reserva."
                });
            }
            catch
            {
                return StatusCode(500, new
                {
                    ok = false,
                    message =
                        "Ocurrió un error cancelando la reserva. No se aplicaron cambios parciales."
                });
            }
        }

        // =========================================================
        // ADMINISTRADOR: CONSULTAR RESERVAS DE USUARIO
        // =========================================================
        [HttpGet]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> GetReservasUsuario(
            string? idUsuario,
            string fechaInicio,
            string fechaFin,
            string? cedula = null)
        {
            idUsuario = idUsuario?.Trim();
            cedula = cedula?.Trim();

            if (string.IsNullOrWhiteSpace(idUsuario)
                && string.IsNullOrWhiteSpace(cedula))
            {
                return BadRequest(new
                {
                    message =
                        "Primero seleccioná el usuario que deseás consultar."
                });
            }

            if (!TryParseFecha(fechaInicio, out var inicio)
                || !TryParseFecha(fechaFin, out var fin))
            {
                return BadRequest(new { message = "El rango de fechas es inválido." });
            }

            if (fin < inicio)
            {
                return BadRequest(new
                {
                    message = "La fecha final no puede ser menor que la fecha inicial."
                });
            }

            var consultaUsuario = _context.Users.AsNoTracking();

            consultaUsuario = !string.IsNullOrWhiteSpace(idUsuario)
                ? consultaUsuario.Where(u => u.Id == idUsuario)
                : consultaUsuario.Where(u => u.Cedula == cedula);

            var usuario = await consultaUsuario
                .Select(u => new
                {
                    u.Id,
                    u.Cedula,
                    u.Nombre,
                    u.Apellidos,
                    u.Email,
                    u.TelefonoPersonal
                })
                .FirstOrDefaultAsync();

            if (usuario == null)
                return NotFound(new { message = "No se encontró el usuario seleccionado." });

            var reservas = await _context.Reservas
                .AsNoTracking()
                .Where(r => r.IdUsuario == usuario.Id
                    && r.Fecha >= inicio
                    && r.Fecha <= fin)
                .Include(r => r.HoraReserva)
                .Include(r => r.UsuarioReserva)
                .OrderByDescending(r => r.Fecha)
                .ThenByDescending(r => r.HoraReserva.Hora)
                .ToListAsync();

            var cultura = CultureInfo.GetCultureInfo("es-CR");
            var ahora = DateTime.Now;

            var resultado = reservas.Select(r =>
            {
                var fechaHora = r.Fecha.Date.Add(r.HoraReserva.Hora);
                var yaPaso = fechaHora < ahora;

                var estado = !r.Activa
                    ? "Cancelada"
                    : yaPaso
                        ? "Reserva realizada"
                        : "Activa";

                return new
                {
                    idReserva = r.IdReserva,
                    fecha = r.Fecha.ToString("yyyy-MM-dd"),
                    fechaTexto = r.Fecha.ToString("dd/MM/yyyy", cultura),
                    hora = string.IsNullOrWhiteSpace(r.HoraReserva.Etiqueta)
                        ? r.HoraReserva.Hora.ToString(@"hh\:mm")
                        : r.HoraReserva.Etiqueta,
                    activa = r.Activa,
                    yaPaso,
                    estado,
                    motivoCancelacion =
                        r.MotivoCancelacion ?? string.Empty,
                    registradaPor = NombreCompleto(
                        r.UsuarioReserva.Nombre,
                        r.UsuarioReserva.Apellidos),
                    registradaPorMismoUsuario =
                        r.IdUsuario == r.IdUsuarioReserva
                };
            }).ToList();

            return Json(new
            {
                usuario = new
                {
                    idUsuario = usuario.Id,
                    cedula = usuario.Cedula,
                    nombre = NombreCompleto(
                        usuario.Nombre,
                        usuario.Apellidos),
                    email = usuario.Email ?? string.Empty,
                    telefono = usuario.TelefonoPersonal ?? string.Empty
                },
                fechaInicio = inicio.ToString("yyyy-MM-dd"),
                fechaFin = fin.ToString("yyyy-MM-dd"),
                total = resultado.Count,
                reservas = resultado
            });
        }

        // =========================================================
        // CORE DE CREACIÓN
        // =========================================================
        private async Task<(bool Ok, string Message)> CrearReservaParaUsuarioAsync(
            DateTime fecha,
            int idHora,
            string idUsuarioObjetivo,
            string idUsuarioCreador)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            (bool Ok, string Message) resultado =
                (false, "No se pudo crear la reserva.");

            await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

                var usuarioObjetivo = await _context.Users
                    .Where(u => u.Id == idUsuarioObjetivo)
                    .Select(u => new
                    {
                        u.Id,
                        u.Nombre,
                        u.Apellidos,
                        u.Familiar,
                        u.CantidadFamilia
                    })
                    .FirstOrDefaultAsync();

                if (usuarioObjetivo == null)
                {
                    resultado =
                        (false, "No se encontró el usuario para quien se hará la reserva.");
                    return;
                }

                if (!await _context.Users.AnyAsync(u => u.Id == idUsuarioCreador))
                {
                    resultado =
                        (false, "No se pudo identificar al usuario que registra la reserva.");
                    return;
                }

                var hora = await _context.HorasReserva
                    .FirstOrDefaultAsync(h => h.IdHora == idHora && h.Activo);

                if (hora == null)
                {
                    resultado = (false, "La hora seleccionada no está disponible.");
                    return;
                }

                var ahora = DateTime.Now;

                if (fecha < ahora.Date)
                {
                    resultado = (false, "No se puede reservar en fechas pasadas.");
                    return;
                }

                if (fecha == ahora.Date)
                {
                    var fechaHoraSlot = fecha.Add(hora.Hora);

                    if (fechaHoraSlot <=
                        ahora.AddMinutes(MINUTOS_ANTICIPACION_RESERVA))
                    {
                        resultado =
                            (false,
                             $"La reserva debe hacerse con al menos {MINUTOS_ANTICIPACION_RESERVA} minutos de anticipación.");
                        return;
                    }
                }

                var blockedDay = await _context.BloqueosHorarios
                    .AnyAsync(b => b.Activo
                        && b.Fecha != null
                        && b.IdHora == null
                        && b.Fecha.Value == fecha);

                if (blockedDay)
                {
                    resultado = (false, "Ese día está bloqueado.");
                    return;
                }

                var blockedHour = await _context.BloqueosHorarios
                    .AnyAsync(b => b.Activo
                        && b.IdHora != null
                        && b.IdHora.Value == idHora
                        && (b.Fecha == null || b.Fecha.Value == fecha));

                if (blockedHour)
                {
                    resultado = (false, "Esa hora está bloqueada.");
                    return;
                }

                var limiteUsuarioPorHora = ObtenerLimitePorHora(
                    usuarioObjetivo.Familiar,
                    usuarioObjetivo.CantidadFamilia);

                var cantidadTotal = await _context.Reservas
                    .CountAsync(r => r.Activa
                        && r.Fecha == fecha
                        && r.IdHora == idHora);

                if (cantidadTotal >= MAX_POR_HORA)
                {
                    resultado = (false, "Esa hora ya está llena.");
                    return;
                }

                var cantidadUsuario = await _context.Reservas
                    .CountAsync(r => r.Activa
                        && r.IdUsuario == idUsuarioObjetivo
                        && r.Fecha == fecha
                        && r.IdHora == idHora);

                if (cantidadUsuario >= limiteUsuarioPorHora)
                {
                    resultado = usuarioObjetivo.Familiar
                        ? (false,
                           $"El usuario ya alcanzó el máximo permitido para su plan familiar en esta hora ({limiteUsuarioPorHora}).")
                        : (false,
                           "El usuario ya tiene una reserva activa en esa hora.");
                    return;
                }

                var paqueteActivo = await _context.PaquetesUsuario
                    .Where(pu => pu.IdUsuario == idUsuarioObjetivo
                        && pu.Activo
                        && pu.CantLecciones > 0
                        && pu.FechaInicio.Date <= fecha
                        && pu.FechaFin.Date >= fecha)
                    .OrderBy(pu => pu.FechaFin)
                    .ThenByDescending(pu => pu.IdPaqueteUsuario)
                    .FirstOrDefaultAsync();

                if (paqueteActivo == null)
                {
                    resultado =
                        (false,
                         "El usuario no tiene lecciones disponibles o su paquete no está vigente para esa fecha.");
                    return;
                }

                _context.Reservas.Add(new Reserva
                {
                    IdUsuario = idUsuarioObjetivo,
                    IdUsuarioReserva = idUsuarioCreador,
                    Fecha = fecha,
                    IdHora = idHora,
                    Activa = true,
                    MotivoCancelacion = null
                });

                paqueteActivo.CantLecciones -= 1;

                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                var nombre = NombreCompleto(
                    usuarioObjetivo.Nombre,
                    usuarioObjetivo.Apellidos);

                resultado = idUsuarioObjetivo == idUsuarioCreador
                    ? (true, "Reserva creada correctamente.")
                    : (true, $"Reserva creada correctamente para {nombre}.");
            });

            return resultado;
        }

        private static bool TryParseFecha(
            string? value,
            out DateTime fecha)
        {
            fecha = default;

            if (string.IsNullOrWhiteSpace(value))
                return false;

            if (!DateTime.TryParseExact(
                    value,
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var fechaParseada))
            {
                return false;
            }

            fecha = fechaParseada.Date;
            return true;
        }

        private static int ObtenerLimitePorHora(
            bool familiar,
            int? cantidadFamilia)
        {
            if (familiar
                && cantidadFamilia.HasValue
                && cantidadFamilia.Value > 0)
            {
                return Math.Min(cantidadFamilia.Value, MAX_POR_HORA);
            }

            return 1;
        }

        private static string NombreCompleto(
            string? nombre,
            string? apellidos)
        {
            var resultado = $"{nombre} {apellidos}".Trim();
            return string.IsNullOrWhiteSpace(resultado) ? "Usuario" : resultado;
        }
    }
}
