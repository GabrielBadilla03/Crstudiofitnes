using EnviaCorreoNotificaciones.Models;
using Microsoft.EntityFrameworkCore;

namespace EnviaCorreoNotificaciones.Data
{
    public class WorkerDbContext : DbContext
    {
        public WorkerDbContext(DbContextOptions<WorkerDbContext> options) : base(options) { }

        // Tablas existentes del worker / app
        public DbSet<Reserva> Reservas => Set<Reserva>();
        public DbSet<HoraReserva> HorasReserva => Set<HoraReserva>();
        public DbSet<AspNetUser> AspNetUsers => Set<AspNetUser>();

        public DbSet<EmailConfiguracion> EmailConfiguraciones => Set<EmailConfiguracion>();
        public DbSet<ReservaEmailReminder> ReservaEmailReminders => Set<ReservaEmailReminder>();

        // ✅ NUEVO: para recordatorio de pesajes
        public DbSet<Historial> Historiales => Set<Historial>();
        public DbSet<Pesaje> Pesajes => Set<Pesaje>();

        // ✅ NUEVO tracking para recordatorios de pesaje
        public DbSet<PesajeEmailReminder> PesajeEmailReminders => Set<PesajeEmailReminder>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Tablas existentes de tu app
            modelBuilder.Entity<Reserva>().ToTable("Reservas");
            modelBuilder.Entity<HoraReserva>().ToTable("HorasReserva");
            modelBuilder.Entity<AspNetUser>().ToTable("AspNetUsers");

            // Tablas del worker
            modelBuilder.Entity<EmailConfiguracion>().ToTable("EmailConfiguracion");
            modelBuilder.Entity<ReservaEmailReminder>().ToTable("ReservaEmailReminder");

            modelBuilder.Entity<ReservaEmailReminder>()
                .HasIndex(x => new { x.ReservaId, x.Kind })
                .IsUnique();

            // ============================
            // ✅ NUEVO: Historial / Pesaje
            // ============================

            // OJO: si tus tablas se llaman distinto en SQL Server,
            // cambia "Historiales" y "Pesajes" aquí.
            modelBuilder.Entity<Historial>().ToTable("Historiales");
            modelBuilder.Entity<Pesaje>().ToTable("Pesajes");

            modelBuilder.Entity<Historial>()
                .Property(h => h.FechaInicio)
                .HasColumnType("date");

            modelBuilder.Entity<Historial>()
                .Property(h => h.FechaFin)
                .HasColumnType("date");

            // ✅ como en tu ApplicationDbContext
            modelBuilder.Entity<Historial>()
                .Property(h => h.Frecuencia)
                .HasConversion<string>();

            modelBuilder.Entity<Pesaje>()
                .Property(p => p.Fecha)
                .HasColumnType("date");

            // Relación Pesaje -> Historial
            modelBuilder.Entity<Pesaje>()
                .HasOne(p => p.Historial)
                .WithMany(h => h.Pesajes)
                .HasForeignKey(p => p.IdHistorial)
                .OnDelete(DeleteBehavior.Cascade);

            // Índices útiles (opcional pero recomendado)
            modelBuilder.Entity<Historial>()
                .HasIndex(h => new { h.IdUsuario, h.FechaInicio });

            modelBuilder.Entity<Pesaje>()
                .HasIndex(p => new { p.IdHistorial, p.Fecha });

            modelBuilder.Entity<PesajeEmailReminder>().ToTable("PesajeEmailReminder");

            modelBuilder.Entity<PesajeEmailReminder>()
                .Property(x => x.DueDate)
                .HasColumnType("date");

            modelBuilder.Entity<PesajeEmailReminder>()
                .HasIndex(x => new { x.HistorialId, x.DueDate, x.Kind })
                .IsUnique();


            modelBuilder.Entity<PesajeEmailReminder>(e =>
            {
                e.ToTable("PesajeEmailReminder");
                e.HasKey(x => x.Id);

                e.Property(x => x.Id).ValueGeneratedOnAdd();

                e.Property(x => x.DueDate).HasColumnType("date");

                // Recomendado para SQL Server
                e.Property(x => x.CreatedAtUtc).HasColumnType("datetime2");
                e.Property(x => x.SentAtUtc).HasColumnType("datetime2");
                e.Property(x => x.NextAttemptAt).HasColumnType("datetime2");

                // ✅ enums como int (tienen que ser INT en SQL)
                e.Property(x => x.Kind).HasConversion<int>();
                e.Property(x => x.Status).HasConversion<int>();

                e.HasIndex(x => new { x.HistorialId, x.DueDate, x.Kind }).IsUnique();
            });

        }
    }
}
