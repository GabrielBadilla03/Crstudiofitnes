using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CrStudioFitnes.Models
{
    public class PesajeCuerpo
    {
        [Required]
        public int IdPesaje { get; set; }

        [Required]
        public int IdCuerpo { get; set; }

        [Required, Range(0, 500)]
        [Column(TypeName = "decimal(7,2)")]
        public decimal Medida { get; set; } // cm

        // Navs
        public Pesaje Pesaje { get; set; } = null!;
        public Cuerpo Cuerpo { get; set; } = null!;
    }
}
