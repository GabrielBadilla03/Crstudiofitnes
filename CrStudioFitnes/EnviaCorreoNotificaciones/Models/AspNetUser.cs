using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnviaCorreoNotificaciones.Models
{
    public class AspNetUser
    {
        [Key]
        public string Id { get; set; } = null!;

        public string? Email { get; set; }

        public string? Nombre { get; set; }
        public string? Apellidos { get; set; }
    }
}
