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
