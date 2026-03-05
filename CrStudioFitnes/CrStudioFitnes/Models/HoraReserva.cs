using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CrStudioFitnes.Models
{
    public class HoraReserva
    {
        [Key]
        public int IdHora { get; set; }

        [Required, DataType(DataType.Time)]
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:HH\\:mm}")]
        [Column(TypeName = "time(0)")]
        public TimeSpan Hora { get; set; }

        // Opcional: texto para mostrar (ej. "06:00")
        [StringLength(10)]
        public string? Etiqueta { get; set; }

        public bool Activo { get; set; } = true;

        // Nav
        public ICollection<Reserva> Reservas { get; set; } = new List<Reserva>();

        public ICollection<BloqueoHorario> BloqueosHorarios { get; set; } = new List<BloqueoHorario>();

    }
}
