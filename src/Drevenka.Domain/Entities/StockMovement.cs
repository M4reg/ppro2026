using Drevenka.Domain.Exceptions;

namespace Drevenka.Domain.Entities;

/// <summary>
/// Neměnný auditní log každého skladového pohybu (příjem, výdej, převoz, inventurní manko/přebytek).
/// Slouží pro zpětnou dohledatelnost: co, kde, kdy, kolik, kdo a na základě čeho vydal či přijal.
/// </summary>
public class StockMovement
{
    public int Id { get; internal set; }

    public int StockItemId { get; private set; }
    public StockItem StockItem { get; private set; } = null!;

    public int ProductId { get; private set; }
    public Product Product { get; private set; } = null!;

    public int WarehouseId { get; private set; }
    public Warehouse Warehouse { get; private set; } = null!;

    /// <summary>
    /// Typ pohybu (Příjem, Výdej, Převoz z/do, Inventura).
    /// </summary>
    public StockMovementType MovementType { get; private set; }

    /// <summary>
    /// Počet kusů, o které se zásoba změnila (vždy kladná hodnota).
    /// </summary>
    public int Quantity { get; private set; }

    /// <summary>
    /// Fyzický stav zásob na daném skladě bezprostředně před pohybem.
    /// </summary>
    public int PreviousQuantity { get; private set; }

    /// <summary>
    /// Fyzický stav zásob na daném skladě po realizaci pohybu.
    /// </summary>
    public int NewQuantity { get; private set; }

    /// <summary>
    /// Číslo souvisejícího dokladu (např. číslo objednávky, číslo převodky).
    /// </summary>
    public string? ReferenceNumber { get; private set; }

    /// <summary>
    /// Textové odůvodnění pohybu (povinné např. u inventurních mank či poškození).
    /// </summary>
    public string? Note { get; private set; }

    /// <summary>
    /// Jméno skladníka nebo uživatele, který pohyb fyzicky zaznamenal.
    /// </summary>
    public string PerformedBy { get; private set; } = "Skladník";

    /// <summary>
    /// Neměnný čas vzniku pohybu (UTC).
    /// </summary>
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    // Pro Entity Framework Core
    protected StockMovement()
    {
    }

    public StockMovement(
        int stockItemId,
        int productId,
        int warehouseId,
        StockMovementType movementType,
        int quantity,
        int previousQuantity,
        int newQuantity,
        string? note = null,
        string? referenceNumber = null,
        string performedBy = "Petr Doležal")
    {
        if (stockItemId <= 0) throw new DomainException("Neplatné ID skladové položky.");
        if (productId <= 0) throw new DomainException("Neplatné ID produktu.");
        if (warehouseId <= 0) throw new DomainException("Neplatné ID skladu.");
        if (quantity <= 0) throw new DomainException("Množství pohybu musí být větší než 0.");
        if (previousQuantity < 0 || newQuantity < 0) throw new DomainException("Stav skladu v pohybu nesmí být záporný.");
        if (movementType == StockMovementType.InventoryDiscrepancy && string.IsNullOrWhiteSpace(note))
        {
            throw new DomainException("U inventurního vyrovnání (manko/přebytek) je povinné zadat textové odůvodnění.");
        }

        StockItemId = stockItemId;
        ProductId = productId;
        WarehouseId = warehouseId;
        MovementType = movementType;
        Quantity = quantity;
        PreviousQuantity = previousQuantity;
        NewQuantity = newQuantity;
        Note = note?.Trim();
        ReferenceNumber = referenceNumber?.Trim();
        PerformedBy = string.IsNullOrWhiteSpace(performedBy) ? "Petr Doležal" : performedBy.Trim();
        CreatedAt = DateTime.UtcNow;
    }
}

