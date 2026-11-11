namespace Drevenka.Domain.Entities;

/// <summary>
/// Vazební entita pro relaci M:N mezi produkty a kategoriemi.
/// </summary>
public class ProductCategory
{
    public int ProductId { get; private set; }
    public Product Product { get; private set; } = null!;

    public int CategoryId { get; private set; }
    public Category Category { get; private set; } = null!;

    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    // Pro Entity Framework Core
    protected ProductCategory()
    {
    }

    public ProductCategory(int productId, int categoryId)
    {
        ProductId = productId;
        CategoryId = categoryId;
        CreatedAt = DateTime.UtcNow;
    }

    public ProductCategory(Product product, Category category)
    {
        Product = product;
        ProductId = product.Id;
        Category = category;
        CategoryId = category.Id;
        CreatedAt = DateTime.UtcNow;
    }
}

