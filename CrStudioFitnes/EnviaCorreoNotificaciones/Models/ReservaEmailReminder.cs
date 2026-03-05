using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnviaCorreoNotificaciones.Models
{
    public enum ReminderKind : byte { NextHour = 1, NextDay = 2 }
    public enum ReminderStatus : byte { Pending = 0, Sent = 1, Failed = 2 }

    public class ReservaEmailReminder
    {
        [Key]
        public int Id { get; set; }

        public int ReservaId { get; set; }
        public ReminderKind Kind { get; set; }
        public ReminderStatus Status { get; set; } = ReminderStatus.Pending;

        public int Attempts { get; set; } = 0;
        public DateTime? NextAttemptAt { get; set; }
        public string? LastError { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? SentAtUtc { get; set; }
    }
}
