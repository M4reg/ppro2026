namespace Drevenka.Domain.Entities;

/// <summary>
/// Stavy životního cyklu zákaznické objednávky.
/// </summary>
public enum OrderStatus
{
    /// <summary>
    /// Nová přijatá objednávka (čeká na ověření a rezervaci).
    /// </summary>
    New = 0,

    /// <summary>
    /// Potvrzená objednávka (zboží je zarezervováno ve skladu).
    /// </summary>
    Confirmed = 1,

    /// <summary>
    /// Expedovaná objednávka (zboží fyzicky vyskladněno a odesláno zákazníkovi).
    /// </summary>
    Dispatched = 2,

    /// <summary>
    /// Zrušená/stornovaná objednávka (případná rezervace uvolněna zpět k dispozici).
    /// </summary>
    Cancelled = 3
}

