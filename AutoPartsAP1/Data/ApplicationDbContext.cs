using AutoPartsAP1.Components.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AutoPartsAP1.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : IdentityDbContext<ApplicationUser>(options)
    {
        public DbSet<Productos> Producto { get; set; }
        public DbSet<Ventas> Ventas { get; set; }
        public DbSet<VentasDetalles> VentasDetalle { get; set; }
        public DbSet<PagoModel> Pago { get; set; }
        public DbSet<Servicios> Servicio { get; set; }
        public DbSet<Cita> Citas { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<IdentityRole>().HasData(
                new IdentityRole
                {
                    Id = "04",
                    Name = "Admin",
                    NormalizedName = "ADMIN"
                },
                new IdentityRole
                {
                    Id = "03",
                    Name = "User",
                    NormalizedName = "USER"
                }
            );

            modelBuilder.Entity<Ventas>()
                .HasMany(v => v.VentasDetalles)
                .WithOne(d => d.Venta)
                .HasForeignKey(d => d.VentaId);

            modelBuilder.Entity<VentasDetalles>()
                .HasOne(d => d.Pago)
                .WithMany()
                .HasForeignKey(d => d.PagoId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
