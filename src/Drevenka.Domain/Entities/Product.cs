using Drevenka.Domain.Exceptions;

namespace Drevenka.Domain.Entities;

/// <summary>
/// Doménová entita reprezentující produkt (dřevěnou hračku) v sortimentu firmy Dřevěnka s.r.o.
/// </summary>
public class Product
{
    public int Id { get; internal set; }

    /// <summary>
    /// Unikátní kód produktu (SKU kód, např. "KAC-001").
    /// </summary>
    public string Code { get; private set; } = string.Empty;

    /// <summary>
    /// Název produktu.
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Podrobný popis produktu (materiál, povrchová úprava, věkové určení).
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Nákupní cena za jednotku v Kč. Nesmí být záporná.
    /// </summary>
    public decimal PurchasePrice { get; private set; }

    /// <summary>
    /// Prodejní cena za jednotku v Kč bez DPH. Nesmí být záporná.
    /// </summary>
    public decimal SellingPrice { get; private set; }

    /// <summary>
    /// Příznak, zda je produkt aktivní v nabídce.
    /// </summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>
    /// Datum a čas vytvoření záznamu.
    /// </summary>
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    /// <summary>
    /// Datum a čas poslední aktualizace.
    /// </summary>
    public DateTime? UpdatedAt { get; private set; }

    private readonly List<ProductCategory> _productCategories = [];

    /// <summary>
    /// Vazby M:N na kategorie, do kterých je tento produkt zařazen.
    /// </summary>
    public IReadOnlyCollection<ProductCategory> ProductCategories => _productCategories.AsReadOnly();

    private readonly List<StockItem> _stockItems = [];

    /// <summary>
    /// Stavy zásob produktu na jednotlivých skladech.
    /// </summary>
    public IReadOnlyCollection<StockItem> StockItems => _stockItems.AsReadOnly();

    /// <summary>
    /// Celková fyzická zásoba na všech skladech dohromady.
    /// </summary>
    public int TotalQuantityOnHand => _stockItems.Sum(s => s.QuantityOnHand);

    /// <summary>
    /// Celková disponibilní (volná) zásoba k okamžitému prodeji na e-shopu.
    /// </summary>
    public int TotalAvailableQuantity => _stockItems.Sum(s => s.AvailableQuantity);

    // Pro Entity Framework Core
    protected Product()
    {
    }

    public Product(string code, string name, string? description, decimal purchasePrice, decimal sellingPrice)
    {
        ValidateAndSet(code, name, purchasePrice, sellingPrice);
        Description = description?.Trim();
        CreatedAt = DateTime.UtcNow;
        IsActive = true;
    }

    /// <summary>
    /// Aktualizace údajů o produktu s dodržením obchodních pravidel.
    /// </summary>
    public void Update(string code, string name, string? description, decimal purchasePrice, decimal sellingPrice, bool isActive)
    {
        ValidateAndSet(code, name, purchasePrice, sellingPrice);
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

    /// <summary>
    /// Přiřazení kategorie k produktu (pokud ještě není přiřazena).
    /// </summary>
    public void AddCategory(int categoryId)
    {
        if (categoryId <= 0)
        {
            throw new DomainException("Neplatné ID kategorie.");
        }

        if (!_productCategories.Any(pc => pc.CategoryId == categoryId))
        {
            _productCategories.Add(new ProductCategory(Id, categoryId));
            UpdatedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Odebrání kategorie z produktu.
    /// </summary>
    public void RemoveCategory(int categoryId)
    {
        var existing = _productCategories.FirstOrDefault(pc => pc.CategoryId == categoryId);
        if (existing != null)
        {
            _productCategories.Remove(existing);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Synchronizace přiřazených kategorií produktu podle dodaného seznamu ID.
    /// </summary>
    public void SetCategories(IEnumerable<int> categoryIds)
    {
        var targetIds = categoryIds.Where(id => id > 0).Distinct().ToHashSet();

        // Odebereme ty, které v novém seznamu nejsou
        _productCategories.RemoveAll(pc => !targetIds.Contains(pc.CategoryId));

        // Přidáme ty, které v seznamu ještě nejsou
        foreach (var id in targetIds)
        {
            if (!_productCategories.Any(pc => pc.CategoryId == id))
            {
                _productCategories.Add(new ProductCategory(Id, id));
            }
        }

        UpdatedAt = DateTime.UtcNow;
    }

    private void ValidateAndSet(string code, string name, decimal purchasePrice, decimal sellingPrice)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new DomainException("Kód produktu (SKU) je povinný a nesmí být prázdný.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Název produktu je povinný a nesmí být prázdný.");
        }

        if (purchasePrice < 0)
        {
            throw new DomainException("Nákupní cena produktu nesmí být záporná.");
        }

        if (sellingPrice < 0)
        {
            throw new DomainException("Prodejní cena produktu nesmí být záporná.");
        }

        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        PurchasePrice = decimal.Round(purchasePrice, 2);
        SellingPrice = decimal.Round(sellingPrice, 2);
    }
}
