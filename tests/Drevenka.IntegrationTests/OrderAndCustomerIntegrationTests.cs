using Drevenka.Domain.Entities;
using Drevenka.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Drevenka.IntegrationTests;

[Collection("DatabaseCollection")]
public class OrderAndCustomerIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlDatabaseFixture _fixture;

    public OrderAndCustomerIntegrationTests(PostgreSqlDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync() => _fixture.ResetDataAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task PersistOrder_WithCustomerAndItems_ShouldSaveAndLoadCorrectly_FromPostgreSql()
    {
        // Arrange
        await using (var seedContext = _fixture.CreateContext())
        {
            var catHracky = new Category("Dřevěné hračky", "Tradiční hračky");
            var catDarky = new Category("Dárky do 300 Kč", "Drobné dárky");
            seedContext.Categories.AddRange(catHracky, catDarky);

            var p1 = new Product("HRA-001", "Dřevěná káča", "Barevná káča", 60m, 120m);
            var p2 = new Product("HRA-002", "Dřevěný vláček", "Lokomotiva", 220m, 390m);
            seedContext.Products.AddRange(p1, p2);
            await seedContext.SaveChangesAsync();

            // Vazba M:N přes entitu ProductCategory
            seedContext.ProductCategories.AddRange(
                new ProductCategory(p1.Id, catHracky.Id),
                new ProductCategory(p1.Id, catDarky.Id),
                new ProductCategory(p2.Id, catHracky.Id));

            var customer = new Customer(
                fullName: "Marie Svobodová",
                email: "marie.svobodova@example.cz",
                phone: "+420608999888",
                address: "Velké náměstí 45",
                city: "Hradec Králové",
                postalCode: "500 01");
            seedContext.Customers.Add(customer);

            var warehouse = new Warehouse("HK-MAIN", "Hlavní sklad Hradec Králové", isMain: true);
            seedContext.Warehouses.Add(warehouse);

            await seedContext.SaveChangesAsync();

            // Vytvoření objednávky
            var order = new Order(
                orderNumber: "OBJ-2026-0001",
                customerId: customer.Id,
                deliveryAddress: customer.Address,
                deliveryCity: customer.City,
                deliveryPostalCode: customer.PostalCode,
                customerNote: "Zabalit s mašlí");

            order.AddItem(p1.Id, quantity: 2, unitPrice: 120m); // 240 Kč
            order.AddItem(p2.Id, quantity: 1, unitPrice: 390m); // 390 Kč
            order.Confirm();

            seedContext.Orders.Add(order);
            await seedContext.SaveChangesAsync();
        }

        // Act & Assert v novém izolovaném DbContextu (načtení přímo z databáze)
        await using (var queryContext = _fixture.CreateContext())
        {
            var loadedOrder = await queryContext.Orders
                .Include(o => o.Customer)
                .Include(o => o.Items)
                    .ThenInclude(i => i.Product)
                        .ThenInclude(p => p.ProductCategories)
                            .ThenInclude(pc => pc.Category)
                .FirstOrDefaultAsync(o => o.OrderNumber == "OBJ-2026-0001");

            loadedOrder.Should().NotBeNull();
            loadedOrder!.Customer.FullName.Should().Be("Marie Svobodová");
            loadedOrder.Status.Should().Be(OrderStatus.Confirmed);
            loadedOrder.TotalItemsCount.Should().Be(3);
            loadedOrder.TotalAmount.Should().Be(630m); // 240 + 390
            loadedOrder.Items.Should().HaveCount(2);

            // Ověření M:N relace u prvního produktu v objednávce
            var kacaItem = loadedOrder.Items.First(i => i.Quantity == 2);
            kacaItem.UnitPrice.Should().Be(120m);
            kacaItem.Product.ProductCategories.Should().HaveCount(2);
            kacaItem.Product.ProductCategories.Select(pc => pc.Category.Name)
                .Should().Contain(["Dřevěné hračky", "Dárky do 300 Kč"]);
        }
    }
}
