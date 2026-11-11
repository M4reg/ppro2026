using Drevenka.Domain.Entities;

namespace Drevenka.Domain.Interfaces;

/// <summary>
/// Repozitářové rozhraní pro správu kategorií.
/// </summary>
public interface ICategoryRepository
{
    Task<Category?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Category?> GetByNameAsync(string name, CancellationToken ct = default);
    Task<IReadOnlyList<Category>> GetAllAsync(bool onlyActive = false, CancellationToken ct = default);
    Task<bool> ExistsNameAsync(string name, int? excludeId = null, CancellationToken ct = default);
    Task AddAsync(Category category, CancellationToken ct = default);
    Task UpdateAsync(Category category, CancellationToken ct = default);
    Task DeleteAsync(Category category, CancellationToken ct = default);
}

