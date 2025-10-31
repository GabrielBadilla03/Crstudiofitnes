using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace CrStudioFitnes.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required, StringLength(60)]
        public string Nombre { get; set; } = null!;

        [Required, StringLength(80)]
        public string Apellidos { get; set; } = null!;

        [Required, StringLength(25)]
        public string Cedula { get; set; } = null!;

        [StringLength(25)]
        public string? TelefonoPersonal { get; set; }

        [StringLength(25)]
        public string? TelefonoEmergencia { get; set; }

        [StringLength(120)]
        public string? LesionOperacion { get; set; }

        [StringLength(120)]
        public string? Patologia { get; set; }

        // Navegación
        public ICollection<Historial> Historiales { get; set; } = new List<Historial>();
        public ICollection<Reserva> Reservas { get; set; } = new List<Reserva>();
        public ICollection<PaqueteUsuario> PaquetesUsuario { get; set; } = new List<PaqueteUsuario>();
        public ICollection<PagoPaquete> PagosPaquetes { get; set; } = new List<PagoPaquete>();
    }
}