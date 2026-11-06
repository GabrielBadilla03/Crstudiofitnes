using EnviaCorreoNotificaciones.Models;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace EnviaCorreoNotificaciones.Services
{
    public class MetaWhatsAppSender : IWhatsAppSender
    {
        private readonly HttpClient _http;
        private readonly WhatsAppSettings _settings;

        public MetaWhatsAppSender(HttpClient http, IOptions<WhatsAppSettings> options)
        {
            _http = http;
            _settings = options.Value;
        }

        public async Task<string> SendReservationReminderAsync(
            string telefono,
            string nombre,
            DateTime fecha,
            TimeSpan hora,
            ReminderKind kind,
            int reservaId,
            CancellationToken ct)
        {
            if (!_settings.Enabled)
                throw new InvalidOperationException("WhatsApp está deshabilitado.");

            if (string.IsNullOrWhiteSpace(_settings.PhoneNumberId)
                || string.IsNullOrWhiteSpace(_settings.AccessToken)
                || string.IsNullOrWhiteSpace(_settings.GraphApiVersion))
            {
                throw new InvalidOperationException("La configuración de WhatsApp está incompleta.");
            }

            var phone = NormalizarTelefonoCostaRica(telefono);
            var template = kind == ReminderKind.NextDay
                ? _settings.TemplateNextDay
                : _settings.TemplateNextHour;

            if (string.IsNullOrWhiteSpace(template))
                throw new InvalidOperationException("No se configuró la plantilla de WhatsApp.");

            var horaStr = DateTime.Today.Add(hora).ToString("hh:mm tt");
            var fechaStr = fecha.ToString("dd/MM/yyyy");
            var nombreSeguro = string.IsNullOrWhiteSpace(nombre) ? "usuario" : nombre.Trim();

            var components = new List<object>
            {
                new
                {
                    type = "body",
                    parameters = new object[]
                    {
                        new { type = "text", text = nombreSeguro },
                        new { type = "text", text = fechaStr },
                        new { type = "text", text = horaStr }
                    }
                }
            };

            if (kind == ReminderKind.NextDay)
            {
                components.Add(new
                {
                    type = "button",
                    sub_type = "quick_reply",
                    index = "0",
                    parameters = new object[]
                    {
                        new { type = "payload", payload = $"CONFIRMAR_{reservaId}" }
                    }
                });

                components.Add(new
                {
                    type = "button",
                    sub_type = "quick_reply",
                    index = "1",
                    parameters = new object[]
                    {
                        new { type = "payload", payload = $"CANCELAR_{reservaId}" }
                    }
                });
            }

            var payload = new
            {
                messaging_product = "whatsapp",
                to = phone,
                type = "template",
                template = new
                {
                    name = template,
                    language = new { code = _settings.LanguageCode },
                    components
                }
            };

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"https://graph.facebook.com/{_settings.GraphApiVersion}/{_settings.PhoneNumberId}/messages");

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.AccessToken);
            request.Content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");

            using var response = await _http.SendAsync(request, ct);
            var body = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"Meta WhatsApp respondió {(int)response.StatusCode}: {body}");

            using var json = JsonDocument.Parse(body);
            var wamid = json.RootElement
                .GetProperty("messages")[0]
                .GetProperty("id")
                .GetString();

            if (string.IsNullOrWhiteSpace(wamid))
                throw new InvalidOperationException("Meta no devolvió el identificador wamid.");

            return wamid;
        }

        public static string NormalizarTelefonoCostaRica(string telefono)
        {
            var digits = new string((telefono ?? string.Empty).Where(char.IsDigit).ToArray());

            if (digits.StartsWith("506") && digits.Length == 11)
                return digits;

            if (digits.Length == 8)
                return "506" + digits;

            throw new InvalidOperationException(
                $"El teléfono '{telefono}' no tiene un formato válido para Costa Rica.");
        }
    }
}
