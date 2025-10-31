using System.ComponentModel.DataAnnotations;

namespace CrStudioFitnes.Models
{
    public class Cuerpo
    {
        [Key]
        public int IdCuerpo { get; set; }

        [Required, StringLength(60)]
        public string Nombre { get; set; } = null!; // Ej.: "Cintura", "Brazo"

        [StringLength(120)]
        public string? Detalle { get; set; }

        // Nav
        public ICollection<PesajeCuerpo> Pesajes { get; set; } = new List<PesajeCuerpo>();
    }
}
