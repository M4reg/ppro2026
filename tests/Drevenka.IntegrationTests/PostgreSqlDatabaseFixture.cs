using Drevenka.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;
using Xunit;

namespace Drevenka.IntegrationTests;

public class PostgreSqlDatabaseFixture : IAsyncLifetime
{
    private const string AdminConnectionString = "Host=localhost;Port=5432;Database=drevenka_db;Username=drevenka_user;Password=drevenka_pass";
    public const string TestDatabaseName = "drevenka_integration_test";
    public const string TestConnectionString = "Host=localhost;Port=5432;Database=drevenka_integration_test;Username=drevenka_user;Password=drevenka_pass";

    private DbContextOptions<DrevenkaDbContext>? _options;

    public DbContextOptions<DrevenkaDbContext> Options => _options
        ?? throw new InvalidOperationException("Fixture nebyla inicializována.");

    public DrevenkaDbContext CreateContext()
    {
        return new DrevenkaDbContext(Options);
    }

    public async Task InitializeAsync()
    {
        // 1. Ověříme / vytvoříme testovací databázi v PostgreSQL
        await EnsureTestDatabaseExistsAsync();

        // 2. Nastavíme DbContextOptions a aplikujeme všechny verzované migrace
        _options = new DbContextOptionsBuilder<DrevenkaDbContext>()
            .UseNpgsql(TestConnectionString)
            .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
            .Options;

        await using var context = CreateContext();
        await context.Database.MigrateAsync();

        // 3. Vyčistíme data před testy
        await ResetDataAsync();
    }

    public async Task DisposeAsync()
    {
        try
        {
            await ResetDataAsync();
        }
        catch
        {
            // Ponecháme schéma čisté
        }
    }

    public async Task ResetDataAsync()
    {
        try
        {
            await using var context = CreateContext();
            await context.Database.ExecuteSqlRawAsync(@"
                TRUNCATE TABLE 
                    order_items,
                    orders,
                    customers,
                    stock_movements,
                    stock_items,
                    warehouses,
                    product_categories,
                    categories,
                    products
                RESTART IDENTITY CASCADE;
            ");
        }
        catch (PostgresException ex) when (ex.SqlState == "42P01") // undefined_table
        {
            // Tabulky ještě nebyly vytvořeny
        }
    }

    private static async Task EnsureTestDatabaseExistsAsync()
    {
        await using var adminConn = new NpgsqlConnection(AdminConnectionString);
        await adminConn.OpenAsync();

        await using var checkCmd = new NpgsqlCommand(
            $"SELECT 1 FROM pg_database WHERE datname = '{TestDatabaseName}'", adminConn);
        var exists = await checkCmd.ExecuteScalarAsync();

        if (exists == null)
        {
            await using var createCmd = new NpgsqlCommand($"CREATE DATABASE {TestDatabaseName};", adminConn);
            await createCmd.ExecuteNonQueryAsync();
        }
    }
}

[CollectionDefinition("DatabaseCollection")]
public class DatabaseCollection : ICollectionFixture<PostgreSqlDatabaseFixture>
{
}
