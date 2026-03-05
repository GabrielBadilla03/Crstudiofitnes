using EnviaCorreoNotificaciones.Data;
using EnviaCorreoNotificaciones.Models;
using EnviaCorreoNotificaciones.Services;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace EnviaCorreoNotificaciones.Workers
{
    public class ReservationReminderWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IEmailSender _email;
        private readonly SchedulerSettings _sched;
        private readonly TimeZoneInfo _tz;
        private readonly ILogger<ReservationReminderWorker> _logger;
        private DateTime? _lastPesajeRunDate;

        public ReservationReminderWorker(
            IServiceScopeFactory scopeFactory,
            IEmailSender email,
            IOptions<SchedulerSettings> schedOpt,
            ILogger<ReservationReminderWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _email = email;
            _sched = schedOpt.Value;
            _logger = logger;
            _tz = TimeZoneInfo.FindSystemTimeZoneById(_sched.TimeZoneId);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_sched.PollSeconds));

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    var nowLocal = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, _tz).DateTime;

                    await ProcessNextHourAsync(nowLocal, stoppingToken);

                    if (nowLocal.TimeOfDay >= _sched.NextDayTime)
                    {
                        await ProcessNextDayAsync(nowLocal, stoppingToken);

                        // ✅ Pesajes: correr 1 vez por día (idempotente también por DB)
                        if (_lastPesajeRunDate?.Date != nowLocal.Date)
                        {
                            await ProcessPesajeRemindersDailyAsync(nowLocal, stoppingToken);
                            _lastPesajeRunDate = nowLocal.Date;
                        }
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error general del worker");
                }
            }
        }

        private async Task ProcessNextHourAsync(DateTime nowLocal, CancellationToken ct)
        {
            var baseHour = new DateTime(nowLocal.Year, nowLocal.Month, nowLocal.Day, nowLocal.Hour, 0, 0);
            var target = baseHour.AddHours(1);

            var targetDate = target.Date;
            var targetTime = target.TimeOfDay; // 08:00

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<WorkerDbContext>();

            var reservas = await (
                from r in db.Reservas
                join h in db.HorasReserva on r.IdHora equals h.IdHora
                join u in db.AspNetUsers on r.IdUsuario equals u.Id
                where r.Fecha == targetDate
                      && h.Hora == targetTime
                      && u.Email != null
                select new
                {
                    r.IdReserva,
                    Email = u.Email!,
                    Nombre = ((u.Nombre ?? "") + " " + (u.Apellidos ?? "")).Trim(),
                    r.Fecha,
                    Hora = h.Hora
                }
            ).ToListAsync(ct);

            foreach (var x in reservas)
                await TrySendReminderAsync(db, x.IdReserva, ReminderKind.NextHour, x.Email, x.Nombre, x.Fecha, x.Hora, ct);
        }

        private async Task ProcessNextDayAsync(DateTime nowLocal, CancellationToken ct)
        {
            var tomorrow = nowLocal.Date.AddDays(1);

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<WorkerDbContext>();

            var reservas = await (
                from r in db.Reservas
                join h in db.HorasReserva on r.IdHora equals h.IdHora
                join u in db.AspNetUsers on r.IdUsuario equals u.Id
                where r.Fecha == tomorrow
                      && u.Email != null
                select new
                {
                    r.IdReserva,
                    Email = u.Email!,
                    Nombre = ((u.Nombre ?? "") + " " + (u.Apellidos ?? "")).Trim(),
                    r.Fecha,
                    Hora = h.Hora
                }
            ).ToListAsync(ct);

            foreach (var x in reservas)
                await TrySendReminderAsync(db, x.IdReserva, ReminderKind.NextDay, x.Email, x.Nombre, x.Fecha, x.Hora, ct);
        }

        private async Task TrySendReminderAsync(
            WorkerDbContext db,
            int reservaId,
            ReminderKind kind,
            string toEmail,
            string nombre,
            DateTime fecha,
            TimeSpan hora,
            CancellationToken ct)
        {
            var nowUtc = DateTime.UtcNow;

            var reminder = await db.ReservaEmailReminders
                .FirstOrDefaultAsync(x => x.ReservaId == reservaId && x.Kind == kind, ct);

            if (reminder is { Status: ReminderStatus.Sent })
                return;

            if (reminder is { Status: ReminderStatus.Failed } &&
                reminder.NextAttemptAt.HasValue && reminder.NextAttemptAt > nowUtc)
                return;

            if (reminder is null)
            {
                reminder = new ReservaEmailReminder
                {
                    ReservaId = reservaId,
                    Kind = kind,
                    Status = ReminderStatus.Pending,
                    Attempts = 0,
                    CreatedAtUtc = nowUtc
                };

                db.ReservaEmailReminders.Add(reminder);

                try { await db.SaveChangesAsync(ct); }
                catch { return; } // ya existe por carrera, salimos
            }

            if (reminder.Attempts >= _sched.MaxAttempts)
                return;

            var horaStr = DateTime.Today.Add(hora).ToString("hh:mm tt");
            var fechaStr = fecha.ToString("dd/MM/yyyy");

            var subject = kind == ReminderKind.NextHour
                ? $"Recordatorio: tenés una reserva a las {horaStr}"
                : $"Recordatorio: mañana tenés una reserva a las {horaStr}";

            var body = $@"
                <div style='font-family:Arial,sans-serif'>
                  <p>Hola {(string.IsNullOrWhiteSpace(nombre) ? "usuario" : System.Net.WebUtility.HtmlEncode(nombre))},</p>
                  <p>Este es un recordatorio de tu reserva:</p>
                  <ul>
                    <li><b>Fecha:</b> {fechaStr}</li>
                    <li><b>Hora:</b> {horaStr}</li>
                  </ul>
                  <p>¡Te esperamos!</p>
                  <p><b>CVStudioFitness</b></p>
                </div>";

            try
            {
                await _email.SendAsync(toEmail, subject, body, ct);

                reminder.Status = ReminderStatus.Sent;
                reminder.SentAtUtc = nowUtc;
                reminder.LastError = null;
                reminder.NextAttemptAt = null;

                await db.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                reminder.Status = ReminderStatus.Failed;
                reminder.Attempts += 1;
                reminder.LastError = ex.Message;

                var minutes = reminder.Attempts switch { 1 => 1, 2 => 5, 3 => 15, _ => 60 };
                reminder.NextAttemptAt = nowUtc.AddMinutes(minutes);

                await db.SaveChangesAsync(ct);
            }
        }

        private async Task ProcessPesajeRemindersDailyAsync(DateTime nowLocal, CancellationToken ct)
        {
            var today = nowLocal.Date;
            var tomorrow = today.AddDays(1);

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<WorkerDbContext>();

            // 1) Traer todos los historiales del usuario con email y ordenarlos por FechaInicio desc
            //    Luego en memoria nos quedamos con el historial más reciente por usuario
            var rawHist = await (
                from h in db.Historiales.AsNoTracking()
                join u in db.AspNetUsers.AsNoTracking() on h.IdUsuario equals u.Id
                where u.Email != null
                select new
                {
                    h.IdHistorial,
                    h.IdUsuario,
                    h.FechaInicio,
                    h.FechaFin,
                    h.Frecuencia,
                    h.Objetivo,
                    Email = u.Email!,
                    Nombre = ((u.Nombre ?? "") + " " + (u.Apellidos ?? "")).Trim()
                }
            )
            .OrderByDescending(x => x.FechaInicio)
            .ThenByDescending(x => x.IdHistorial)
            .ToListAsync(ct);

            var latestByUser = rawHist
                .GroupBy(x => x.IdUsuario)
                .Select(g => g.First())
                .ToList();

            if (latestByUser.Count == 0) return;

            // 2) Último pesaje por historial (de esos historiales más recientes)
            var historialIds = latestByUser.Select(x => x.IdHistorial).ToList();

            var lastPesajeList = await db.Pesajes.AsNoTracking()
                .Where(p => historialIds.Contains(p.IdHistorial))
                .GroupBy(p => p.IdHistorial)
                .Select(g => new { IdHistorial = g.Key, LastFecha = g.Max(p => p.Fecha) })
                .ToListAsync(ct);

            var lastPesajeMap = lastPesajeList.ToDictionary(x => x.IdHistorial, x => x.LastFecha.Date);

            // 3) Para cada usuario: calcular próxima fecha de pesaje según frecuencia
            foreach (var h in latestByUser)
            {
                // si historial terminado, no molestar
                if (h.FechaFin.HasValue && h.FechaFin.Value.Date < today)
                    continue;

                if (h.Frecuencia is null)
                    continue;

                // baseDate:
                // - si NO hay pesajes: FechaInicio
                // - si hay: último pesaje
                var baseDate = lastPesajeMap.TryGetValue(h.IdHistorial, out var last)
                    ? last
                    : h.FechaInicio.Date;

                var dueDate = ComputeNextDueDate(baseDate, h.Frecuencia.Value, today);

                // ✅ 1 día antes: hoy si el dueDate es mañana
                if (dueDate == tomorrow)
                {
                    await TrySendPesajeReminderAsync(
                        db,
                        historialId: h.IdHistorial,
                        dueDate: dueDate,
                        kind: PesajeReminderKind.DayBefore,
                        toEmail: h.Email,
                        nombre: h.Nombre,
                        frecuencia: h.Frecuencia.Value,
                        objetivo: h.Objetivo,
                        baseDate: baseDate,
                        ct: ct
                    );
                }

                // ✅ mismo día: hoy si dueDate es hoy
                if (dueDate == today)
                {
                    await TrySendPesajeReminderAsync(
                        db,
                        historialId: h.IdHistorial,
                        dueDate: dueDate,
                        kind: PesajeReminderKind.SameDay,
                        toEmail: h.Email,
                        nombre: h.Nombre,
                        frecuencia: h.Frecuencia.Value,
                        objetivo: h.Objetivo,
                        baseDate: baseDate,
                        ct: ct
                    );
                }
            }
        }

        private static DateTime ComputeNextDueDate(DateTime baseDate, TipoPlanDias frecuencia, DateTime today)
        {
            // La primera "fecha que toca" es base + periodo
            var due = AddPeriod(baseDate.Date, frecuencia);

            // Si se cayó el worker y ya pasaron varias fechas, adelantamos hasta llegar a hoy o futuro
            int guard = 0;
            while (due.Date < today.Date && guard++ < 500)
                due = AddPeriod(due, frecuencia);

            return due.Date;
        }

        private static DateTime AddPeriod(DateTime d, TipoPlanDias frecuencia)
        {
            return frecuencia switch
            {
                TipoPlanDias.Diario => d.AddDays(1),
                TipoPlanDias.Semanal => d.AddDays(7),
                TipoPlanDias.Quincenal => d.AddDays(15),
                TipoPlanDias.Mensual => d.AddMonths(1),
                _ => d.AddDays(7)
            };
        }

        private async Task TrySendPesajeReminderAsync(
            WorkerDbContext db,
            int historialId,
            DateTime dueDate,
            PesajeReminderKind kind,
            string toEmail,
            string nombre,
            TipoPlanDias frecuencia,
            string? objetivo,
            DateTime baseDate,
            CancellationToken ct)
        {
            var nowUtc = DateTime.UtcNow;

            var reminder = await db.PesajeEmailReminders
                .FirstOrDefaultAsync(x =>
                    x.HistorialId == historialId &&
                    x.DueDate == dueDate.Date &&
                    x.Kind == kind, ct);

            if (reminder is { Status: ReminderStatus.Sent })
                return;

            if (reminder is { Status: ReminderStatus.Failed } &&
                reminder.NextAttemptAt.HasValue && reminder.NextAttemptAt > nowUtc)
                return;

            if (reminder is null)
            {
                reminder = new PesajeEmailReminder
                {
                    HistorialId = historialId,
                    DueDate = dueDate.Date,
                    Kind = kind,
                    Status = ReminderStatus.Pending,
                    Attempts = 0,
                    CreatedAtUtc = nowUtc
                };

                db.PesajeEmailReminders.Add(reminder);

                try { await db.SaveChangesAsync(ct); }
                catch { return; } // carrera: ya existe
            }

            if (reminder.Attempts >= _sched.MaxAttempts)
                return;

            var fechaDueStr = dueDate.ToString("dd/MM/yyyy");
            var baseStr = baseDate.ToString("dd/MM/yyyy");

            var freqTxt = frecuencia switch
            {
                TipoPlanDias.Diario => "diaria",
                TipoPlanDias.Semanal => "semanal",
                TipoPlanDias.Quincenal => "quincenal",
                TipoPlanDias.Mensual => "mensual",
                _ => "semanal"
            };

            var subject = kind == PesajeReminderKind.DayBefore
                ? $"Recordatorio: mañana te toca pesaje ({freqTxt})"
                : $"Recordatorio: hoy te toca pesaje ({freqTxt})";

            var safeNombre = string.IsNullOrWhiteSpace(nombre) ? "usuario" : System.Net.WebUtility.HtmlEncode(nombre);
            var safeObj = string.IsNullOrWhiteSpace(objetivo) ? "—" : System.Net.WebUtility.HtmlEncode(objetivo);

            var body = $@"
        <div style='font-family:Arial,sans-serif'>
          <p>Hola {safeNombre},</p>

          <p>
            {(
                        kind == PesajeReminderKind.DayBefore
                            ? "Mañana"
                            : "Hoy"
                      )} te corresponde realizar tu <b>pesaje</b> según tu frecuencia <b>{freqTxt}</b>.
          </p>

          <ul>
            <li><b>Fecha del pesaje:</b> {fechaDueStr}</li>
            <li><b>Objetivo:</b> {safeObj}</li>
            <li><b>Última referencia usada:</b> {baseStr}</li>
          </ul>

          <p>Entrá a <b>CVStudioFitness</b> y registrá tu pesaje cuando lo realicés.</p>
          <p><b>CVStudioFitness</b></p>
        </div>";

            try
            {
                await _email.SendAsync(toEmail, subject, body, ct);

                reminder.Status = ReminderStatus.Sent;
                reminder.SentAtUtc = nowUtc;
                reminder.LastError = null;
                reminder.NextAttemptAt = null;

                await db.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                reminder.Status = ReminderStatus.Failed;
                reminder.Attempts += 1;
                reminder.LastError = ex.Message;

                var minutes = reminder.Attempts switch { 1 => 1, 2 => 5, 3 => 15, _ => 60 };
                reminder.NextAttemptAt = nowUtc.AddMinutes(minutes);

                await db.SaveChangesAsync(ct);
            }
        }
    }
}