using CrStudioFitnes.Data;
using CrStudioFitnes.Models;
using CrStudioFitnes.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Data;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace CrStudioFitnes.Controllers
{
    [ApiController]
    [Route("api/whatsapp/webhook")]
    [AllowAnonymous]
    public class WhatsAppWebhookController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly WhatsAppSettings _settings;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<WhatsAppWebhookController> _logger;

        public WhatsAppWebhookController(
            ApplicationDbContext db,
            IOptions<WhatsAppSettings> options,
            IHttpClientFactory httpClientFactory,
            ILogger<WhatsAppWebhookController> logger)
        {
            _db = db;
            _settings = options.Value;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Verify(
            [FromQuery(Name = "hub.mode")] string? mode,
            [FromQuery(Name = "hub.verify_token")] string? verifyToken,
            [FromQuery(Name = "hub.challenge")] string? challenge)
        {
            if (mode == "subscribe"
                && !string.IsNullOrWhiteSpace(_settings.VerifyToken)
                && CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(verifyToken ?? string.Empty),
                    Encoding.UTF8.GetBytes(_settings.VerifyToken)))
            {
                return Content(challenge ?? string.Empty, "text/plain");
            }

            return Unauthorized();
        }

        [HttpPost]
        public async Task<IActionResult> Receive(CancellationToken ct)
        {
            Request.EnableBuffering();

            string body;
            using (var reader = new StreamReader(
                Request.Body,
                Encoding.UTF8,
                detectEncodingFromByteOrderMarks: false,
                leaveOpen: true))
            {
                body = await reader.ReadToEndAsync(ct);
                Request.Body.Position = 0;
            }

            if (!ValidarFirma(body))
                return Unauthorized();

            try
            {
                using var json = JsonDocument.Parse(body);

                if (!json.RootElement.TryGetProperty("entry", out var entries)
                    || entries.ValueKind != JsonValueKind.Array)
                {
                    return Ok();
                }

                foreach (var entry in entries.EnumerateArray())
                {
                    if (!entry.TryGetProperty("changes", out var changes)
                        || changes.ValueKind != JsonValueKind.Array)
                    {
                        continue;
                    }

                    foreach (var change in changes.EnumerateArray())
                    {
                        if (!change.TryGetProperty("value", out var value))
                            continue;

                        await ProcesarEstadosAsync(value, ct);
                        await ProcesarMensajesAsync(value, ct);
                    }
                }

                return Ok();
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Webhook de WhatsApp con JSON inválido.");
                return BadRequest();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando webhook de WhatsApp.");
                return StatusCode(500);
            }
        }

        private bool ValidarFirma(string body)
        {
            if (string.IsNullOrWhiteSpace(_settings.AppSecret))
            {
                _logger.LogError("WhatsApp AppSecret no está configurado.");
                return false;
            }

            if (!Request.Headers.TryGetValue("X-Hub-Signature-256", out var header))
                return false;

            var received = header.ToString();
            if (!received.StartsWith("sha256=", StringComparison.OrdinalIgnoreCase))
                return false;

            var expectedBytes = HMACSHA256.HashData(
                Encoding.UTF8.GetBytes(_settings.AppSecret),
                Encoding.UTF8.GetBytes(body));

            var expected = "sha256=" + Convert.ToHexString(expectedBytes).ToLowerInvariant();

            return CryptographicOperations.FixedTimeEquals(
                Encoding.ASCII.GetBytes(received.ToLowerInvariant()),
                Encoding.ASCII.GetBytes(expected));
        }

        private async Task ProcesarEstadosAsync(JsonElement value, CancellationToken ct)
        {
            if (!value.TryGetProperty("statuses", out var statuses)
                || statuses.ValueKind != JsonValueKind.Array)
            {
                return;
            }

            foreach (var status in statuses.EnumerateArray())
            {
                var wamid = ObtenerString(status, "id");
                var estado = ObtenerString(status, "status");

                if (string.IsNullOrWhiteSpace(wamid) || string.IsNullOrWhiteSpace(estado))
                    continue;

                var reminder = await _db.ReservaWhatsAppReminders
                    .FirstOrDefaultAsync(x => x.Wamid == wamid, ct);

                if (reminder == null)
                    continue;

                reminder.DeliveryStatus = estado;
                reminder.DeliveryStatusAtUtc = DateTime.UtcNow;

                if (estado.Equals("failed", StringComparison.OrdinalIgnoreCase)
                    && status.TryGetProperty("errors", out var errors))
                {
                    var error = errors.GetRawText();
                    reminder.LastError = error.Length > 1000 ? error[..1000] : error;
                }

                await _db.SaveChangesAsync(ct);
            }
        }

        private async Task ProcesarMensajesAsync(JsonElement value, CancellationToken ct)
        {
            if (!value.TryGetProperty("messages", out var messages)
                || messages.ValueKind != JsonValueKind.Array)
            {
                return;
            }

            foreach (var message in messages.EnumerateArray())
            {
                var payload = ObtenerPayloadBoton(message);
                if (string.IsNullOrWhiteSpace(payload))
                    continue;

                if (!TryParseAccion(payload, out var accion, out var reservaId))
                    continue;

                var messageId = ObtenerString(message, "id");
                var telefonoOrigen = ObtenerString(message, "from");
                var contextWamid = message.TryGetProperty("context", out var context)
                    ? ObtenerString(context, "id")
                    : null;

                var reminder = !string.IsNullOrWhiteSpace(contextWamid)
                    ? await _db.ReservaWhatsAppReminders
                        .FirstOrDefaultAsync(x => x.Wamid == contextWamid, ct)
                    : await _db.ReservaWhatsAppReminders
                        .Where(x => x.ReservaId == reservaId)
                        .OrderByDescending(x => x.SentAtUtc)
                        .FirstOrDefaultAsync(ct);

                if (reminder == null || reminder.ReservaId != reservaId)
                    continue;

                if (!TelefonosCoinciden(reminder.Phone, telefonoOrigen))
                {
                    _logger.LogWarning(
                        "Respuesta WhatsApp ignorada por teléfono distinto. Reserva {ReservaId}.",
                        reservaId);
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(reminder.Response))
                    continue;

                if (accion == "CONFIRMAR")
                {
                    await ConfirmarReservaAsync(reminder, messageId, ct);
                }
                else
                {
                    await CancelarReservaAsync(reminder, messageId, ct);
                }
            }
        }

        private async Task ConfirmarReservaAsync(
            ReservaWhatsAppReminder reminder,
            string? responseMessageId,
            CancellationToken ct)
        {
            var reserva = await _db.Reservas
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.IdReserva == reminder.ReservaId, ct);

            if (reserva == null || !reserva.Activa)
            {
                reminder.Response = "CONFIRMACION_RECHAZADA";
                reminder.RespondedAtUtc = DateTime.UtcNow;
                reminder.ResponseMessageId = responseMessageId;
                reminder.LastError = "La reserva ya no se encuentra activa.";
                await _db.SaveChangesAsync(ct);

                await EnviarTextoAsync(
                    reminder.Phone,
                    "La reserva ya no se encuentra activa, por lo que no fue posible confirmarla.",
                    ct);
                return;
            }

            var lecciones = await _db.PaquetesUsuario
                .AsNoTracking()
                .Where(p => p.IdUsuario == reserva.IdUsuario && p.Activo)
                .OrderByDescending(p => p.IdPaqueteUsuario)
                .Select(p => (int?)p.CantLecciones)
                .FirstOrDefaultAsync(ct) ?? 0;

            reminder.Response = "CONFIRMADA";
            reminder.RespondedAtUtc = DateTime.UtcNow;
            reminder.ResponseMessageId = responseMessageId;

            _db.MovimientosReservas.Add(new MovimientoReserva
            {
                IdUsuario = reserva.IdUsuario,
                Fecha = DateTime.Now,
                TipoMovimiento = "Reserva confirmada por WhatsApp",
                LeccionesAntes = lecciones,
                CantidadMovimiento = 0,
                LeccionesDespues = lecciones,
                Motivo = "El usuario confirmó su asistencia desde el recordatorio de WhatsApp.",
                IdUsuarioEjecutor = reserva.IdUsuario
            });

            await _db.SaveChangesAsync(ct);

            await EnviarTextoAsync(
                reminder.Phone,
                "Tu reserva quedó confirmada correctamente. ¡Te esperamos en CVStudioFitness!",
                ct);
        }

        private async Task CancelarReservaAsync(
            ReservaWhatsAppReminder reminder,
            string? responseMessageId,
            CancellationToken ct)
        {
            var strategy = _db.Database.CreateExecutionStrategy();
            string mensajeRespuesta = string.Empty;

            await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _db.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable,
                    ct);

                var trackedReminder = await _db.ReservaWhatsAppReminders
                    .FirstAsync(x => x.Id == reminder.Id, ct);

                if (!string.IsNullOrWhiteSpace(trackedReminder.Response))
                    return;

                var reserva = await _db.Reservas
                    .Include(r => r.HoraReserva)
                    .FirstOrDefaultAsync(r => r.IdReserva == trackedReminder.ReservaId, ct);

                if (reserva == null || !reserva.Activa)
                {
                    trackedReminder.Response = "CANCELACION_RECHAZADA";
                    trackedReminder.RespondedAtUtc = DateTime.UtcNow;
                    trackedReminder.ResponseMessageId = responseMessageId;
                    trackedReminder.LastError = "La reserva ya no se encuentra activa.";
                    mensajeRespuesta = "La reserva ya no se encuentra activa.";
                    await _db.SaveChangesAsync(ct);
                    await tx.CommitAsync(ct);
                    return;
                }

                var fechaHoraReserva = reserva.Fecha.Date.Add(reserva.HoraReserva.Hora);

                if (fechaHoraReserva < DateTime.Now.AddHours(1))
                {
                    trackedReminder.Response = "CANCELACION_RECHAZADA";
                    trackedReminder.RespondedAtUtc = DateTime.UtcNow;
                    trackedReminder.ResponseMessageId = responseMessageId;
                    trackedReminder.LastError =
                        "La cancelación llegó con menos de una hora de anticipación.";
                    mensajeRespuesta =
                        "No fue posible cancelar porque faltan menos de 1 hora para la reserva.";
                    await _db.SaveChangesAsync(ct);
                    await tx.CommitAsync(ct);
                    return;
                }

                var paquete = await _db.PaquetesUsuario
                    .Where(pu => pu.IdUsuario == reserva.IdUsuario
                        && pu.FechaInicio.Date <= reserva.Fecha.Date
                        && pu.FechaFin.Date >= reserva.Fecha.Date)
                    .OrderByDescending(pu => pu.Activo)
                    .ThenByDescending(pu => pu.IdPaqueteUsuario)
                    .FirstOrDefaultAsync(ct);

                if (paquete == null)
                {
                    trackedReminder.Response = "CANCELACION_RECHAZADA";
                    trackedReminder.RespondedAtUtc = DateTime.UtcNow;
                    trackedReminder.ResponseMessageId = responseMessageId;
                    trackedReminder.LastError =
                        "No se encontró el paquete del cual se descontó la lección.";
                    mensajeRespuesta =
                        "No fue posible cancelar automáticamente. Por favor contactá a CVStudioFitness.";
                    await _db.SaveChangesAsync(ct);
                    await tx.CommitAsync(ct);
                    return;
                }

                var leccionesAntes = paquete.CantLecciones;

                reserva.Activa = false;
                reserva.MotivoCancelacion = "Cancelada por el usuario mediante WhatsApp";
                paquete.CantLecciones += 1;

                trackedReminder.Response = "CANCELADA";
                trackedReminder.RespondedAtUtc = DateTime.UtcNow;
                trackedReminder.ResponseMessageId = responseMessageId;
                trackedReminder.LastError = null;

                _db.MovimientosReservas.Add(new MovimientoReserva
                {
                    IdUsuario = reserva.IdUsuario,
                    Fecha = DateTime.Now,
                    TipoMovimiento = "Reserva cancelada por WhatsApp",
                    LeccionesAntes = leccionesAntes,
                    CantidadMovimiento = paquete.CantLecciones - leccionesAntes,
                    LeccionesDespues = paquete.CantLecciones,
                    Motivo =
                        $"Reserva del {reserva.Fecha:dd/MM/yyyy} a las {reserva.HoraReserva.Hora:hh\\:mm}. Cancelada desde WhatsApp.",
                    IdUsuarioEjecutor = reserva.IdUsuario
                });

                await _db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);

                mensajeRespuesta =
                    "Tu reserva fue cancelada correctamente y se devolvió 1 lección.";
            });

            if (!string.IsNullOrWhiteSpace(mensajeRespuesta))
                await EnviarTextoAsync(reminder.Phone, mensajeRespuesta, ct);
        }

        private async Task EnviarTextoAsync(
            string? telefono,
            string texto,
            CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(telefono)
                || string.IsNullOrWhiteSpace(_settings.AccessToken)
                || string.IsNullOrWhiteSpace(_settings.PhoneNumberId))
            {
                return;
            }

            try
            {
                var payload = new
                {
                    messaging_product = "whatsapp",
                    to = telefono,
                    type = "text",
                    text = new { body = texto }
                };

                using var request = new HttpRequestMessage(
                    HttpMethod.Post,
                    $"https://graph.facebook.com/{_settings.GraphApiVersion}/{_settings.PhoneNumberId}/messages");

                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", _settings.AccessToken);

                request.Content = new StringContent(
                    JsonSerializer.Serialize(payload),
                    Encoding.UTF8,
                    "application/json");

                var client = _httpClientFactory.CreateClient();
                using var response = await client.SendAsync(request, ct);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync(ct);
                    _logger.LogWarning(
                        "No se pudo responder WhatsApp. HTTP {Status}: {Error}",
                        (int)response.StatusCode,
                        error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo enviar respuesta de WhatsApp.");
            }
        }

        private static string? ObtenerPayloadBoton(JsonElement message)
        {
            var type = ObtenerString(message, "type");

            if (type == "button"
                && message.TryGetProperty("button", out var button))
            {
                return ObtenerString(button, "payload");
            }

            if (type == "interactive"
                && message.TryGetProperty("interactive", out var interactive)
                && interactive.TryGetProperty("button_reply", out var buttonReply))
            {
                return ObtenerString(buttonReply, "id");
            }

            return null;
        }

        private static bool TryParseAccion(
            string payload,
            out string accion,
            out int reservaId)
        {
            accion = string.Empty;
            reservaId = 0;

            var separator = payload.LastIndexOf('_');
            if (separator <= 0 || separator == payload.Length - 1)
                return false;

            accion = payload[..separator].Trim().ToUpperInvariant();
            if (accion != "CONFIRMAR" && accion != "CANCELAR")
                return false;

            return int.TryParse(payload[(separator + 1)..], out reservaId)
                && reservaId > 0;
        }

        private static string? ObtenerString(JsonElement element, string property)
        {
            if (!element.TryGetProperty(property, out var value)
                || value.ValueKind != JsonValueKind.String)
            {
                return null;
            }

            return value.GetString();
        }

        private static bool TelefonosCoinciden(string? esperado, string? recibido)
        {
            static string SoloDigitos(string? value) =>
                new((value ?? string.Empty).Where(char.IsDigit).ToArray());

            var a = SoloDigitos(esperado);
            var b = SoloDigitos(recibido);

            return a.Length > 0 && a == b;
        }
    }
}
