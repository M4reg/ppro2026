using Drevenka.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Drevenka.Infrastructure.Data;

public class DrevenkaDbContext : DbContext
{
    public DrevenkaDbContext(DbContextOptions<DrevenkaDbContext> options) : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<StockItem> StockItems => Set<StockItem>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Product
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

            entity.HasMany(p => p.ProductCategories)
                .WithOne(pc => pc.Product)
                .HasForeignKey(pc => pc.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(p => p.StockItems)
                .WithOne(si => si.Product)
                .HasForeignKey(si => si.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Category
        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("categories");

            entity.HasKey(c => c.Id);

            entity.Property(c => c.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.HasIndex(c => c.Name)
                .IsUnique();

            entity.Property(c => c.Description)
                .HasMaxLength(500);

            entity.Property(c => c.IsActive)
                .HasDefaultValue(true);

            entity.Property(c => c.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasMany(c => c.ProductCategories)
                .WithOne(pc => pc.Category)
                .HasForeignKey(pc => pc.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ProductCategory (M:N)
        modelBuilder.Entity<ProductCategory>(entity =>
        {
            entity.ToTable("product_categories");

            entity.HasKey(pc => new { pc.ProductId, pc.CategoryId });

            entity.Property(pc => pc.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(pc => pc.Product)
                .WithMany(p => p.ProductCategories)
                .HasForeignKey(pc => pc.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(pc => pc.Category)
                .WithMany(c => c.ProductCategories)
                .HasForeignKey(pc => pc.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Warehouse
        modelBuilder.Entity<Warehouse>(entity =>
        {
            entity.ToTable("warehouses");

            entity.HasKey(w => w.Id);

            entity.Property(w => w.Code)
                .IsRequired()
                .HasMaxLength(30);

            entity.HasIndex(w => w.Code)
                .IsUnique();

            entity.Property(w => w.Name)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(w => w.Location)
                .HasMaxLength(300);

            entity.Property(w => w.IsMain)
                .HasDefaultValue(false);

            entity.Property(w => w.IsActive)
                .HasDefaultValue(true);

            entity.Property(w => w.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasMany(w => w.StockItems)
                .WithOne(si => si.Warehouse)
                .HasForeignKey(si => si.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // StockItem
        modelBuilder.Entity<StockItem>(entity =>
        {
            entity.ToTable("stock_items", t =>
            {
                // Integritní check omezení zákazu výdeje do záporu
                t.HasCheckConstraint("ck_stock_items_quantity_on_hand", "\"QuantityOnHand\" >= 0");
                t.HasCheckConstraint("ck_stock_items_reserved_quantity", "\"ReservedQuantity\" >= 0");
            });

            entity.HasKey(si => si.Id);

            entity.HasIndex(si => new { si.ProductId, si.WarehouseId })
                .IsUnique();

            entity.Property(si => si.QuantityOnHand)
                .HasDefaultValue(0);

            entity.Property(si => si.ReservedQuantity)
                .HasDefaultValue(0);

            entity.Property(si => si.MinStockLevel)
                .HasDefaultValue(5);

            entity.Property(si => si.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(si => si.Product)
                .WithMany(p => p.StockItems)
                .HasForeignKey(si => si.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(si => si.Warehouse)
                .WithMany(w => w.StockItems)
                .HasForeignKey(si => si.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(si => si.Movements)
                .WithOne(sm => sm.StockItem)
                .HasForeignKey(sm => sm.StockItemId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // StockMovement
        modelBuilder.Entity<StockMovement>(entity =>
        {
            entity.ToTable("stock_movements");

            entity.HasKey(sm => sm.Id);

            entity.Property(sm => sm.MovementType)
                .IsRequired();

            entity.Property(sm => sm.Quantity)
                .IsRequired();

            entity.Property(sm => sm.ReferenceNumber)
                .HasMaxLength(100);

            entity.Property(sm => sm.Note)
                .HasMaxLength(500);

            entity.Property(sm => sm.PerformedBy)
                .IsRequired()
                .HasMaxLength(100)
                .HasDefaultValue("Petr Doležal");

            entity.Property(sm => sm.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(sm => sm.CreatedAt);

            entity.HasOne(sm => sm.Product)
                .WithMany()
                .HasForeignKey(sm => sm.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(sm => sm.Warehouse)
                .WithMany()
                .HasForeignKey(sm => sm.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
