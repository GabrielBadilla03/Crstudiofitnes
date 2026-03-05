using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnviaCorreoNotificaciones.Models
{
    public class HoraReserva
    {
        [Key]
        public int IdHora { get; set; }

        [Column(TypeName = "time(0)")]
        public TimeSpan Hora { get; set; }

        public string? Etiqueta { get; set; }
    }
}
