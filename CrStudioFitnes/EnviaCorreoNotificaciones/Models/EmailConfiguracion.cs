using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnviaCorreoNotificaciones.Models
{
    public class EmailConfiguracion
    {
        [Key]
        public int Id { get; set; }

        public string Tipo { get; set; } = "Notificaciones";
        public bool Activo { get; set; } = true;

        public string Host { get; set; } = "";
        public int Port { get; set; } = 587;
        public bool UseSsl { get; set; } = false;
        public bool UseStartTls { get; set; } = true;

        public string FromEmail { get; set; } = "";
        public string? FromName { get; set; }

        public string Username { get; set; } = "";
        public string Password { get; set; } = "";

        public int TimeoutSeconds { get; set; } = 30;
    }
}
