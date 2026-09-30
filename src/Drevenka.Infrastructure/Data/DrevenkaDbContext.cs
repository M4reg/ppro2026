using Drevenka.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Drevenka.Infrastructure.Data;

public class DrevenkaDbContext : DbContext
{
    public DrevenkaDbContext(DbContextOptions<DrevenkaDbContext> options) : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("products", t =>
            {
                t.HasCheckConstraint("ck_products_purchase_price", "\"PurchasePrice\" >= 0");
                t.HasCheckConstraint("ck_products_selling_price", "\"SellingPrice\" >= 0");
            });

            entity.HasKey(p => p.Id);

            entity.Property(p => p.Code)
                .IsRequired()
                .HasMaxLength(50);

            entity.HasIndex(p => p.Code)
                .IsUnique();

            entity.Property(p => p.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(p => p.Description)
                .HasMaxLength(2000);

            entity.Property(p => p.PurchasePrice)
                .HasPrecision(18, 2);

            entity.Property(p => p.SellingPrice)
                .HasPrecision(18, 2);

            entity.Property(p => p.IsActive)
                .HasDefaultValue(true);

            entity.Property(p => p.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });
    }
}
