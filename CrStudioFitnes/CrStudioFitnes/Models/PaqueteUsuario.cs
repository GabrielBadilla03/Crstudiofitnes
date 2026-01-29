using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CrStudioFitnes.Models
{
    public class PaqueteUsuario
    {
        [Key]
        public int IdPaqueteUsuario { get; set; }

        [Required]
        public int IdPaquete { get; set; }

        [Required]
        public string IdUsuario { get; set; } = null!;

        [Required, Range(0, 1000)]
        public int CantLecciones { get; set; }

        [Required, DataType(DataType.Date)]
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:dd/MM/yyyy}")]
        public DateTime FechaInicio { get; set; }

        [Required, DataType(DataType.Date)]
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:dd/MM/yyyy}")]
        public DateTime FechaFin { get; set; }

        // Nav
        public Paquete Paquete { get; set; } = null!;
        public ApplicationUser Usuario { get; set; } = null!;
    }
}
