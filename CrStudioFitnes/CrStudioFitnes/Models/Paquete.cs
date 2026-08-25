using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CrStudioFitnes.Models
{
    public class Paquete
    {
        [Key]
        public int IdPaquete { get; set; }

        [Required]
        public TipoPlanDias CantDias { get; set; }

        [Required]
        [Range(1, 1000, ErrorMessage = "La cantidad total de lecciones debe ser mayor que cero.")]
        [Display(Name = "Cantidad total de lecciones")]
        public int CantLecciones { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        [Range(0.01, 1000000, ErrorMessage = "El monto total debe ser mayor que cero.")]
        [Display(Name = "Monto total del paquete")]
        public decimal Pago { get; set; }

        [Required]
        [Range(1, 1000, ErrorMessage = "La cantidad de lecciones por usuario debe ser mayor que cero.")]
        [Display(Name = "Lecciones por usuario")]
        public int CantLeccionesPorUsuario { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        [Range(0.01, 1000000, ErrorMessage = "El monto por usuario debe ser mayor que cero.")]
        [Display(Name = "Monto por usuario")]
        public decimal PagoPorUsuario { get; set; }

        [StringLength(200)]
        [Display(Name = "Nombre del paquete")]
        public string? Detalle { get; set; }

        [Display(Name = "Visible en catálogo")]
        public bool Activo { get; set; } = true;

        public ICollection<PaqueteUsuario> PaquetesUsuario { get; set; }
            = new List<PaqueteUsuario>();
    }
}