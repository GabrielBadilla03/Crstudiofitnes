using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CrStudioFitnes.Models
{
    public class PagoPaqueteAbono
    {
        [Key]
        public int IdPagoPaqueteAbono { get; set; }

        // FK correcta hacia PagoPaquete
        [Required]
        public int IdPagoPaquete { get; set; }

        [Required]
        [DataType(DataType.DateTime)]
        public DateTime Fecha { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        [Range(
            0.01,
            1000000,
            ErrorMessage = "El monto del abono debe ser mayor que cero.")]
        public decimal Monto { get; set; }

        // Navegación
        [ForeignKey(nameof(IdPagoPaquete))]
        public PagoPaquete PagoPaquete { get; set; } = null!;
    }
}