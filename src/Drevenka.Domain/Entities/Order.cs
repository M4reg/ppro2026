using Drevenka.Domain.Exceptions;

namespace Drevenka.Domain.Entities;

/// <summary>
/// Doménová entita reprezentující zákaznickou objednávku v e-shopu Dřevěnka s.r.o.
/// </summary>
public class Order
{
    public int Id { get; internal set; }

    /// <summary>
    /// Jedinečné číslo objednávky (např. "OBJ-2026-0001").
    /// </summary>
    public string OrderNumber { get; private set; } = string.Empty;

    public int CustomerId { get; private set; }
    public Customer Customer { get; private set; } = null!;

    public DateTime OrderDate { get; private set; } = DateTime.UtcNow;
    public OrderStatus Status { get; private set; } = OrderStatus.New;

    public string DeliveryAddress { get; private set; } = string.Empty;
    public string DeliveryCity { get; private set; } = string.Empty;
    public string DeliveryPostalCode { get; private set; } = string.Empty;

    public string? CustomerNote { get; private set; }
    public string? InternalNote { get; private set; }

    public DateTime? ConfirmedAt { get; private set; }
    public DateTime? DispatchedAt { get; private set; }
    public DateTime? CancelledAt { get; private set; }
    public string? CancellationReason { get; private set; }

    private readonly List<OrderItem> _items = [];
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    /// <summary>
    /// Celková částka za objednávku v Kč.
    /// </summary>
    public decimal TotalAmount => _items.Sum(i => i.TotalPrice);

    /// <summary>
    /// Celkový počet objednaných kusů.
    /// </summary>
    public int TotalItemsCount => _items.Sum(i => i.Quantity);

    // Pro Entity Framework Core
    protected Order() { }

    public Order(
        string orderNumber,
        int customerId,
        string deliveryAddress,
        string deliveryCity,
        string deliveryPostalCode,
        string? customerNote = null)
    {
        if (string.IsNullOrWhiteSpace(orderNumber))
            throw new DomainException("Číslo objednávky nesmí být prázdné.");

        if (customerId <= 0)
            throw new DomainException("Neplatný zákazník objednávky.");

        if (string.IsNullOrWhiteSpace(deliveryAddress) || string.IsNullOrWhiteSpace(deliveryCity))
            throw new DomainException("Dodací adresa a město musí být vyplněny.");

        OrderNumber = orderNumber.Trim().ToUpperInvariant();
        CustomerId = customerId;
        DeliveryAddress = deliveryAddress.Trim();
        DeliveryCity = deliveryCity.Trim();
        DeliveryPostalCode = deliveryPostalCode?.Trim() ?? string.Empty;
        CustomerNote = string.IsNullOrWhiteSpace(customerNote) ? null : customerNote.Trim();
        OrderDate = DateTime.UtcNow;
        Status = OrderStatus.New;
    }

    public void AddItem(int productId, int quantity, decimal unitPrice)
    {
        if (Status == OrderStatus.Dispatched)
            throw new DomainException("Do již expedované objednávky nelze přidávat položky.");

        if (Status == OrderStatus.Cancelled)
            throw new DomainException("Do stornované objednávky nelze přidávat položky.");

        var existingItem = _items.FirstOrDefault(i => i.ProductId == productId);
        if (existingItem != null)
        {
            throw new DomainException($"Produkt s ID {productId} již v objednávce existuje. Upravte množství stávající položky.");
        }

        _items.Add(new OrderItem(productId, quantity, unitPrice));
    }

    public void Confirm()
    {
        if (Status != OrderStatus.New)
            throw new DomainException($"Objednávku ve stavu '{Status}' nelze potvrdit. Lze potvrdit pouze novou objednávku.");

        if (_items.Count == 0)
            throw new DomainException("Objednávka musí obsahovat alespoň jednu položku před potvrzením.");

        Status = OrderStatus.Confirmed;
        ConfirmedAt = DateTime.UtcNow;
    }

    public void Dispatch()
    {
        if (Status != OrderStatus.Confirmed)
            throw new DomainException($"Objednávku ve stavu '{Status}' nelze expedovat. Expedovat lze pouze potvrzenou objednávku.");

        Status = OrderStatus.Dispatched;
        DispatchedAt = DateTime.UtcNow;
    }

    public void Cancel(string reason)
    {
        if (Status == OrderStatus.Dispatched)
            throw new DomainException("Již expedovanou objednávku nelze stornovat. Je nutné provést proces vratky/reklamace.");

        if (Status == OrderStatus.Cancelled)
            throw new DomainException("Objednávka je již stornována.");

        Status = OrderStatus.Cancelled;
        CancelledAt = DateTime.UtcNow;
        CancellationReason = string.IsNullOrWhiteSpace(reason) ? "Stornováno operátorem" : reason.Trim();
    }
}

