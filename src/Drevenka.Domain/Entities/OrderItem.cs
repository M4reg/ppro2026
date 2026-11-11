using Drevenka.Domain.Exceptions;

namespace Drevenka.Domain.Entities;

/// <summary>
/// Položka zákaznické objednávky.
/// Obchodní pravidlo: Neměnnost historických cen – UnitPrice je fixním snapshotem platným v okamžiku objednání.
/// </summary>
public class OrderItem
{
    public int Id { get; internal set; }

    public int OrderId { get; private set; }
    public Order Order { get; private set; } = null!;

    public int ProductId { get; private set; }
    public Product Product { get; private set; } = null!;

    /// <summary>
    /// Objednané množství kusů.
    /// </summary>
    public int Quantity { get; private set; }

    /// <summary>
    /// Jednotková prodejní cena v Kč platná v okamžiku objednání (neměnná historie).
    /// </summary>
    public decimal UnitPrice { get; private set; }

    /// <summary>
    /// Celková cena za položku v Kč (Množství * Jednotková cena).
    /// </summary>
    public decimal TotalPrice => Quantity * UnitPrice;

    // Pro Entity Framework Core
    protected OrderItem() { }

    public OrderItem(int productId, int quantity, decimal unitPrice)
    {
        if (productId <= 0)
            throw new DomainException("Neplatné ID produktu.");

        if (quantity <= 0)
            throw new DomainException("Množství položky objednávky musí být alespoň 1 ks.");

        if (unitPrice < 0)
            throw new DomainException("Jednotková cena nesmí být záporná.");

        ProductId = productId;
        Quantity = quantity;
        UnitPrice = decimal.Round(unitPrice, 2);
    }
}

