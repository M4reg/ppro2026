using Drevenka.Domain.Exceptions;

namespace Drevenka.Domain.Entities;

/// <summary>
/// Doménová entita reprezentující skladovou zásobu konkrétního produktu na konkrétním fyzickém skladě.
/// Zajišťuje striktní dodržování pravidla nezápornosti a dvoustupňový model zásob.
/// </summary>
public class StockItem
{
    public int Id { get; internal set; }

    public int ProductId { get; private set; }
    public Product Product { get; private set; } = null!;

    public int WarehouseId { get; private set; }
    public Warehouse Warehouse { get; private set; } = null!;

    /// <summary>
    /// Aktuální fyzické množství kusů na regále ve skladu. Nesmí klesnout pod 0 (Zákaz výdeje do záporu).
    /// </summary>
    public int QuantityOnHand { get; private set; }

    /// <summary>
    /// Množství zarezervované v přijatých otevřených objednávkách zákazníků.
    /// </summary>
    public int ReservedQuantity { get; private set; }

    /// <summary>
    /// Disponibilní (volná) zásoba, kterou lze okamžitě přislíbit novým objednávkám.
    /// Disponibilní = Fyzická zásoba - Rezervace.
    /// </summary>
    public int AvailableQuantity => Math.Max(0, QuantityOnHand - ReservedQuantity);

    /// <summary>
    /// Minimální doporučená bezpečnostní hladina zásob pro úvodní dashboard a varování před vyprodáním.
    /// </summary>
    public int MinStockLevel { get; private set; } = 5;

    /// <summary>
    /// Příznak, zda je stav zásob na či pod minimální doporučenou hladinou.
    /// </summary>
    public bool IsLowStock => QuantityOnHand <= MinStockLevel;

    /// <summary>
    /// Datum založení skladové položky.
    /// </summary>
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    /// <summary>
    /// Datum poslední změny stavu zásob.
    /// </summary>
    public DateTime? UpdatedAt { get; private set; }

    private readonly List<StockMovement> _movements = [];
    public IReadOnlyCollection<StockMovement> Movements => _movements.AsReadOnly();

    // Pro Entity Framework Core
    protected StockItem()
    {
    }

    public StockItem(int productId, int warehouseId, int initialQuantity = 0, int minStockLevel = 5)
    {
        if (productId <= 0) throw new DomainException("Neplatné ID produktu.");
        if (warehouseId <= 0) throw new DomainException("Neplatné ID skladu.");
        if (initialQuantity < 0) throw new DomainException("Počáteční zásoba nesmí být záporná.");
        if (minStockLevel < 0) throw new DomainException("Minimální hladina zásob nesmí být záporná.");

        ProductId = productId;
        WarehouseId = warehouseId;
        QuantityOnHand = initialQuantity;
        ReservedQuantity = 0;
        MinStockLevel = minStockLevel;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Fyzický příjem zboží na sklad (naskladnění z dílny nebo převod).
    /// </summary>
    public void ReceiveStock(int quantity)
    {
        if (quantity <= 0)
        {
            throw new DomainException("Přijímané množství musí být větší než 0.");
        }

        QuantityOnHand += quantity;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Fyzický výdej zboží ze skladu (přímý výdej nebo meziskladový přesun).
    /// Striktně vynucuje ZÁKAZ VÝDEJE DO ZÁPORU.
    /// </summary>
    public void IssueStock(int quantity)
    {
        if (quantity <= 0)
        {
            throw new DomainException("Vydávané množství musí být větší než 0.");
        }

        if (quantity > QuantityOnHand)
        {
            throw new DomainException(
                $"Zákaz výdeje do záporu! Nelze vydat {quantity} ks. " +
                $"Na skladě je fyzicky k dispozici pouze {QuantityOnHand} ks.");
        }

        QuantityOnHand -= quantity;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Okamžitá rezervace zboží při přijetí zákaznické objednávky.
    /// </summary>
    public void ReserveStock(int quantity)
    {
        if (quantity <= 0)
        {
            throw new DomainException("Rezervované množství musí být větší než 0.");
        }

        if (quantity > AvailableQuantity)
        {
            throw new DomainException(
                $"Objednávku nelze pokrýt! Požadováno {quantity} ks, " +
                $"avšak volná disponibilní zásoba činí pouze {AvailableQuantity} ks (fyzicky {QuantityOnHand} ks, rezervováno {ReservedQuantity} ks).");
        }

        ReservedQuantity += quantity;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Uvolnění rezervace (např. při stornování či zrušení objednávky).
    /// </summary>
    public void ReleaseReservation(int quantity)
    {
        if (quantity <= 0)
        {
            throw new DomainException("Uvolňované množství musí být větší než 0.");
        }

        ReservedQuantity = Math.Max(0, ReservedQuantity - quantity);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Fyzická expedice dříve zarezervovaného zboží zákazníkovi.
    /// Odpíše fyzickou zásobu a zároveň zruší příslušnou rezervaci.
    /// </summary>
    public void ShipReserved(int quantity)
    {
        if (quantity <= 0)
        {
            throw new DomainException("Expedované množství musí být větší než 0.");
        }

        if (quantity > ReservedQuantity)
        {
            throw new DomainException($"Nelze expedovat více než zarezervované množství ({ReservedQuantity} ks).");
        }

        if (quantity > QuantityOnHand)
        {
            throw new DomainException($"Kritická chyba: Na skladě fyzicky chybí kusy pro expedici (fyzicky {QuantityOnHand} ks).");
        }

        ReservedQuantity -= quantity;
        QuantityOnHand -= quantity;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Nastavení minimální bezpečné hladiny zásob.
    /// </summary>
    public void SetMinStockLevel(int level)
    {
        if (level < 0)
        {
            throw new DomainException("Minimální hladina zásob nesmí být záporná.");
        }

        MinStockLevel = level;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Inventurní úprava stavu při zjištěné neshodě v garáži či skladu.
    /// </summary>
    public void AdjustStock(int actualCount)
    {
        if (actualCount < 0)
        {
            throw new DomainException("Výsledný fyzický stav po inventuře nesmí být záporný.");
        }

        QuantityOnHand = actualCount;
        UpdatedAt = DateTime.UtcNow;
    }
}

