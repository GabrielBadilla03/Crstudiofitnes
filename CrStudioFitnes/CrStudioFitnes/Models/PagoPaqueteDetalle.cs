using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CrStudioFitnes.Models
{
    public class PagoPaqueteDetalle
    {
        [Key]
        public int IdPagoPaqueteDetalle { get; set; }

        [Required]
        public int IdPagoPaquete { get; set; }

        [Required]
        public TipoPlanDias CantDias { get; set; }

        [Range(0, 1000)]
        public int? CantLecciones { get; set; }

        [Required, Column(TypeName = "decimal(10,2)")]
        [Range(0.00, 1000000)]
        public decimal Pago { get; set; }

        [StringLength(200)]
        public string? Detalle { get; set; }

        // Nav
        public PagoPaquete PagoPaquete { get; set; } = null!;
    }
}
