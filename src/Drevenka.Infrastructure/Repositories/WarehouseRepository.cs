using Drevenka.Domain.Entities;
using Drevenka.Domain.Interfaces;
using Drevenka.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Drevenka.Infrastructure.Repositories;

public class WarehouseRepository : IWarehouseRepository
{
    private readonly DrevenkaDbContext _dbContext;

    public WarehouseRepository(DrevenkaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Warehouse?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _dbContext.Warehouses
            .Include(w => w.StockItems)
            .FirstOrDefaultAsync(w => w.Id == id, ct);
    }

    public async Task<Warehouse?> GetByCodeAsync(string code, CancellationToken ct = default)
    {
        var normalized = code.Trim().ToUpperInvariant();
        return await _dbContext.Warehouses
            .Include(w => w.StockItems)
            .FirstOrDefaultAsync(w => w.Code == normalized, ct);
    }

    public async Task<IReadOnlyList<Warehouse>> GetAllAsync(bool onlyActive = false, CancellationToken ct = default)
    {
        IQueryable<Warehouse> query = _dbContext.Warehouses.AsNoTracking();

        if (onlyActive)
        {
            query = query.Where(w => w.IsActive);
        }

        return await query.OrderByDescending(w => w.IsMain).ThenBy(w => w.Name).ToListAsync(ct);
    }

    public async Task<Warehouse?> GetMainWarehouseAsync(CancellationToken ct = default)
    {
        return await _dbContext.Warehouses
            .FirstOrDefaultAsync(w => w.IsMain && w.IsActive, ct);
    }

    public async Task AddAsync(Warehouse warehouse, CancellationToken ct = default)
    {
        await _dbContext.Warehouses.AddAsync(warehouse, ct);
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Warehouse warehouse, CancellationToken ct = default)
    {
        _dbContext.Warehouses.Update(warehouse);
        await _dbContext.SaveChangesAsync(ct);
    }
}

