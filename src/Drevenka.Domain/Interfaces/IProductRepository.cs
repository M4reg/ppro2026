using Drevenka.Domain.Entities;

namespace Drevenka.Domain.Interfaces;

/// <summary>
/// Repozitářové rozhraní pro správu produktů.
/// </summary>
public interface IProductRepository
{
    Task<Product?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Product?> GetByCodeAsync(string code, CancellationToken ct = default);
    Task<IReadOnlyList<Product>> GetAllAsync(string? searchTerm = null, bool onlyActive = false, CancellationToken ct = default);
    Task<bool> ExistsCodeAsync(string code, int? excludeId = null, CancellationToken ct = default);
    Task AddAsync(Product product, CancellationToken ct = default);
    Task UpdateAsync(Product product, CancellationToken ct = default);
    Task DeleteAsync(Product product, CancellationToken ct = default);
}
