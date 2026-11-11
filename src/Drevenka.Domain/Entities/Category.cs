using Drevenka.Domain.Exceptions;

namespace Drevenka.Domain.Entities;

/// <summary>
/// Doménová entita reprezentující kategorii produktů (např. "Pro nejmenší", "Dárky do 300 Kč").
/// </summary>
public class Category
{
    public int Id { get; internal set; }

    /// <summary>
    /// Název kategorie. Musí být unikátní a neprázdný.
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Volitelný stručný popis kategorie.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Příznak, zda je kategorie aktivní.
    /// </summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>
    /// Datum a čas vytvoření kategorie.
    /// </summary>
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    /// <summary>
    /// Datum a čas poslední aktualizace kategorie.
    /// </summary>
    public DateTime? UpdatedAt { get; private set; }

    private readonly List<ProductCategory> _productCategories = [];

    /// <summary>
    /// Vazby M:N na produkty zařazené do této kategorie.
    /// </summary>
    public IReadOnlyCollection<ProductCategory> ProductCategories => _productCategories.AsReadOnly();

    // Pro Entity Framework Core
    protected Category()
    {
    }

    public Category(string name, string? description = null)
    {
        ValidateAndSetName(name);
        Description = description?.Trim();
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(string name, string? description, bool isActive)
    {
        ValidateAndSetName(name);
        Description = description?.Trim();
        IsActive = isActive;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    private void ValidateAndSetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Název kategorie je povinný a nesmí být prázdný.");
        }

        Name = name.Trim();
    }
}

