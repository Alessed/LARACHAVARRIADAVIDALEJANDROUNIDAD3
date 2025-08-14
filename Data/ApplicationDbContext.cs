using Dentalara.Models;
using Microsoft.EntityFrameworkCore;

namespace Dentalara.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Recuperacion> Recuperaciones { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Usuario>(entity =>
            {
                entity.HasKey(u => u.IdUsuario);

                entity.Property(u => u.IdUsuario)
                    .ValueGeneratedOnAdd()
                    .HasColumnType("int")
                    .HasConversion<int>(); // Conversión explícita

                // Configuración para evitar cualquier problema de conversión
                entity.Property(u => u.TipoUsuario)
                    .HasConversion<string>(); // Si es necesario
            });
        }
    }
}