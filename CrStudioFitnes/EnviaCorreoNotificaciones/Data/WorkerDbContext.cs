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
        public DbSet<AspNetRole> AspNetRoles => Set<AspNetRole>();
        public DbSet<AspNetUserRole> AspNetUserRoles => Set<AspNetUserRole>();

        public DbSet<EmailConfiguracion> EmailConfiguraciones => Set<EmailConfiguracion>();
        public DbSet<ReservaEmailReminder> ReservaEmailReminders => Set<ReservaEmailReminder>();
        public DbSet<ReservaWhatsAppReminder> ReservaWhatsAppReminders => Set<ReservaWhatsAppReminder>();
        public DbSet<StaffWhatsAppReminder> StaffWhatsAppReminders => Set<StaffWhatsAppReminder>();

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
            modelBuilder.Entity<AspNetRole>().ToTable("AspNetRoles");
            modelBuilder.Entity<AspNetUserRole>().ToTable("AspNetUserRoles");
            modelBuilder.Entity<AspNetUserRole>()
                .HasKey(x => new { x.UserId, x.RoleId });

            // Tablas del worker
            modelBuilder.Entity<EmailConfiguracion>().ToTable("EmailConfiguracion");
            modelBuilder.Entity<ReservaEmailReminder>().ToTable("ReservaEmailReminder");

            modelBuilder.Entity<ReservaEmailReminder>()
                .HasIndex(x => new { x.ReservaId, x.Kind })
                .IsUnique();

            modelBuilder.Entity<ReservaWhatsAppReminder>().ToTable("ReservaWhatsAppReminder");
            modelBuilder.Entity<ReservaWhatsAppReminder>()
                .Property(x => x.Kind).HasConversion<byte>();
            modelBuilder.Entity<ReservaWhatsAppReminder>()
                .Property(x => x.Status).HasConversion<byte>();
            modelBuilder.Entity<ReservaWhatsAppReminder>()
                .HasIndex(x => new { x.ReservaId, x.Kind })
                .IsUnique();
            modelBuilder.Entity<ReservaWhatsAppReminder>()
                .HasIndex(x => x.Wamid);

            modelBuilder.Entity<StaffWhatsAppReminder>().ToTable("StaffWhatsAppReminder");
            modelBuilder.Entity<StaffWhatsAppReminder>()
                .Property(x => x.Status).HasConversion<byte>();
            modelBuilder.Entity<StaffWhatsAppReminder>()
                .Property(x => x.Fecha).HasColumnType("date");
            modelBuilder.Entity<StaffWhatsAppReminder>()
                .Property(x => x.Hora).HasColumnType("time(0)");
            modelBuilder.Entity<StaffWhatsAppReminder>()
                .HasIndex(x => new { x.StaffUserId, x.Fecha, x.Hora })
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
