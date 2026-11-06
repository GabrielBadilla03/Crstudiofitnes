using System.ComponentModel.DataAnnotations;

namespace EnviaCorreoNotificaciones.Models
{
    public class ReservaWhatsAppReminder
    {
        [Key]
        public int Id { get; set; }

        public int ReservaId { get; set; }
        public ReminderKind Kind { get; set; }
        public ReminderStatus Status { get; set; } = ReminderStatus.Pending;

        [StringLength(25)]
        public string? Phone { get; set; }

        [StringLength(255)]
        public string? Wamid { get; set; }

        [StringLength(50)]
        public string? DeliveryStatus { get; set; }

        public DateTime? DeliveryStatusAtUtc { get; set; }

        [StringLength(50)]
        public string? Response { get; set; }

        public DateTime? RespondedAtUtc { get; set; }

        [StringLength(255)]
        public string? ResponseMessageId { get; set; }

        public int Attempts { get; set; }
        public DateTime? NextAttemptAt { get; set; }

        [StringLength(1000)]
        public string? LastError { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? SentAtUtc { get; set; }
    }
}
