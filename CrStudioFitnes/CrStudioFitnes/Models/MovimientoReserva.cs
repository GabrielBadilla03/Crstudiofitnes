using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CrStudioFitnes.Models
{
    [Table("movimientosreservas")]
    public class MovimientoReserva
    {
        [Key]
        public int IdMovimientoReserva { get; set; }

        [Required, StringLength(450)]
        public string IdUsuario { get; set; } = null!;

        [Required]
        public DateTime Fecha { get; set; } = DateTime.Now;

        [Required, StringLength(80)]
        public string TipoMovimiento { get; set; } = null!;

        [Required, Range(0, 1000)]
        public int LeccionesAntes { get; set; }

        [Required]
        public int CantidadMovimiento { get; set; }

        [Required, Range(0, 1000)]
        public int LeccionesDespues { get; set; }

        [Required, StringLength(500)]
        public string Motivo { get; set; } = null!;

        [Required, StringLength(450)]
        public string IdUsuarioEjecutor { get; set; } = null!;
    }
}
