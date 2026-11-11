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

            // 5. Seedování zákazníků
            if (!await dbContext.Customers.AnyAsync(ct))
            {
                logger.LogInformation("Vkládání výchozích syntetických zákazníků...");
                var sampleCustomers = new List<Customer>
                {
                    new(
                        fullName: "Jana Nováková",
                        email: "jana.novakova@priklad.cz",
                        phone: "+420 602 123 456",
                        address: "Pospíšilova 312",
                        city: "Hradec Králové",
                        postalCode: "500 03",
                        note: "Pravidelná zákaznice, dřevěné hračky pro mateřskou školu."
                    ),
                    new(
                        fullName: "Martin Dvořák",
                        email: "martin.dvorak@priklad.cz",
                        phone: "+420 777 987 654",
                        address: "Palackého náměstí 45",
                        city: "Třebechovice pod Orebem",
                        postalCode: "503 46",
                        note: "Osobní odběr u garáže po telefonické domluvě."
                    ),
                    new(
                        fullName: "Mateřská škola Sluníčko",
                        email: "ms.slunicko@priklad.cz",
                        phone: "+420 495 555 111",
                        address: "Školská 89",
                        city: "Pardubice",
                        postalCode: "530 02",
                        note: "Odběr větších sad kostek a pexes pro předškoláky."
                    )
                };

                await dbContext.Customers.AddRangeAsync(sampleCustomers, ct);
                await dbContext.SaveChangesAsync(ct);
                logger.LogInformation("Výchozí zákazníci úspěšně vloženi ({Count} zákazníků).", sampleCustomers.Count);
            }

            // 6. Seedování ukázkových objednávek
            if (!await dbContext.Orders.AnyAsync(ct))
            {
                logger.LogInformation("Vkládání ukázkových objednávek a demonstrace rezervací...");

                var custJana = await dbContext.Customers.FirstAsync(c => c.Email == "jana.novakova@priklad.cz", ct);
                var custMartin = await dbContext.Customers.FirstAsync(c => c.Email == "martin.dvorak@priklad.cz", ct);
                var prodKaca = await dbContext.Products.FirstAsync(p => p.Code == "KAC-001", ct);
                var prodVlacek = await dbContext.Products.FirstAsync(p => p.Code == "VLA-002", ct);
                var prodKostky = await dbContext.Products.FirstAsync(p => p.Code == "KOS-003", ct);
                var whMain = await dbContext.Warehouses.FirstAsync(w => w.IsMain, ct);

                // Objednávka 1: Potvrzená s rezervací
                var order1 = new Order(
                    orderNumber: "OBJ-2026-0001",
                    customerId: custJana.Id,
                    deliveryAddress: custJana.Address,
                    deliveryCity: custJana.City,
                    deliveryPostalCode: custJana.PostalCode,
                    customerNote: "Prosím o doručení do pátku.");
                order1.AddItem(prodKaca.Id, 2, prodKaca.SellingPrice);
                order1.AddItem(prodVlacek.Id, 1, prodVlacek.SellingPrice);
                order1.Confirm();

                // Rezervace na hlavním skladě
                var stockKaca = await dbContext.StockItems.FirstAsync(s => s.ProductId == prodKaca.Id && s.WarehouseId == whMain.Id, ct);
                stockKaca.ReserveStock(2);

                var stockVlacek = await dbContext.StockItems.FirstAsync(s => s.ProductId == prodVlacek.Id && s.WarehouseId == whMain.Id, ct);
                stockVlacek.ReserveStock(1);

                await dbContext.Orders.AddAsync(order1, ct);

                // Objednávka 2: Již expedovaná (fyzicky vyskladněno)
                var order2 = new Order(
                    orderNumber: "OBJ-2026-0002",
                    customerId: custMartin.Id,
                    deliveryAddress: custMartin.Address,
                    deliveryCity: custMartin.City,
                    deliveryPostalCode: custMartin.PostalCode,
                    customerNote: "Vyzvednuto osobně.");
                order2.AddItem(prodKostky.Id, 1, prodKostky.SellingPrice);
                order2.Confirm();
                order2.Dispatch();

                // Provedeme fyzický výdej 1 ks kostek a auditní pohyb
                var stockKostky = await dbContext.StockItems.FirstAsync(s => s.ProductId == prodKostky.Id && s.WarehouseId == whMain.Id, ct);
                var prevQty = stockKostky.QuantityOnHand;
                stockKostky.IssueStock(1);
                var newQty = stockKostky.QuantityOnHand;

                var movExpedice = new StockMovement(
                    stockItemId: stockKostky.Id,
                    productId: prodKostky.Id,
                    warehouseId: whMain.Id,
                    movementType: StockMovementType.Issue,
                    quantity: 1,
                    previousQuantity: prevQty,
                    newQuantity: newQty,
                    note: "Fyzická expedice zákaznické objednávky č. OBJ-2026-0002 pro Martin Dvořák",
                    referenceNumber: "OBJ-2026-0002",
                    performedBy: "Petr Doležal");

                await dbContext.Orders.AddAsync(order2, ct);
                await dbContext.StockMovements.AddAsync(movExpedice, ct);

                await dbContext.SaveChangesAsync(ct);
                logger.LogInformation("Ukázkové objednávky úspěšně založeny.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Chyba při inicializaci nebo migraci databáze.");
            throw;
        }
    }
}
