namespace Drevenka.Domain.Entities;

/// <summary>
/// Typ skladového pohybu v auditním logu.
/// </summary>
public enum StockMovementType
{
    /// <summary>
    /// Příjem zboží z výroby / naskladnění od dodavatele.
    /// </summary>
    Receipt = 1,

    /// <summary>
    /// Výdej zboží zákazníkovi / fyzická expedice objednávky.
    /// </summary>
    Issue = 2,

    /// <summary>
    /// Výdej v rámci přesunu na jiný sklad (např. Třebechovice -> Hradec).
    /// </summary>
    TransferOut = 3,

    /// <summary>
    /// Příjem v rámci přesunu z jiného skladu (naskladnění v cílovém skladu).
    /// </summary>
    TransferIn = 4,

    /// <summary>
    /// Inventurní vyrovnání (zjištěné manko nebo přebytek s povinným odůvodněním).
    /// </summary>
    InventoryDiscrepancy = 5
}

