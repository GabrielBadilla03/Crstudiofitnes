using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnviaCorreoNotificaciones.Models
{
    public class PesajeEmailReminder
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int HistorialId { get; set; }

        [Required]
        [Column(TypeName = "date")]
        public DateTime DueDate { get; set; } // la fecha "que toca" el pesaje (no el día que se envía)

        [Required]
        public PesajeReminderKind Kind { get; set; }

        [Required]
        public ReminderStatus Status { get; set; } = ReminderStatus.Pending;

        public int Attempts { get; set; } = 0;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? SentAtUtc { get; set; }
        public string? LastError { get; set; }
        public DateTime? NextAttemptAt { get; set; }
    }
}
