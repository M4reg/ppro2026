using Drevenka.Domain.Entities;
using Drevenka.Domain.Interfaces;
using Drevenka.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Drevenka.Infrastructure.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly DrevenkaDbContext _dbContext;

    public ProductRepository(DrevenkaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Product?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _dbContext.Products.FirstOrDefaultAsync(p => p.Id == id, ct);
    }

    public async Task<Product?> GetByCodeAsync(string code, CancellationToken ct = default)
    {
        var normalized = code.Trim().ToUpperInvariant();
        return await _dbContext.Products.FirstOrDefaultAsync(p => p.Code == normalized, ct);
    }

    public async Task<IReadOnlyList<Product>> GetAllAsync(string? searchTerm = null, bool onlyActive = false, CancellationToken ct = default)
    {
        IQueryable<Product> query = _dbContext.Products.AsNoTracking();

        if (onlyActive)
        {
            query = query.Where(p => p.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(term) || p.Code.ToLower().Contains(term));
        }

        return await query.OrderBy(p => p.Name).ToListAsync(ct);
    }

    public async Task<bool> ExistsCodeAsync(string code, int? excludeId = null, CancellationToken ct = default)
    {
        var normalized = code.Trim().ToUpperInvariant();
        return await _dbContext.Products.AnyAsync(p => p.Code == normalized && (!excludeId.HasValue || p.Id != excludeId.Value), ct);
    }

    public async Task AddAsync(Product product, CancellationToken ct = default)
    {
        await _dbContext.Products.AddAsync(product, ct);
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Product product, CancellationToken ct = default)
    {
        _dbContext.Products.Update(product);
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Product product, CancellationToken ct = default)
    {
        _dbContext.Products.Remove(product);
        await _dbContext.SaveChangesAsync(ct);
    }
}
