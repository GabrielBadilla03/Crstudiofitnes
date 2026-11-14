using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EnviaCorreoNotificaciones.Models
{
    [Table("StaffWhatsAppReminder")]
    public class StaffWhatsAppReminder
    {
        [Key]
        public int Id { get; set; }

        [StringLength(450)]
        public string StaffUserId { get; set; } = null!;

        [Column(TypeName = "date")]
        public DateTime Fecha { get; set; }

        [Column(TypeName = "time(0)")]
        public TimeSpan Hora { get; set; }

        public ReminderStatus Status { get; set; } = ReminderStatus.Pending;

        [StringLength(25)]
        public string? Phone { get; set; }

        [StringLength(255)]
        public string? Wamid { get; set; }

        public int Attempts { get; set; }
        public DateTime? NextAttemptAt { get; set; }

        [StringLength(1000)]
        public string? LastError { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? SentAtUtc { get; set; }
    }
}
