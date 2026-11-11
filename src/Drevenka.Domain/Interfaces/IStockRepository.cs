using Drevenka.Domain.Entities;

namespace Drevenka.Domain.Interfaces;

public interface IStockRepository
{
    Task<StockItem?> GetStockItemAsync(int productId, int warehouseId, CancellationToken ct = default);
    Task<IReadOnlyList<StockItem>> GetStockItemsByProductAsync(int productId, CancellationToken ct = default);
    Task<IReadOnlyList<StockItem>> GetStockItemsByWarehouseAsync(int warehouseId, CancellationToken ct = default);
    Task<IReadOnlyList<StockItem>> GetAllStockItemsAsync(string? searchTerm = null, int? warehouseId = null, bool lowStockOnly = false, CancellationToken ct = default);
    Task<StockItem> GetOrCreateStockItemAsync(int productId, int warehouseId, CancellationToken ct = default);
    Task AddMovementAsync(StockMovement movement, CancellationToken ct = default);
    Task<IReadOnlyList<StockMovement>> GetMovementsAsync(int? productId = null, int? warehouseId = null, int limit = 100, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}

