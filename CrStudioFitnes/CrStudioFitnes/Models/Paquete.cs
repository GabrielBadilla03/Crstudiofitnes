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

        [Required, Range(1, 1000, ErrorMessage = "La cantidad de lecciones no puede ser negativa.")]
        public int CantLecciones { get; set; }

        [Required, Column(TypeName = "decimal(10,2)")]
        [Range(0.01, 1000000)]
        public decimal Pago { get; set; }

        [StringLength(200)]
        public string? Detalle { get; set; }

        public bool Activo { get; set; } = true;

        public ICollection<PaqueteUsuario> PaquetesUsuario { get; set; } = new List<PaqueteUsuario>();
    }
}
