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
        [Range(1, 1000, ErrorMessage = "La cantidad de lecciones por cupo debe ser mayor que cero.")]
        [Display(Name = "Lecciones por cupo")]
        public int CantLeccionesPorUsuario { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        [Range(0.01, 1000000, ErrorMessage = "El monto por cupo debe ser mayor que cero.")]
        [Display(Name = "Monto por cupo")]
        public decimal PagoPorUsuario { get; set; }

        [StringLength(200)]
        [Display(Name = "Nombre del paquete")]
        public string? Detalle { get; set; }

        [Display(Name = "Visible en catálogo")]
        public bool Activo { get; set; } = true;

        [Display(Name = "Paquete grupal")]
        public bool EsGrupal { get; set; } = false;

        [Range(1, 1000, ErrorMessage = "La cantidad de cupos debe ser mayor que cero.")]
        [Display(Name = "Cantidad de cupos")]
        public int CantidadUsuarios { get; set; } = 1;

        public ICollection<PaqueteUsuario> PaquetesUsuario { get; set; }
            = new List<PaqueteUsuario>();

        public ICollection<GrupoPaquete> GruposPaquete { get; set; }
            = new List<GrupoPaquete>();
    }
}
