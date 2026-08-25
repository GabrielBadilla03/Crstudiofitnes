using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CrStudioFitnes.Models
{
    public class Reserva
    {
        [Key]
        public int IdReserva { get; set; }

        // Usuario para quien se hizo la reserva
        [Required]
        public string IdUsuario { get; set; } = null!;

        // Usuario que realizó o registró la reserva
        [Required]
        public string IdUsuarioReserva { get; set; } = null!;

        // Solo fecha: día/mes/año
        [Required]
        [DataType(DataType.Date)]
        [DisplayFormat(
            ApplyFormatInEditMode = true,
            DataFormatString = "{0:dd/MM/yyyy}")]
        public DateTime Fecha { get; set; }

        // FK a la hora disponible
        [Required]
        public int IdHora { get; set; }

        [Display(Name = "Reserva activa")]
        public bool Activa { get; set; } = true;

        [StringLength(
            300,
            ErrorMessage = "El motivo de cancelación no puede superar los 300 caracteres.")]
        [Display(Name = "Motivo de cancelación")]
        public string? MotivoCancelacion { get; set; }

        // Usuario para quien se hizo la reserva
        [ForeignKey(nameof(IdUsuario))]
        public ApplicationUser Usuario { get; set; } = null!;

        // Usuario que registró la reserva
        [ForeignKey(nameof(IdUsuarioReserva))]
        public ApplicationUser UsuarioReserva { get; set; } = null!;

        public HoraReserva HoraReserva { get; set; } = null!;
    }
}