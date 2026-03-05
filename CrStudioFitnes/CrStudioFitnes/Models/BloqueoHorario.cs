using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CrStudioFitnes.Models
{
    public class BloqueoHorario
    {
        [Key]
        public int IdBloqueoHorario { get; set; }

        // Solo día (sin hora). NULL si es "bloqueo global por hora".
        [DataType(DataType.Date)]
        [Column(TypeName = "date")]
        public DateTime? Fecha { get; set; }

        // Hora del catálogo. NULL si es "día completo".
        public int? IdHora { get; set; }

        [StringLength(200)]
        public string? Motivo { get; set; }

        public bool Activo { get; set; } = true;

        // Nav opcional (porque IdHora puede ser null)
        public HoraReserva? HoraReserva { get; set; }
    }
}
