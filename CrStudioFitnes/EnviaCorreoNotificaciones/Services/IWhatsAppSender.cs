using EnviaCorreoNotificaciones.Models;

namespace EnviaCorreoNotificaciones.Services
{
    public interface IWhatsAppSender
    {
        Task<string> SendReservationReminderAsync(
            string telefono,
            string nombre,
            DateTime fecha,
            TimeSpan hora,
            ReminderKind kind,
            int reservaId,
            CancellationToken ct);
    }
}
