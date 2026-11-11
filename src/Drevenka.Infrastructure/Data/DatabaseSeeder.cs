using Drevenka.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Drevenka.Infrastructure.Data;

public static class DatabaseSeeder
{
    public static async Task InitializeDatabaseAsync(DrevenkaDbContext dbContext, ILogger logger, CancellationToken ct = default)
    {
        try
        {
            logger.LogInformation("Kontrola a aplikace databázových migrací PostgreSQL...");
            await dbContext.Database.MigrateAsync(ct);
            logger.LogInformation("Databázové migrace úspěšně aplikovány.");

            // 1. Seedování kategorií
            if (!await dbContext.Categories.AnyAsync(ct))
            {
                logger.LogInformation("Vkládání výchozích kategorií...");

                var sampleCategories = new List<Category>
                {
                    new("Dřevěné hračky", "Tradiční a bezpečné hračky z masivního dřeva pro všechny věkové kategorie."),
                    new("Pro nejmenší", "Hračky speciálně tvarované a testované pro batolata a předškolní děti."),
                    new("Dárky do 300 Kč", "Cenově dostupné a vkusné drobné dárky ze dřeva."),
                    new("Dopravní prostředky", "Autíčka, vláčky, traktory a další dřevěná vozidla na kolečkách."),
                    new("Stavebnice a hry", "Dřevěné kostky, pexesa a hlavolamy rozvíjející logiku a zručnost.")
                };

                await dbContext.Categories.AddRangeAsync(sampleCategories, ct);
                await dbContext.SaveChangesAsync(ct);
                logger.LogInformation("Výchozí kategorie úspěšně vloženy ({Count} kategorií).", sampleCategories.Count);
            }

            var allCategories = await dbContext.Categories.ToDictionaryAsync(c => c.Name, c => c, ct);

            // 2. Seedování produktů
            if (!await dbContext.Products.AnyAsync(ct))
            {
                logger.LogInformation("Vkládání výchozích syntetických produktů pro Dřevěnka s.r.o....");

                var pKaca = new Product(
                    "KAC-001",
                    "Dřevěná káča klasická",
                    "Tradiční barevná soustružená káča z bukového dřeva. Rozvíjí jemnou motoriku.",
                    45.00m,
                    120.00m
                );

                var pVlacek = new Product(
                    "VLK-001",
                    "Dřevěný vláček se třemi vagónky",
                    "Bukový vláček s magnetickými spřáhly a odnímatelným nákladem. Délka 35 cm.",
                    185.00m,
                    490.00m
                );

                var pKostky = new Product(
                    "KOS-050",
                    "Kostky z bukového dřeva (50 ks)",
                    "Sada přírodních i barevných dřevěných kostek různých tvarů v praktickém kyblíku.",
                    120.00m,
                    350.00m
                );

                var pKonik = new Product(
                    "KON-001",
                    "Dřevěný houpací koník",
                    "Robustní houpací koník z masivní borovice s bezpečnostním madlem pro nejmenší.",
                    650.00m,
                    1590.00m
                );

                var pPexeso = new Product(
                    "PEX-001",
                    "Dřevěné pexeso se zvířátky",
                    "32 dřevěných kartiček s gravírovanými lesními zvířátky v dřevěné krabičce.",
                    75.00m,
                    220.00m
                );

                var sampleProducts = new List<Product> { pKaca, pVlacek, pKostky, pKonik, pPexeso };
                await dbContext.Products.AddRangeAsync(sampleProducts, ct);
                await dbContext.SaveChangesAsync(ct);

                // Propojení M:N do kategorií
                if (allCategories.TryGetValue("Dřevěné hračky", out var katHracky))
                {
                    pKaca.AddCategory(katHracky.Id);
                    pVlacek.AddCategory(katHracky.Id);
                    pKostky.AddCategory(katHracky.Id);
                    pKonik.AddCategory(katHracky.Id);
                    pPexeso.AddCategory(katHracky.Id);
                }

                if (allCategories.TryGetValue("Pro nejmenší", out var katNejmensi))
                {
                    pKaca.AddCategory(katNejmensi.Id);
                    pKostky.AddCategory(katNejmensi.Id);
                    pKonik.AddCategory(katNejmensi.Id);
                }

                if (allCategories.TryGetValue("Dárky do 300 Kč", out var katDarky))
                {
                    pKaca.AddCategory(katDarky.Id);
                    pPexeso.AddCategory(katDarky.Id);
                }

                if (allCategories.TryGetValue("Dopravní prostředky", out var katDoprava))
                {
                    pVlacek.AddCategory(katDoprava.Id);
                }

                if (allCategories.TryGetValue("Stavebnice a hry", out var katHry))
                {
                    pKostky.AddCategory(katHry.Id);
                    pPexeso.AddCategory(katHry.Id);
                }

                await dbContext.SaveChangesAsync(ct);
                logger.LogInformation("Výchozí syntetické produkty úspěšně vloženy a zařazeny do kategorií.");
            }
            else
            {
                // Pokud již produkty existují, ověříme, zda mají přiřazené kategorie M:N
                if (!await dbContext.ProductCategories.AnyAsync(ct))
                {
                    logger.LogInformation("Doplnění vazeb M:N ke stávajícím produktům...");
                    var products = await dbContext.Products.ToListAsync(ct);

                    foreach (var p in products)
                    {
                        if (allCategories.TryGetValue("Dřevěné hračky", out var katHracky))
                        {
                            p.AddCategory(katHracky.Id);
                        }

                        if (p.Code == "KAC-001")
                        {
                            if (allCategories.TryGetValue("Pro nejmenší", out var kn)) p.AddCategory(kn.Id);
                            if (allCategories.TryGetValue("Dárky do 300 Kč", out var kd)) p.AddCategory(kd.Id);
                        }
                        else if (p.Code == "VLK-001")
                        {
                            if (allCategories.TryGetValue("Dopravní prostředky", out var kdop)) p.AddCategory(kdop.Id);
                        }
                        else if (p.Code == "KOS-050")
                        {
                            if (allCategories.TryGetValue("Pro nejmenší", out var kn)) p.AddCategory(kn.Id);
                            if (allCategories.TryGetValue("Stavebnice a hry", out var kh)) p.AddCategory(kh.Id);
                        }
                        else if (p.Code == "KON-001")
                        {
                            if (allCategories.TryGetValue("Pro nejmenší", out var kn)) p.AddCategory(kn.Id);
                        }
                        else if (p.Code == "PEX-001")
                        {
                            if (allCategories.TryGetValue("Dárky do 300 Kč", out var kd)) p.AddCategory(kd.Id);
                            if (allCategories.TryGetValue("Stavebnice a hry", out var kh)) p.AddCategory(kh.Id);
                        }
                    }

                    await dbContext.SaveChangesAsync(ct);
                    logger.LogInformation("Vazby M:N úspěšně doplněny.");
                }
            }

            // 3. Seedování skladů (Sklad HK - centrála, Garáž Třebechovice)
            if (!await dbContext.Warehouses.AnyAsync(ct))
            {
                logger.LogInformation("Vkládání výchozích skladů...");

                var warehouses = new List<Warehouse>
                {
                    new(
                        "HK-01",
                        "Hlavní sklad Hradec Králové (Centrála)",
                        "Pražská 120, 500 04 Hradec Králové – primární expedice e-shopu",
                        isMain: true
                    ),
                    new(
                        "TREB-01",
                        "Sklad Třebechovice pod Orebem (Garáž)",
                        "Heldova 45, 503 46 Třebechovice p. O. – sezónní sklad a přebytky",
                        isMain: false
                    )
                };

                await dbContext.Warehouses.AddRangeAsync(warehouses, ct);
                await dbContext.SaveChangesAsync(ct);
                logger.LogInformation("Výchozí sklady úspěšně vloženy (Hradec Králové + Třebechovice).");
            }

            // 4. Seedování skladových zásob a počátečních auditních pohybů
            if (!await dbContext.StockItems.AnyAsync(ct))
            {
                logger.LogInformation("Vkládání výchozích skladových zásob a auditních pohybů...");

                var whHradec = await dbContext.Warehouses.FirstAsync(w => w.Code == "HK-01", ct);
                var whTrebechovice = await dbContext.Warehouses.FirstAsync(w => w.Code == "TREB-01", ct);
                var products = await dbContext.Products.ToListAsync(ct);

                // Definice počátečních stavů: (Code, HradecQty, TrebQty, MinLevel)
                var stockSetup = new (string Code, int HradecQty, int TrebQty, int MinLevel)[]
                {
                    ("KAC-001", 25, 50, 10),
                    ("VLK-001", 12, 8, 5),
                    ("KOS-050", 30, 40, 10),
                    ("KON-001", 3, 2, 5),  // Pod minimem (3 <= 5) -> ukázka low stock na dashboardu!
                    ("PEX-001", 45, 60, 15)
                };

                foreach (var item in stockSetup)
                {
                    var prod = products.FirstOrDefault(p => p.Code == item.Code);
                    if (prod == null) continue;

                    // Položka pro Hradec Králové
                    var stockHk = new StockItem(prod.Id, whHradec.Id, initialQuantity: item.HradecQty, minStockLevel: item.MinLevel);
                    await dbContext.StockItems.AddAsync(stockHk, ct);
                    await dbContext.SaveChangesAsync(ct);

                    var movementHk = new StockMovement(
                        stockItemId: stockHk.Id,
                        productId: prod.Id,
                        warehouseId: whHradec.Id,
                        movementType: StockMovementType.Receipt,
                        quantity: item.HradecQty,
                        previousQuantity: 0,
                        newQuantity: item.HradecQty,
                        note: "Počáteční naskladnění výroby při spuštění systému",
                        referenceNumber: "POCATEK-HK",
                        performedBy: "Petr Doležal"
                    );
                    await dbContext.StockMovements.AddAsync(movementHk, ct);

                    // Položka pro Třebechovice (Garáž)
                    var stockTreb = new StockItem(prod.Id, whTrebechovice.Id, initialQuantity: item.TrebQty, minStockLevel: item.MinLevel);
                    await dbContext.StockItems.AddAsync(stockTreb, ct);
                    await dbContext.SaveChangesAsync(ct);

                    var movementTreb = new StockMovement(
                        stockItemId: stockTreb.Id,
                        productId: prod.Id,
                        warehouseId: whTrebechovice.Id,
                        movementType: StockMovementType.Receipt,
                        quantity: item.TrebQty,
                        previousQuantity: 0,
                        newQuantity: item.TrebQty,
                        note: "Převedení původního zjištěného stavu z garáže",
                        referenceNumber: "POCATEK-TREB",
                        performedBy: "Petr Doležal"
                    );
                    await dbContext.StockMovements.AddAsync(movementTreb, ct);
                }

                await dbContext.SaveChangesAsync(ct);
                logger.LogInformation("Počáteční skladové zásoby a auditní logy úspěšně zavedeny.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Chyba při inicializaci nebo migraci databáze.");
            throw;
        }
    }
}
