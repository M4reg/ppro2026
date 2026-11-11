using Drevenka.Domain.Entities;
using Drevenka.Domain.Interfaces;
using Drevenka.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Drevenka.Infrastructure.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly DrevenkaDbContext _dbContext;

    public CategoryRepository(DrevenkaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Category?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _dbContext.Categories
            .Include(c => c.ProductCategories)
            .FirstOrDefaultAsync(c => c.Id == id, ct);
    }

    public async Task<Category?> GetByNameAsync(string name, CancellationToken ct = default)
    {
        var normalized = name.Trim().ToLower();
        return await _dbContext.Categories
            .FirstOrDefaultAsync(c => c.Name.ToLower() == normalized, ct);
    }

    public async Task<IReadOnlyList<Category>> GetAllAsync(bool onlyActive = false, CancellationToken ct = default)
    {
        IQueryable<Category> query = _dbContext.Categories.AsNoTracking();

        if (onlyActive)
        {
            query = query.Where(c => c.IsActive);
        }

        return await query.OrderBy(c => c.Name).ToListAsync(ct);
    }

    public async Task<bool> ExistsNameAsync(string name, int? excludeId = null, CancellationToken ct = default)
    {
        var normalized = name.Trim().ToLower();
        return await _dbContext.Categories
            .AnyAsync(c => c.Name.ToLower() == normalized && (!excludeId.HasValue || c.Id != excludeId.Value), ct);
    }

    public async Task AddAsync(Category category, CancellationToken ct = default)
    {
        await _dbContext.Categories.AddAsync(category, ct);
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Category category, CancellationToken ct = default)
    {
        _dbContext.Categories.Update(category);
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Category category, CancellationToken ct = default)
    {
        _dbContext.Categories.Remove(category);
        await _dbContext.SaveChangesAsync(ct);
    }
}

