namespace EnviaCorreoNotificaciones.Services
{
    public class WhatsAppSettings
    {
        public bool Enabled { get; set; } = false;
        public string GraphApiVersion { get; set; } = "v25.0";
        public string PhoneNumberId { get; set; } = string.Empty;
        public string AccessToken { get; set; } = string.Empty;
        public string TemplateNextDay { get; set; } = "recordatorio_reserva_confirmacion";
        public string TemplateNextHour { get; set; } = "recordatorio_reserva_proxima";
        public string LanguageCode { get; set; } = "es";
        public string VerifyToken { get; set; } = string.Empty;
        public string AppSecret { get; set; } = string.Empty;
    }
}
