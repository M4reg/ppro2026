using Drevenka.Domain.Entities;
using Drevenka.Domain.Interfaces;
using Drevenka.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Drevenka.Infrastructure.Repositories;

public class CustomerRepository : ICustomerRepository
{
    private readonly DrevenkaDbContext _context;

    public CustomerRepository(DrevenkaDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Customer>> GetAllAsync(string? searchTerm = null, bool onlyActive = true, CancellationToken ct = default)
    {
        var query = _context.Customers.AsQueryable();

        if (onlyActive)
        {
            query = query.Where(c => c.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(c =>
                c.FullName.ToLower().Contains(term) ||
                c.Email.ToLower().Contains(term) ||
                c.Phone.ToLower().Contains(term) ||
                c.City.ToLower().Contains(term));
        }

        return await query
            .Include(c => c.Orders)
            .OrderBy(c => c.FullName)
            .ToListAsync(ct);
    }

    public async Task<Customer?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _context.Customers
            .Include(c => c.Orders)
            .FirstOrDefaultAsync(c => c.Id == id, ct);
    }

    public async Task<Customer?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        var normalizedEmail = email.Trim().ToLower();
        return await _context.Customers
            .Include(c => c.Orders)
            .FirstOrDefaultAsync(c => c.Email.ToLower() == normalizedEmail, ct);
    }

    public async Task AddAsync(Customer customer, CancellationToken ct = default)
    {
        await _context.Customers.AddAsync(customer, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Customer customer, CancellationToken ct = default)
    {
        _context.Customers.Update(customer);
        await _context.SaveChangesAsync(ct);
    }
}

