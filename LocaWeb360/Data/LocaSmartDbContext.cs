using Microsoft.EntityFrameworkCore;
using LocaSmart360.Models;

namespace LocaSmart360.Data
{
    public class LocaSmartDbContext : DbContext
    {
        public LocaSmartDbContext(DbContextOptions<LocaSmartDbContext> options) : base(options)
        {
        }

        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Produto> Produtos { get; set; }
        public DbSet<Venda> Vendas { get; set; }
        public DbSet<EtlLog> EtlLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Usuario>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<Venda>()
                .Property(v => v.ValorTotal)
                .HasColumnType("decimal(18,2)");
        }
    }
}