using Drevenka.Domain.Exceptions;

namespace Drevenka.Domain.Entities;

/// <summary>
/// Doménová entita reprezentující fyzický sklad (např. Centrála Hradec Králové, Garáž Třebechovice).
/// </summary>
public class Warehouse
{
    public int Id { get; internal set; }

    /// <summary>
    /// Kód skladu (např. "HK-01", "TREB-01").
    /// </summary>
    public string Code { get; private set; } = string.Empty;

    /// <summary>
    /// Název skladu.
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Popis nebo adresa umístění skladu.
    /// </summary>
    public string? Location { get; private set; }

    /// <summary>
    /// Příznak, zda se jedná o primární expediční sklad (centrálu).
    /// </summary>
    public bool IsMain { get; private set; }

    /// <summary>
    /// Příznak, zda je sklad aktivní v provozu.
    /// </summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>
    /// Datum založení skladu v systému.
    /// </summary>
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    /// <summary>
    /// Datum poslední úpravy.
    /// </summary>
    public DateTime? UpdatedAt { get; private set; }

    private readonly List<StockItem> _stockItems = [];
    public IReadOnlyCollection<StockItem> StockItems => _stockItems.AsReadOnly();

    // Pro Entity Framework Core
    protected Warehouse()
    {
    }

    public Warehouse(string code, string name, string? location = null, bool isMain = false)
    {
        ValidateAndSet(code, name);
        Location = location?.Trim();
        IsMain = isMain;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(string code, string name, string? location, bool isMain, bool isActive)
    {
        ValidateAndSet(code, name);
        Location = location?.Trim();
        IsMain = isMain;
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

    private void ValidateAndSet(string code, string name)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new DomainException("Kód skladu je povinný a nesmí být prázdný.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Název skladu je povinný a nesmí být prázdný.");
        }

        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
    }
}

