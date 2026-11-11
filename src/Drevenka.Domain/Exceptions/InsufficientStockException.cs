namespace Drevenka.Domain.Exceptions;

/// <summary>
/// Výjimka vyvolaná při pokusu o objednání či výdej zboží, na které není dostatek disponibilních zásob.
/// Klient požaduje odmítnutí objednávky při nedostatku skladových zásob.
/// </summary>
public class InsufficientStockException : DomainException
{
    public string ProductName { get; }
    public int RequestedQuantity { get; }
    public int AvailableQuantity { get; }

    public InsufficientStockException(string productName, int requestedQuantity, int availableQuantity)
        : base($"Nedostatek zásob pro produkt '{productName}'. Požadováno: {requestedQuantity} ks, k dispozici pouze: {availableQuantity} ks.")
    {
        ProductName = productName;
        RequestedQuantity = requestedQuantity;
        AvailableQuantity = availableQuantity;
    }
}

