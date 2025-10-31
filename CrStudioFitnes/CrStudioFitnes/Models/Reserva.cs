using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CrStudioFitnes.Models
{
    public class Reserva
    {
        [Key]
        public int IdReserva { get; set; }

        [Required]
        public string IdUsuario { get; set; } = null!;

        // Solo FECHA (día/mes/año)
        [Required, DataType(DataType.Date)]
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:dd/MM/yyyy}")]
        public DateTime Fecha { get; set; }

        // FK a la hora disponible (catálogo)
        [Required]
        public int IdHora { get; set; }

        // Navs
        public ApplicationUser Usuario { get; set; } = null!;
        public HoraReserva HoraReserva { get; set; } = null!;

    }
}
