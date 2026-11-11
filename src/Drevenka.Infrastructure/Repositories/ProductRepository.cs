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
        return await _dbContext.Products
            .Include(p => p.ProductCategories)
            .ThenInclude(pc => pc.Category)
            .Include(p => p.StockItems)
            .ThenInclude(si => si.Warehouse)
            .FirstOrDefaultAsync(p => p.Id == id, ct);
    }

    public async Task<Product?> GetByCodeAsync(string code, CancellationToken ct = default)
    {
        var normalized = code.Trim().ToUpperInvariant();
        return await _dbContext.Products
            .Include(p => p.ProductCategories)
            .ThenInclude(pc => pc.Category)
            .Include(p => p.StockItems)
            .ThenInclude(si => si.Warehouse)
            .FirstOrDefaultAsync(p => p.Code == normalized, ct);
    }

    public async Task<IReadOnlyList<Product>> GetAllAsync(string? searchTerm = null, int? categoryId = null, bool onlyActive = false, CancellationToken ct = default)
    {
        IQueryable<Product> query = _dbContext.Products
            .Include(p => p.ProductCategories)
            .ThenInclude(pc => pc.Category)
            .Include(p => p.StockItems)
            .ThenInclude(si => si.Warehouse)
            .AsNoTracking();

        if (onlyActive)
        {
            query = query.Where(p => p.IsActive);
        }

        if (categoryId.HasValue && categoryId.Value > 0)
        {
            query = query.Where(p => p.ProductCategories.Any(pc => pc.CategoryId == categoryId.Value));
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var tokens = searchTerm.Split(new[] { ',', ';', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                   .Select(t => t.ToLower())
                                   .Where(t => !string.IsNullOrWhiteSpace(t))
                                   .Distinct()
                                   .ToArray();

            if (tokens.Length > 1)
            {
                var param = System.Linq.Expressions.Expression.Parameter(typeof(Product), "p");
                var toLower = typeof(string).GetMethod(nameof(string.ToLower), Type.EmptyTypes)!;
                var contains = typeof(string).GetMethod(nameof(string.Contains), new[] { typeof(string) })!;

                var codeProp = System.Linq.Expressions.Expression.Call(System.Linq.Expressions.Expression.Property(param, nameof(Product.Code)), toLower);
                var nameProp = System.Linq.Expressions.Expression.Call(System.Linq.Expressions.Expression.Property(param, nameof(Product.Name)), toLower);

                System.Linq.Expressions.Expression? combined = null;
                foreach (var token in tokens)
                {
                    var tokenConst = System.Linq.Expressions.Expression.Constant(token);
                    var codeContains = System.Linq.Expressions.Expression.Call(codeProp, contains, tokenConst);
                    var nameContains = System.Linq.Expressions.Expression.Call(nameProp, contains, tokenConst);
                    var tokenMatch = System.Linq.Expressions.Expression.OrElse(codeContains, nameContains);

                    combined = combined == null ? tokenMatch : System.Linq.Expressions.Expression.OrElse(combined, tokenMatch);
                }

                if (combined != null)
                {
                    var lambda = System.Linq.Expressions.Expression.Lambda<Func<Product, bool>>(combined, param);
                    query = query.Where(lambda);
                }
            }
            else
            {
                var term = searchTerm.Trim().ToLower();
                query = query.Where(p => p.Name.ToLower().Contains(term) || p.Code.ToLower().Contains(term));
            }
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
