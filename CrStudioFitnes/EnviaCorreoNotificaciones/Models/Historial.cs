using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

    namespace EnviaCorreoNotificaciones.Models
    {
        public class Historial
        {
            [Key]
            public int IdHistorial { get; set; }

            [Required]
            public string IdUsuario { get; set; } = null!;

            [Column(TypeName = "date")]
            public DateTime FechaInicio { get; set; }

            [Column(TypeName = "date")]
            public DateTime? FechaFin { get; set; }

            [Column(TypeName = "decimal(6,2)")]
            public decimal? Estatura { get; set; }

            [Column(TypeName = "decimal(6,2)")]
            public decimal? Peso { get; set; }

            public int? Edad { get; set; }

            [StringLength(50)]
            public string? Estado { get; set; }

            [StringLength(50)]
            public string? Actividad { get; set; }

            // ✅ OJO: en tu app se guarda como string por HasConversion<string>()
            public TipoPlanDias? Frecuencia { get; set; }

            [StringLength(120)]
            public string? Objetivo { get; set; }

            // Navs (opcional, pero útil para queries)
            public AspNetUser? Usuario { get; set; }
            public ICollection<Pesaje> Pesajes { get; set; } = new List<Pesaje>();
        }
    }
