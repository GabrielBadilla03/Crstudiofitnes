using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CrStudioFitnes.Models
{
    public class PagoPaquete
    {
        [Key]
        public int IdPagoPaquete { get; set; }

        [Required]
        public string IdUsuario { get; set; } = null!;

        [Required, DataType(DataType.DateTime)]
        public DateTime Fecha { get; set; }

        [Required, Column(TypeName = "decimal(10,2)")]
        [Range(0.01, 1000000)]
        public decimal Monto { get; set; }

        public ApplicationUser Usuario { get; set; } = null!;
        public ICollection<PagoPaqueteDetalle> Detalles { get; set; } = new List<PagoPaqueteDetalle>();
    }
}
