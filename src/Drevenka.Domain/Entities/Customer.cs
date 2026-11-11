using Drevenka.Domain.Exceptions;

namespace Drevenka.Domain.Entities;

/// <summary>
/// Doménová entita reprezentující zákazníka e-shopu Dřevěnka s.r.o.
/// Obchodní pravidlo: Zákazníci se z databáze nemažou (pouze deaktivují).
/// </summary>
public class Customer
{
    public int Id { get; internal set; }

    public string FullName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;
    public string Address { get; private set; } = string.Empty;
    public string City { get; private set; } = string.Empty;
    public string PostalCode { get; private set; } = string.Empty;
    public string? Note { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; private set; }

    private readonly List<Order> _orders = [];
    public IReadOnlyCollection<Order> Orders => _orders.AsReadOnly();

    // Pro Entity Framework Core
    protected Customer() { }

    public Customer(
        string fullName,
        string email,
        string phone,
        string address,
        string city,
        string postalCode,
        string? note = null)
    {
        UpdateDetails(fullName, email, phone, address, city, postalCode, note);
        CreatedAt = DateTime.UtcNow;
        IsActive = true;
    }

    public void UpdateDetails(
        string fullName,
        string email,
        string phone,
        string address,
        string city,
        string postalCode,
        string? note = null)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new DomainException("Jméno zákazníka nesmí být prázdné.");

        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            throw new DomainException("E-mail zákazníka musí být platný.");

        FullName = fullName.Trim();
        Email = email.Trim().ToLowerInvariant();
        Phone = phone?.Trim() ?? string.Empty;
        Address = address?.Trim() ?? string.Empty;
        City = city?.Trim() ?? string.Empty;
        PostalCode = postalCode?.Trim() ?? string.Empty;
        Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }
}

