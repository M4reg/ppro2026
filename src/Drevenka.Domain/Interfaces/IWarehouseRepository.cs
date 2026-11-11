using Drevenka.Domain.Entities;

namespace Drevenka.Domain.Interfaces;

public interface IWarehouseRepository
{
    Task<Warehouse?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Warehouse?> GetByCodeAsync(string code, CancellationToken ct = default);
    Task<IReadOnlyList<Warehouse>> GetAllAsync(bool onlyActive = false, CancellationToken ct = default);
    Task<Warehouse?> GetMainWarehouseAsync(CancellationToken ct = default);
    Task AddAsync(Warehouse warehouse, CancellationToken ct = default);
    Task UpdateAsync(Warehouse warehouse, CancellationToken ct = default);
}

