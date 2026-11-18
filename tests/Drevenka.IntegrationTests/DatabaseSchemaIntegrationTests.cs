using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Drevenka.IntegrationTests;

[Collection("DatabaseCollection")]
public class DatabaseSchemaIntegrationTests
{
    private readonly PostgreSqlDatabaseFixture _fixture;

    public DatabaseSchemaIntegrationTests(PostgreSqlDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Database_ShouldHaveAppliedAllMigrations_AndAllTablesExist()
    {
        await using var context = _fixture.CreateContext();

        var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
        pendingMigrations.Should().BeEmpty("všechny verzované migrace musí být aplikovány");

        var appliedMigrations = await context.Database.GetAppliedMigrationsAsync();
        appliedMigrations.Count().Should().BeGreaterThanOrEqualTo(4);
        appliedMigrations.Should().Contain(m => m.Contains("InitialCreate"));
        appliedMigrations.Should().Contain(m => m.Contains("AddCategoriesAndProductCategory"));
        appliedMigrations.Should().Contain(m => m.Contains("AddWarehousesAndStockMovements"));
        appliedMigrations.Should().Contain(m => m.Contains("AddCustomersAndOrders"));

        // Ověříme přítomnost všech 9 relačních tabulek
        var tables = new[]
        {
            "products",
            "categories",
            "product_categories",
            "warehouses",
            "stock_items",
            "stock_movements",
            "customers",
            "orders",
            "order_items"
        };

        foreach (var table in tables)
        {
            var count = await context.Database.SqlQueryRaw<int>(
                "SELECT COUNT(*)::int AS \"Value\" FROM information_schema.tables WHERE table_name = {0} AND table_schema = 'public'",
                table)
                .FirstAsync();

            count.Should().Be(1, $"tabulka '{table}' musí v PostgreSQL databázi existovat");
        }
    }
}
