using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnviaCorreoNotificaciones.Models
{
    public class Reserva
    {
        [Key]
        public int IdReserva { get; set; }

        public string IdUsuario { get; set; } = null!;

        [Column(TypeName = "date")]
        public DateTime Fecha { get; set; }

        public int IdHora { get; set; }
    }
}
