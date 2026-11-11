using Drevenka.Domain.Entities;
using Drevenka.Domain.Interfaces;
using Drevenka.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Drevenka.Infrastructure.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly DrevenkaDbContext _context;

    public OrderRepository(DrevenkaDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Order>> GetAllAsync(OrderStatus? status = null, string? searchTerm = null, CancellationToken ct = default)
    {
        var query = _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(o => o.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(o =>
                o.OrderNumber.ToLower().Contains(term) ||
                o.Customer.FullName.ToLower().Contains(term) ||
                o.Customer.Email.ToLower().Contains(term) ||
                o.DeliveryCity.ToLower().Contains(term));
        }

        return await query
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync(ct);
    }

    public async Task<Order?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == id, ct);
    }

    public async Task<Order?> GetByOrderNumberAsync(string orderNumber, CancellationToken ct = default)
    {
        var normalizedNumber = orderNumber.Trim().ToUpper();
        return await _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.OrderNumber == normalizedNumber, ct);
    }

    public async Task<IReadOnlyList<Order>> GetByCustomerIdAsync(int customerId, CancellationToken ct = default)
    {
        return await _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync(ct);
    }

    public async Task AddAsync(Order order, CancellationToken ct = default)
    {
        await _context.Orders.AddAsync(order, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Order order, CancellationToken ct = default)
    {
        _context.Orders.Update(order);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<string> GenerateNextOrderNumberAsync(CancellationToken ct = default)
    {
        var currentYear = DateTime.UtcNow.Year;
        var prefix = $"OBJ-{currentYear}-";

        var countThisYear = await _context.Orders
            .Where(o => o.OrderNumber.StartsWith(prefix))
            .CountAsync(ct);

        var nextNumber = countThisYear + 1;
        return $"{prefix}{nextNumber:D4}";
    }
}

