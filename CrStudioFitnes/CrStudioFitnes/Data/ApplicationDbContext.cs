// Data/ApplicationDbContext.cs
using System;
using System.Collections.Generic;
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

        // DbSets
        public DbSet<Cuerpo> Cuerpos => Set<Cuerpo>();
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

            // -------- ApplicationUser
            modelBuilder.Entity<ApplicationUser>()
                .HasIndex(u => u.Cedula)
                .IsUnique();

            // -------- Paquete
            modelBuilder.Entity<Paquete>()
                .Property(p => p.CantDias)
                .HasConversion<string>(); // enum -> texto

            // -------- PaqueteUsuario
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
                .Property(pu => pu.Fecha)
                .HasColumnType("date"); // corrige anotación

            // -------- PagoPaquete / Detalle
            modelBuilder.Entity<PagoPaquete>()
                .HasOne(pp => pp.Usuario)
                .WithMany(u => u.PagosPaquetes)
                .HasForeignKey(pp => pp.IdUsuario)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PagoPaquete>()
                .Property(p => p.Monto)
                .HasPrecision(10, 2);

            modelBuilder.Entity<PagoPaqueteDetalle>()
                .Property(d => d.CantDias)
                .HasConversion<string>(); // enum -> texto

            modelBuilder.Entity<PagoPaqueteDetalle>()
                .Property(d => d.Pago)
                .HasPrecision(10, 2);

            modelBuilder.Entity<PagoPaqueteDetalle>()
                .HasOne(d => d.PagoPaquete)
                .WithMany(h => h.Detalles)
                .HasForeignKey(d => d.IdPagoPaquete)
                .OnDelete(DeleteBehavior.Cascade);

            // -------- Reserva (date + catálogo de horas)
            modelBuilder.Entity<Reserva>()
                .Property(r => r.Fecha)
                .HasColumnType("date");

            modelBuilder.Entity<Reserva>()
                .HasOne(r => r.Usuario)
                .WithMany(u => u.Reservas)
                .HasForeignKey(r => r.IdUsuario)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Reserva>()
                .HasOne(r => r.HoraReserva)
                .WithMany(h => h.Reservas)
                .HasForeignKey(r => r.IdHora)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Reserva>()
                .HasIndex(r => new { r.IdUsuario, r.Fecha, r.IdHora })
                .IsUnique();

            // -------- HoraReserva
            modelBuilder.Entity<HoraReserva>()
                .Property(h => h.Hora)
                .HasColumnType("time(0)");

            modelBuilder.Entity<HoraReserva>()
                .HasIndex(h => h.Hora)
                .IsUnique();

            // -------- Historial / Pesaje / Cuerpo / PesajeCuerpo
            modelBuilder.Entity<Historial>()
                .HasOne(h => h.Usuario)
                .WithMany(u => u.Historiales)
                .HasForeignKey(h => h.IdUsuario)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Historial>()
                .Property(h => h.FechaInicio).HasColumnType("date"); // corrige anotación
            modelBuilder.Entity<Historial>()
                .Property(h => h.FechaFin).HasColumnType("date");    // corrige anotación

            modelBuilder.Entity<Pesaje>()
                .HasOne(p => p.Historial)
                .WithMany(h => h.Pesajes)
                .HasForeignKey(p => p.IdHistorial)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Pesaje>()
                .Property(p => p.Fecha)
                .HasColumnType("date"); // corrige anotación

            modelBuilder.Entity<Cuerpo>()
                .HasIndex(c => c.Nombre)
                .IsUnique();

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
        }
    }
}