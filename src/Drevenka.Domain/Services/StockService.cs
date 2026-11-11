using Drevenka.Domain.Entities;
using Drevenka.Domain.Exceptions;
using Drevenka.Domain.Interfaces;

namespace Drevenka.Domain.Services;

public interface IStockService
{
    Task<StockMovement> ReceiveStockAsync(int productId, int warehouseId, int quantity, string? note, string performedBy, CancellationToken ct = default);
    Task<StockMovement> IssueStockAsync(int productId, int warehouseId, int quantity, string? note, string performedBy, string? referenceNumber = null, CancellationToken ct = default);
    Task<(StockMovement OutMovement, StockMovement InMovement)> TransferStockAsync(int productId, int sourceWarehouseId, int targetWarehouseId, int quantity, string? note, string performedBy, CancellationToken ct = default);
    Task<StockMovement> AdjustStockAsync(int productId, int warehouseId, int actualCount, string reason, string performedBy, CancellationToken ct = default);
    Task<IReadOnlyList<StockItem>> GetStockOverviewAsync(string? search = null, int? warehouseId = null, bool lowStockOnly = false, CancellationToken ct = default);
    Task<IReadOnlyList<StockMovement>> GetMovementsHistoryAsync(int? productId = null, int? warehouseId = null, int limit = 100, CancellationToken ct = default);
}

public class StockService : IStockService
{
    private readonly IStockRepository _stockRepository;
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly IProductRepository _productRepository;

    public StockService(
        IStockRepository stockRepository,
        IWarehouseRepository warehouseRepository,
        IProductRepository productRepository)
    {
        _stockRepository = stockRepository;
        _warehouseRepository = warehouseRepository;
        _productRepository = productRepository;
    }

    public async Task<StockMovement> ReceiveStockAsync(int productId, int warehouseId, int quantity, string? note, string performedBy, CancellationToken ct = default)
    {
        if (quantity <= 0)
        {
            throw new DomainException("Přijímané množství musí být větší než 0.");
        }

        var product = await _productRepository.GetByIdAsync(productId, ct)
            ?? throw new DomainException($"Produkt s ID {productId} nebyl nalezen.");

        var warehouse = await _warehouseRepository.GetByIdAsync(warehouseId, ct)
            ?? throw new DomainException($"Sklad s ID {warehouseId} nebyl nalezen.");

        var stockItem = await _stockRepository.GetOrCreateStockItemAsync(productId, warehouseId, ct);
        var previousQty = stockItem.QuantityOnHand;

        stockItem.ReceiveStock(quantity);
        var newQty = stockItem.QuantityOnHand;

        var movement = new StockMovement(
            stockItemId: stockItem.Id,
            productId: productId,
            warehouseId: warehouseId,
            movementType: StockMovementType.Receipt,
            quantity: quantity,
            previousQuantity: previousQty,
            newQuantity: newQty,
            note: note,
            referenceNumber: $"PRIJEM-{DateTime.UtcNow:yyyyMMdd-HHmm}",
            performedBy: performedBy
        );

        await _stockRepository.AddMovementAsync(movement, ct);
        await _stockRepository.SaveChangesAsync(ct);

        return movement;
    }

    public async Task<StockMovement> IssueStockAsync(int productId, int warehouseId, int quantity, string? note, string performedBy, string? referenceNumber = null, CancellationToken ct = default)
    {
        if (quantity <= 0)
        {
            throw new DomainException("Vydávané množství musí být větší než 0.");
        }

        var product = await _productRepository.GetByIdAsync(productId, ct)
            ?? throw new DomainException($"Produkt s ID {productId} nebyl nalezen.");

        var warehouse = await _warehouseRepository.GetByIdAsync(warehouseId, ct)
            ?? throw new DomainException($"Sklad s ID {warehouseId} nebyl nalezen.");

        var stockItem = await _stockRepository.GetStockItemAsync(productId, warehouseId, ct)
            ?? throw new DomainException($"Položka pro produkt '{product.Name}' na skladu '{warehouse.Name}' nebyla nalezena.");

        var previousQty = stockItem.QuantityOnHand;
        stockItem.IssueStock(quantity); // Zde se vyvolá kontrola ZÁKAZU VÝDEJE DO ZÁPORU
        var newQty = stockItem.QuantityOnHand;

        var movement = new StockMovement(
            stockItemId: stockItem.Id,
            productId: productId,
            warehouseId: warehouseId,
            movementType: StockMovementType.Issue,
            quantity: quantity,
            previousQuantity: previousQty,
            newQuantity: newQty,
            note: note,
            referenceNumber: referenceNumber ?? $"VYDEJ-{DateTime.UtcNow:yyyyMMdd-HHmm}",
            performedBy: performedBy
        );

        await _stockRepository.AddMovementAsync(movement, ct);
        await _stockRepository.SaveChangesAsync(ct);

        return movement;
    }

    public async Task<(StockMovement OutMovement, StockMovement InMovement)> TransferStockAsync(
        int productId,
        int sourceWarehouseId,
        int targetWarehouseId,
        int quantity,
        string? note,
        string performedBy,
        CancellationToken ct = default)
    {
        if (sourceWarehouseId == targetWarehouseId)
        {
            throw new DomainException("Zdrojový a cílový sklad pro přesun musí být různé.");
        }

        if (quantity <= 0)
        {
            throw new DomainException("Množství pro přesun musí být větší než 0.");
        }

        var sourceWh = await _warehouseRepository.GetByIdAsync(sourceWarehouseId, ct)
            ?? throw new DomainException($"Zdrojový sklad s ID {sourceWarehouseId} nebyl nalezen.");

        var targetWh = await _warehouseRepository.GetByIdAsync(targetWarehouseId, ct)
            ?? throw new DomainException($"Cílový sklad s ID {targetWarehouseId} nebyl nalezen.");

        var product = await _productRepository.GetByIdAsync(productId, ct)
            ?? throw new DomainException($"Produkt s ID {productId} nebyl nalezen.");

        var sourceStock = await _stockRepository.GetStockItemAsync(productId, sourceWarehouseId, ct)
            ?? throw new DomainException($"Zboží '{product.Name}' není na zdrojovém skladu '{sourceWh.Name}' evidováno.");

        var targetStock = await _stockRepository.GetOrCreateStockItemAsync(productId, targetWarehouseId, ct);

        var refCode = $"PREV-{DateTime.UtcNow:yyyyMMdd-HHmm}";

        // 1. Výdej ze zdrojového skladu (ověří zákaz záporu)
        var srcPrev = sourceStock.QuantityOnHand;
        sourceStock.IssueStock(quantity);
        var srcNew = sourceStock.QuantityOnHand;

        var outMovement = new StockMovement(
            stockItemId: sourceStock.Id,
            productId: productId,
            warehouseId: sourceWarehouseId,
            movementType: StockMovementType.TransferOut,
            quantity: quantity,
            previousQuantity: srcPrev,
            newQuantity: srcNew,
            note: $"Přesun na sklad {targetWh.Name}. {(string.IsNullOrWhiteSpace(note) ? "" : "Poznámka: " + note)}".Trim(),
            referenceNumber: refCode,
            performedBy: performedBy
        );

        // 2. Příjem na cílový sklad
        var tgtPrev = targetStock.QuantityOnHand;
        targetStock.ReceiveStock(quantity);
        var tgtNew = targetStock.QuantityOnHand;

        var inMovement = new StockMovement(
            stockItemId: targetStock.Id,
            productId: productId,
            warehouseId: targetWarehouseId,
            movementType: StockMovementType.TransferIn,
            quantity: quantity,
            previousQuantity: tgtPrev,
            newQuantity: tgtNew,
            note: $"Příjem přesunem ze skladu {sourceWh.Name}. {(string.IsNullOrWhiteSpace(note) ? "" : "Poznámka: " + note)}".Trim(),
            referenceNumber: refCode,
            performedBy: performedBy
        );

        await _stockRepository.AddMovementAsync(outMovement, ct);
        await _stockRepository.AddMovementAsync(inMovement, ct);
        await _stockRepository.SaveChangesAsync(ct);

        return (outMovement, inMovement);
    }

    public async Task<StockMovement> AdjustStockAsync(
        int productId,
        int warehouseId,
        int actualCount,
        string reason,
        string performedBy,
        CancellationToken ct = default)
    {
        if (actualCount < 0)
        {
            throw new DomainException("Výsledný stav po inventuře nesmí být záporný.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainException("Při inventurním vyrovnání manka či přebytku je povinné uvést textové odůvodnění.");
        }

        var product = await _productRepository.GetByIdAsync(productId, ct)
            ?? throw new DomainException($"Produkt s ID {productId} nebyl nalezen.");

        var warehouse = await _warehouseRepository.GetByIdAsync(warehouseId, ct)
            ?? throw new DomainException($"Sklad s ID {warehouseId} nebyl nalezen.");

        var stockItem = await _stockRepository.GetOrCreateStockItemAsync(productId, warehouseId, ct);
        var previousQty = stockItem.QuantityOnHand;
        var diff = actualCount - previousQty;

        if (diff == 0)
        {
            throw new DomainException("Zadaný počet kusů odpovídá stávajícímu stavu skladu. Nebyl zaznamenán žádný inventurní rozdíl.");
        }

        stockItem.AdjustStock(actualCount);
        var newQty = stockItem.QuantityOnHand;

        var movement = new StockMovement(
            stockItemId: stockItem.Id,
            productId: productId,
            warehouseId: warehouseId,
            movementType: StockMovementType.InventoryDiscrepancy,
            quantity: Math.Abs(diff),
            previousQuantity: previousQty,
            newQuantity: newQty,
            note: $"Inventura (rozdíl: {(diff > 0 ? "+" : "")}{diff} ks): {reason.Trim()}",
            referenceNumber: $"INV-{DateTime.UtcNow:yyyyMMdd-HHmm}",
            performedBy: performedBy
        );

        await _stockRepository.AddMovementAsync(movement, ct);
        await _stockRepository.SaveChangesAsync(ct);

        return movement;
    }

    public async Task<IReadOnlyList<StockItem>> GetStockOverviewAsync(string? search = null, int? warehouseId = null, bool lowStockOnly = false, CancellationToken ct = default)
    {
        return await _stockRepository.GetAllStockItemsAsync(search, warehouseId, lowStockOnly, ct);
    }

    public async Task<IReadOnlyList<StockMovement>> GetMovementsHistoryAsync(int? productId = null, int? warehouseId = null, int limit = 100, CancellationToken ct = default)
    {
        return await _stockRepository.GetMovementsAsync(productId, warehouseId, limit, ct);
    }
}

