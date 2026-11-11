using Drevenka.Domain.Entities;
using Drevenka.Domain.Interfaces;
using Drevenka.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Drevenka.Infrastructure.Repositories;

public class StockRepository : IStockRepository
{
    private readonly DrevenkaDbContext _dbContext;

    public StockRepository(DrevenkaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<StockItem?> GetStockItemAsync(int productId, int warehouseId, CancellationToken ct = default)
    {
        return await _dbContext.StockItems
            .Include(si => si.Product)
            .Include(si => si.Warehouse)
            .FirstOrDefaultAsync(si => si.ProductId == productId && si.WarehouseId == warehouseId, ct);
    }

    public async Task<IReadOnlyList<StockItem>> GetStockItemsByProductAsync(int productId, CancellationToken ct = default)
    {
        return await _dbContext.StockItems
            .Include(si => si.Warehouse)
            .Where(si => si.ProductId == productId)
            .OrderBy(si => si.Warehouse.Name)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<StockItem>> GetStockItemsByWarehouseAsync(int warehouseId, CancellationToken ct = default)
    {
        return await _dbContext.StockItems
            .Include(si => si.Product)
            .Where(si => si.WarehouseId == warehouseId)
            .OrderBy(si => si.Product.Name)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<StockItem>> GetAllStockItemsAsync(
        string? searchTerm = null,
        int? warehouseId = null,
        bool lowStockOnly = false,
        CancellationToken ct = default)
    {
        IQueryable<StockItem> query = _dbContext.StockItems
            .Include(si => si.Product)
            .Include(si => si.Warehouse)
            .AsNoTracking();

        if (warehouseId.HasValue && warehouseId.Value > 0)
        {
            query = query.Where(si => si.WarehouseId == warehouseId.Value);
        }

        if (lowStockOnly)
        {
            query = query.Where(si => si.QuantityOnHand <= si.MinStockLevel);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(si => si.Product.Name.ToLower().Contains(term) || si.Product.Code.ToLower().Contains(term));
        }

        return await query
            .OrderBy(si => si.Product.Name)
            .ThenBy(si => si.Warehouse.Name)
            .ToListAsync(ct);
    }

    public async Task<StockItem> GetOrCreateStockItemAsync(int productId, int warehouseId, CancellationToken ct = default)
    {
        var existing = await _dbContext.StockItems
            .Include(si => si.Product)
            .Include(si => si.Warehouse)
            .FirstOrDefaultAsync(si => si.ProductId == productId && si.WarehouseId == warehouseId, ct);

        if (existing != null)
        {
            return existing;
        }

        var newItem = new StockItem(productId, warehouseId, initialQuantity: 0);
        await _dbContext.StockItems.AddAsync(newItem, ct);
        await _dbContext.SaveChangesAsync(ct);
        return newItem;
    }

    public async Task AddMovementAsync(StockMovement movement, CancellationToken ct = default)
    {
        await _dbContext.StockMovements.AddAsync(movement, ct);
    }

    public async Task<IReadOnlyList<StockMovement>> GetMovementsAsync(
        int? productId = null,
        int? warehouseId = null,
        int limit = 100,
        CancellationToken ct = default)
    {
        IQueryable<StockMovement> query = _dbContext.StockMovements
            .Include(sm => sm.Product)
            .Include(sm => sm.Warehouse)
            .AsNoTracking();

        if (productId.HasValue && productId.Value > 0)
        {
            query = query.Where(sm => sm.ProductId == productId.Value);
        }

        if (warehouseId.HasValue && warehouseId.Value > 0)
        {
            query = query.Where(sm => sm.WarehouseId == warehouseId.Value);
        }

        return await query
            .OrderByDescending(sm => sm.CreatedAt)
            .Take(limit)
            .ToListAsync(ct);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _dbContext.SaveChangesAsync(ct);
    }
}

