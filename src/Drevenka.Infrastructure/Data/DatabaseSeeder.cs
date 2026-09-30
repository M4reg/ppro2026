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

            if (!await dbContext.Products.AnyAsync(ct))
            {
                logger.LogInformation("Vkládání výchozích syntetických produktů pro Dřevěnka s.r.o....");

                var sampleProducts = new List<Product>
                {
                    new(
                        "KAC-001",
                        "Dřevěná káča klasická",
                        "Tradiční barevná soustružená káča z bukového dřeva. Rozvíjí jemnou motoriku.",
                        45.00m,
                        120.00m
                    ),
                    new(
                        "VLK-001",
                        "Dřevěný vláček se třemi vagónky",
                        "Bukový vláček s magnetickými spřáhly a odnímatelným nákladem. Délka 35 cm.",
                        185.00m,
                        490.00m
                    ),
                    new(
                        "KOS-050",
                        "Kostky z bukového dřeva (50 ks)",
                        "Sada přírodních i barevných dřevěných kostek různých tvarů v praktickém kyblíku.",
                        120.00m,
                        350.00m
                    ),
                    new(
                        "KON-001",
                        "Dřevěný houpací koník",
                        "Robustní houpací koník z masivní borovice s bezpečnostním madlem pro nejmenší.",
                        650.00m,
                        1590.00m
                    ),
                    new(
                        "PEX-001",
                        "Dřevěné pexeso se zvířátky",
                        "32 dřevěných kartiček s gravírovanými lesními zvířátky v dřevěné krabičce.",
                        75.00m,
                        220.00m
                    )
                };

                await dbContext.Products.AddRangeAsync(sampleProducts, ct);
                await dbContext.SaveChangesAsync(ct);
                logger.LogInformation("Výchozí syntetické produkty úspěšně vloženy ({Count} položek).", sampleProducts.Count);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Chyba při inicializaci nebo migraci databáze.");
            throw;
        }
    }
}
