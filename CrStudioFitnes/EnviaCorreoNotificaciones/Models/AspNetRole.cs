using System.ComponentModel.DataAnnotations;

namespace EnviaCorreoNotificaciones.Models
{
    public class AspNetRole
    {
        [Key]
        public string Id { get; set; } = null!;

        public string? Name { get; set; }
        public string? NormalizedName { get; set; }
    }
}
