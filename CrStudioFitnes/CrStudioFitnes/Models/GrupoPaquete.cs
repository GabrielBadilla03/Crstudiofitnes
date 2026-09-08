using System.ComponentModel.DataAnnotations;

namespace CrStudioFitnes.Models
{
    public class GrupoPaquete
    {
        [Key]
        public int IdGrupoPaquete { get; set; }

        [Required]
        public int IdPaquete { get; set; }

        [Required]
        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        [Required]
        public bool Activo { get; set; } = true;

        public DateTime? FechaDesactivacion { get; set; }

        [StringLength(300)]
        public string? MotivoDesactivacion { get; set; }

        public Paquete Paquete { get; set; } = null!;
        public ICollection<GrupoPaqueteUsuario> Miembros { get; set; } = new List<GrupoPaqueteUsuario>();
        public ICollection<PagoPaquete> Pagos { get; set; } = new List<PagoPaquete>();
    }
}
