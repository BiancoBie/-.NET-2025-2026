using Microsoft.EntityFrameworkCore;
using OrderManagementAPI.Models;

namespace OrderManagementAPI.Data
{
    /// <summary>
    /// Application database context
    /// </summary>
    public class ApplicationContext : DbContext, IApplicationContext
    {
        public DbSet<Order> Orders { get; set; } = null!;

        public ApplicationContext(DbContextOptions<ApplicationContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Order>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Author).IsRequired().HasMaxLength(100);
                entity.Property(e => e.ISBN).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Price).HasPrecision(18, 2);
                entity.HasIndex(e => e.ISBN).IsUnique();
                entity.HasIndex(e => new { e.Title, e.Author }).IsUnique();
            });
        }
    }

    /// <summary>
    /// Interface for dependency injection
    /// </summary>
    public interface IApplicationContext
    {
        DbSet<Order> Orders { get; set; }
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}