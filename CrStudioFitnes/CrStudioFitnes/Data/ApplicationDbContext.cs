using CrStudioFitnes.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CrStudioFitnes.Data
{
    public class ApplicationDbContext
        : IdentityDbContext<ApplicationUser, IdentityRole, string>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<BloqueoHorario> BloqueosHorarios { get; set; } = null!;
        public DbSet<Cuerpo> Cuerpos => Set<Cuerpo>();
        public DbSet<GrupoPaquete> GruposPaquete => Set<GrupoPaquete>();
        public DbSet<GrupoPaqueteUsuario> GruposPaqueteUsuario => Set<GrupoPaqueteUsuario>();
        public DbSet<Historial> Historiales => Set<Historial>();
        public DbSet<HoraReserva> HorasReserva => Set<HoraReserva>();
        public DbSet<PagoPaquete> PagosPaquete => Set<PagoPaquete>();
        public DbSet<PagoPaqueteDetalle> PagosPaqueteDetalle => Set<PagoPaqueteDetalle>();
        public DbSet<Paquete> Paquetes => Set<Paquete>();
        public DbSet<PaqueteUsuario> PaquetesUsuario => Set<PaqueteUsuario>();
        public DbSet<Pesaje> Pesajes => Set<Pesaje>();
        public DbSet<PesajeCuerpo> PesajesCuerpo => Set<PesajeCuerpo>();
        public DbSet<Reserva> Reservas => Set<Reserva>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ApplicationUser>()
                .HasIndex(u => u.Cedula)
                .IsUnique();

            // =====================================================
            // Paquete
            // =====================================================
            modelBuilder.Entity<Paquete>()
                .Property(p => p.CantDias)
                .HasConversion<string>();

            modelBuilder.Entity<Paquete>()
                .Property(p => p.Pago)
                .HasPrecision(10, 2);

            modelBuilder.Entity<Paquete>()
                .Property(p => p.PagoPorUsuario)
                .HasPrecision(10, 2);

            modelBuilder.Entity<Paquete>()
                .Property(p => p.EsGrupal)
                .HasDefaultValue(false);

            modelBuilder.Entity<Paquete>()
                .Property(p => p.CantidadUsuarios)
                .HasDefaultValue(1);

            // =====================================================
            // PaqueteUsuario
            // =====================================================
            modelBuilder.Entity<PaqueteUsuario>()
                .HasOne(pu => pu.Paquete)
                .WithMany(p => p.PaquetesUsuario)
                .HasForeignKey(pu => pu.IdPaquete)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PaqueteUsuario>()
                .HasOne(pu => pu.Usuario)
                .WithMany(u => u.PaquetesUsuario)
                .HasForeignKey(pu => pu.IdUsuario)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PaqueteUsuario>()
                .Property(pu => pu.FechaInicio)
                .HasColumnType("date");

            modelBuilder.Entity<PaqueteUsuario>()
                .Property(pu => pu.FechaFin)
                .HasColumnType("date");

            modelBuilder.Entity<PaqueteUsuario>()
                .Property(pu => pu.Activo)
                .HasDefaultValue(true);

            modelBuilder.Entity<PaqueteUsuario>()
                .HasIndex(pu => new { pu.IdUsuario, pu.Activo });

            // =====================================================
            // GrupoPaquete
            // =====================================================
            modelBuilder.Entity<GrupoPaquete>(entity =>
            {
                entity.ToTable("GrupoPaquete");

                entity.HasOne(g => g.Paquete)
                    .WithMany(p => p.GruposPaquete)
                    .HasForeignKey(g => g.IdPaquete)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(g => g.Activo)
                    .HasDefaultValue(true);

                entity.HasIndex(g => new { g.IdPaquete, g.Activo });
            });

            // =====================================================
            // GrupoPaqueteUsuario
            // =====================================================
            modelBuilder.Entity<GrupoPaqueteUsuario>(entity =>
            {
                entity.ToTable("GrupoPaqueteUsuario");

                entity.HasOne(m => m.GrupoPaquete)
                    .WithMany(g => g.Miembros)
                    .HasForeignKey(m => m.IdGrupoPaquete)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(m => m.Usuario)
                    .WithMany()
                    .HasForeignKey(m => m.IdUsuario)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(m => m.PaqueteUsuario)
                    .WithMany(pu => pu.GruposUsuario)
                    .HasForeignKey(m => m.IdPaqueteUsuario)
                    .IsRequired()
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(m => m.Activo)
                    .HasDefaultValue(true);

                // Un usuario solamente puede pertenecer a un grupo activo a la vez.
                entity.HasIndex(m => m.IdUsuario)
                    .IsUnique()
                    .HasFilter("[Activo] = 1");

                entity.HasIndex(m => new { m.IdGrupoPaquete, m.Activo });
            });

            // =====================================================
            // PagoPaquete
            // =====================================================
            modelBuilder.Entity<PagoPaquete>()
                .HasOne(pp => pp.Usuario)
                .WithMany(u => u.PagosPaquetes)
                .HasForeignKey(pp => pp.IdUsuario)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PagoPaquete>()
                .HasOne(pp => pp.GrupoPaquete)
                .WithMany(g => g.Pagos)
                .HasForeignKey(pp => pp.IdGrupoPaquete)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PagoPaquete>()
                .HasOne(pp => pp.PaqueteUsuario)
                .WithMany(pu => pu.Pagos)
                .HasForeignKey(pp => pp.IdPaqueteUsuario)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PagoPaquete>()
                .Property(p => p.Monto)
                .HasPrecision(10, 2);

            modelBuilder.Entity<PagoPaquete>()
                .Property(p => p.Activo)
                .HasDefaultValue(true);

            modelBuilder.Entity<PagoPaquete>()
                .HasIndex(p => p.IdOperacionGrupo);

            // =====================================================
            // PagoPaqueteAbono
            // =====================================================
            modelBuilder.Entity<PagoPaqueteAbono>(entity =>
            {
                entity.ToTable("PagoPaqueteAbono");

                entity.HasOne(a => a.PagoPaquete)
                    .WithMany(p => p.Abonos)
                    .HasForeignKey(a => a.IdPagoPaquete)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.Property(a => a.Monto)
                    .HasPrecision(10, 2);
            });

            // =====================================================
            // PagoPaqueteDetalle
            // =====================================================
            modelBuilder.Entity<PagoPaqueteDetalle>()
                .Property(d => d.CantDias)
                .HasConversion<string>();

            modelBuilder.Entity<PagoPaqueteDetalle>()
                .Property(d => d.Pago)
                .HasPrecision(10, 2);

            modelBuilder.Entity<PagoPaqueteDetalle>()
                .HasOne(d => d.PagoPaquete)
                .WithMany(p => p.Detalles)
                .HasForeignKey(d => d.IdPagoPaquete)
                .OnDelete(DeleteBehavior.Cascade);

            // =====================================================
            // Reserva
            // =====================================================
            modelBuilder.Entity<Reserva>()
                .Property(r => r.Fecha)
                .HasColumnType("date");

            modelBuilder.Entity<Reserva>()
                .Property(r => r.Activa)
                .HasDefaultValue(true);

            modelBuilder.Entity<Reserva>()
                .HasOne(r => r.Usuario)
                .WithMany(u => u.Reservas)
                .HasForeignKey(r => r.IdUsuario)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Reserva>()
                .HasOne(r => r.UsuarioReserva)
                .WithMany(u => u.ReservasCreadas)
                .HasForeignKey(r => r.IdUsuarioReserva)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Reserva>()
                .HasOne(r => r.HoraReserva)
                .WithMany(h => h.Reservas)
                .HasForeignKey(r => r.IdHora)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Reserva>()
                .HasIndex(r => new { r.IdUsuario, r.Fecha, r.IdHora });

            modelBuilder.Entity<Reserva>()
                .HasIndex(r => new { r.Fecha, r.IdHora });

            // =====================================================
            // HoraReserva
            // =====================================================
            modelBuilder.Entity<HoraReserva>()
                .Property(h => h.Hora)
                .HasColumnType("time(0)");

            modelBuilder.Entity<HoraReserva>()
                .HasIndex(h => h.Hora)
                .IsUnique();

            // =====================================================
            // Historial
            // =====================================================
            modelBuilder.Entity<Historial>()
                .HasOne(h => h.Usuario)
                .WithMany(u => u.Historiales)
                .HasForeignKey(h => h.IdUsuario)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Historial>()
                .Property(h => h.FechaInicio)
                .HasColumnType("date");

            modelBuilder.Entity<Historial>()
                .Property(h => h.FechaFin)
                .HasColumnType("date");

            modelBuilder.Entity<Historial>()
                .Property(h => h.Frecuencia)
                .HasConversion<string>();

            // =====================================================
            // Pesaje
            // =====================================================
            modelBuilder.Entity<Pesaje>()
                .HasOne(p => p.Historial)
                .WithMany(h => h.Pesajes)
                .HasForeignKey(p => p.IdHistorial)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Pesaje>()
                .Property(p => p.Fecha)
                .HasColumnType("date");

            // =====================================================
            // Cuerpo
            // =====================================================
            modelBuilder.Entity<Cuerpo>()
                .HasIndex(c => c.Nombre)
                .IsUnique();

            // =====================================================
            // PesajeCuerpo
            // =====================================================
            modelBuilder.Entity<PesajeCuerpo>()
                .HasKey(pc => new { pc.IdPesaje, pc.IdCuerpo });

            modelBuilder.Entity<PesajeCuerpo>()
                .HasOne(pc => pc.Pesaje)
                .WithMany(p => p.MedidasCuerpo)
                .HasForeignKey(pc => pc.IdPesaje)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PesajeCuerpo>()
                .HasOne(pc => pc.Cuerpo)
                .WithMany(c => c.Pesajes)
                .HasForeignKey(pc => pc.IdCuerpo)
                .OnDelete(DeleteBehavior.Restrict);

            // =====================================================
            // BloqueoHorario
            // =====================================================
            modelBuilder.Entity<BloqueoHorario>(entity =>
            {
                entity.ToTable("BloqueosHorarios");

                entity.HasOne(b => b.HoraReserva)
                    .WithMany(h => h.BloqueosHorarios)
                    .HasForeignKey(b => b.IdHora)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.ToTable(tableBuilder =>
                    tableBuilder.HasCheckConstraint(
                        "CK_BloqueosHorarios_FechaOrHora",
                        "[Fecha] IS NOT NULL OR [IdHora] IS NOT NULL"));

                entity.HasIndex(x => x.IdHora)
                    .IsUnique()
                    .HasFilter("[Activo] = 1 AND [Fecha] IS NULL AND [IdHora] IS NOT NULL");

                entity.HasIndex(x => x.Fecha)
                    .IsUnique()
                    .HasFilter("[Activo] = 1 AND [Fecha] IS NOT NULL AND [IdHora] IS NULL");

                entity.HasIndex(x => new { x.Fecha, x.IdHora })
                    .IsUnique()
                    .HasFilter("[Activo] = 1 AND [Fecha] IS NOT NULL AND [IdHora] IS NOT NULL");
            });
        }
    }
}
