using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnviaCorreoNotificaciones.Services
{
    public class SchedulerSettings
    {
        public int PollSeconds { get; set; } = 60;
        public string TimeZoneId { get; set; } = "Central America Standard Time";
        public string NextDayReminderTime { get; set; } = "19:00";
        public int MaxAttempts { get; set; } = 5;

        public TimeSpan NextDayTime =>
            TimeSpan.TryParse(NextDayReminderTime, out var t) ? t : new TimeSpan(19, 0, 0);
    }
}
