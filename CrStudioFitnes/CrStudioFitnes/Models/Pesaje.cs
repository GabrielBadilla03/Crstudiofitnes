using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CrStudioFitnes.Models
{
    public class Pesaje
    {
        [Key]
        public int IdPesaje { get; set; }

        [Required]
        public int IdHistorial { get; set; }

        [Required, DataType(DataType.Date)]
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:dd/MM/yyyy}")]
        public DateTime Fecha { get; set; }

        [Required, Range(1, 500)]
        [Column(TypeName = "decimal(6,2)")]
        public decimal Peso { get; set; } // kg

        // Navs
        public Historial Historial { get; set; } = null!;
        public ICollection<PesajeCuerpo> MedidasCuerpo { get; set; } = new List<PesajeCuerpo>();
    }
}
