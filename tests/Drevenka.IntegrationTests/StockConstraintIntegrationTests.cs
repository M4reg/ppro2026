using Drevenka.Domain.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace Drevenka.IntegrationTests;

[Collection("DatabaseCollection")]
public class StockConstraintIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlDatabaseFixture _fixture;

    public StockConstraintIntegrationTests(PostgreSqlDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync() => _fixture.ResetDataAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task PostgreSql_CheckConstraint_ShouldPreventNegativeStock_ZakazVydejeDoZaporu()
    {
        int stockItemId;

        await using (var setupContext = _fixture.CreateContext())
        {
            var product = new Product("HRA-001", "Dřevěná káča", "Test", 50m, 100m);
            setupContext.Products.Add(product);

            var warehouse = new Warehouse("HK-MAIN", "Hlavní sklad Hradec Králové", isMain: true);
            setupContext.Warehouses.Add(warehouse);

            await setupContext.SaveChangesAsync();

            var stockItem = new StockItem(product.Id, warehouse.Id, initialQuantity: 5);
            setupContext.StockItems.Add(stockItem);
            await setupContext.SaveChangesAsync();

            stockItemId = stockItem.Id;
        }

        // Pokusíme se přímo v databázi nastavit záporný stav zásob (např. -3 ks)
        await using (var testContext = _fixture.CreateContext())
        {
            var act = async () =>
            {
                await testContext.Database.ExecuteSqlAsync(
                    $"UPDATE stock_items SET \"QuantityOnHand\" = -3 WHERE \"Id\" = {stockItemId}");
            };

            // PostgreSQL musí transakci odmítnout chybou integritního omezení (23514 check_violation)
            var ex = await act.Should().ThrowAsync<PostgresException>();
            ex.Which.SqlState.Should().Be("23514"); // PostgreSQL CHECK_VIOLATION
            ex.Which.ConstraintName.Should().Be("ck_stock_items_quantity_on_hand");
        }
    }

    [Fact]
    public async Task StockMovement_AuditTrail_ShouldPersistFullHistory_WithOrderReference()
    {
        await using var context = _fixture.CreateContext();

        var product = new Product("HRA-002", "Dřevěný koník", "Houpací koník", 300m, 590m);
        context.Products.Add(product);

        var warehouse = new Warehouse("HK-MAIN", "Hlavní sklad Hradec Králové", isMain: true);
        context.Warehouses.Add(warehouse);

        await context.SaveChangesAsync();

        var stockItem = new StockItem(product.Id, warehouse.Id, initialQuantity: 0);
        context.StockItems.Add(stockItem);
        await context.SaveChangesAsync();

        // 1. Pohyb: Příjem 20 ks
        stockItem.ReceiveStock(20);
        var mov1 = new StockMovement(
            stockItemId: stockItem.Id,
            productId: product.Id,
            warehouseId: warehouse.Id,
            movementType: StockMovementType.Receipt,
            quantity: 20,
            previousQuantity: 0,
            newQuantity: 20,
            note: "Příjem z truhlárny",
            performedBy: "Petr Doležal");
        context.StockMovements.Add(mov1);
        await context.SaveChangesAsync();

        // 2. Pohyb: Výdej 4 ks na objednávku OBJ-2026-0005
        stockItem.ReserveStock(4);
        stockItem.ShipReserved(4);
        var mov2 = new StockMovement(
            stockItemId: stockItem.Id,
            productId: product.Id,
            warehouseId: warehouse.Id,
            movementType: StockMovementType.Issue,
            quantity: 4,
            previousQuantity: 20,
            newQuantity: 16,
            note: "Expedice zákazníkovi",
            referenceNumber: "OBJ-2026-0005",
            performedBy: "Skladník Pavel");
        context.StockMovements.Add(mov2);
        await context.SaveChangesAsync();

        // Ověření v novém kontextu (auditní stopa)
        await using var verifyContext = _fixture.CreateContext();
        var movements = await verifyContext.StockMovements
            .Where(m => m.ProductId == product.Id)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();

        movements.Should().HaveCount(2);

        movements[0].MovementType.Should().Be(StockMovementType.Receipt);
        movements[0].Quantity.Should().Be(20);
        movements[0].PerformedBy.Should().Be("Petr Doležal");

        movements[1].MovementType.Should().Be(StockMovementType.Issue);
        movements[1].Quantity.Should().Be(4);
        movements[1].ReferenceNumber.Should().Be("OBJ-2026-0005");
        movements[1].PerformedBy.Should().Be("Skladník Pavel");
        movements[1].NewQuantity.Should().Be(16);
    }
}
