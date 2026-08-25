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
        [Range(0.00, 1000000)]
        public decimal Monto { get; set; }

        [Display(Name = "Pago activo")]
        public bool Activo { get; set; } = true;

        [StringLength(
        300,
        ErrorMessage = "El motivo de anulación no puede superar los 300 caracteres.")]
        [Display(Name = "Motivo de anulación")]
        public string? MotivoAnulacion { get; set; }

        [Required, StringLength(10)]
        public string TipoPago { get; set; } = null!;

        public ApplicationUser Usuario { get; set; } = null!;
        public ICollection<PagoPaqueteDetalle> Detalles { get; set; } = new List<PagoPaqueteDetalle>();
        public ICollection<PagoPaqueteAbono> Abonos { get; set; } = new List<PagoPaqueteAbono>();

    }
}
