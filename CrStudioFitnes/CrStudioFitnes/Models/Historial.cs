using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CrStudioFitnes.Models
{
    public class Historial
    {
        [Key]
        public int IdHistorial { get; set; }

        [Required]
        public string IdUsuario { get; set; } = null!;

        [Required, DataType(DataType.Date)]
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:dd/MM/yyyy}")]
        public DateTime FechaInicio { get; set; }

        [DataType(DataType.Date)]
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:dd/MM/yyyy}")]
        public DateTime? FechaFin { get; set; }

        [Range(50, 300)]
        [Column(TypeName = "decimal(6,2)")]
        public decimal? Estatura { get; set; }

        [Range(1, 500)]
        [Column(TypeName = "decimal(6,2)")]
        public decimal? Peso { get; set; }

        [Range(1, 120)]
        public int? Edad { get; set; }

        [StringLength(50)]
        public string? Estado { get; set; }

        [StringLength(50)]
        public string? Actividad { get; set; }

        [Range(0, 14)]
        public int? Frecuencia { get; set; }

        [StringLength(120)]
        public string? Objetivo { get; set; }

        public ApplicationUser Usuario { get; set; } = null!;
        public ICollection<Pesaje> Pesajes { get; set; } = new List<Pesaje>();
    }
}
