using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CrStudioFitnes.Models
{
    [Table("ReservaWhatsAppReminder")]
    public class ReservaWhatsAppReminder
    {
        [Key]
        public int Id { get; set; }

        public int ReservaId { get; set; }
        public byte Kind { get; set; }
        public byte Status { get; set; }

        [StringLength(25)]
        public string? Phone { get; set; }

        [StringLength(255)]
        public string? Wamid { get; set; }

        [StringLength(50)]
        public string? DeliveryStatus { get; set; }

        public DateTime? DeliveryStatusAtUtc { get; set; }

        [StringLength(50)]
        public string? Response { get; set; }

        public DateTime? RespondedAtUtc { get; set; }

        [StringLength(255)]
        public string? ResponseMessageId { get; set; }

        public int Attempts { get; set; }
        public DateTime? NextAttemptAt { get; set; }

        [StringLength(1000)]
        public string? LastError { get; set; }

        public DateTime CreatedAtUtc { get; set; }
        public DateTime? SentAtUtc { get; set; }
    }
}
