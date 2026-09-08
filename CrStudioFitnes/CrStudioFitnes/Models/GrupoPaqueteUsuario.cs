using System.ComponentModel.DataAnnotations;

namespace CrStudioFitnes.Models
{
    public class GrupoPaqueteUsuario
    {
        [Key]
        public int IdGrupoPaqueteUsuario { get; set; }

        [Required]
        public int IdGrupoPaquete { get; set; }

        [Required]
        public string IdUsuario { get; set; } = null!;

        [Required]
        public int IdPaqueteUsuario { get; set; }

        [Required]
        public bool Activo { get; set; } = true;

        [Required]
        public DateTime FechaIngreso { get; set; } = DateTime.Now;

        public DateTime? FechaSalida { get; set; }

        public GrupoPaquete GrupoPaquete { get; set; } = null!;
        public ApplicationUser Usuario { get; set; } = null!;
        public PaqueteUsuario PaqueteUsuario { get; set; } = null!;
    }
}
