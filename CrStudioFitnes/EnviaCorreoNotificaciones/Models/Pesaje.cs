using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnviaCorreoNotificaciones.Models
{
    public class Pesaje
    {
        [Key]
        public int IdPesaje { get; set; }

        [Required]
        public int IdHistorial { get; set; }

        [Column(TypeName = "date")]
        public DateTime Fecha { get; set; }

        [Required]
        [Column(TypeName = "decimal(6,2)")]
        public decimal Peso { get; set; }

        // Nav
        public Historial Historial { get; set; } = null!;
    }
}
