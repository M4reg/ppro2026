using Drevenka.Domain.Entities;
using Drevenka.Domain.Exceptions;
using Drevenka.Domain.Interfaces;
using Drevenka.Domain.Services;
using FluentAssertions;
using Xunit;

namespace Drevenka.Domain.Tests.Services;

public class OrderServiceTests
{
    private class FakeOrderRepository : IOrderRepository
    {
        public readonly List<Order> Items = [];
        private int _counter = 1;

        public Task<IReadOnlyList<Order>> GetAllAsync(OrderStatus? status = null, string? searchTerm = null, CancellationToken ct = default)
        {
            IEnumerable<Order> q = Items;
            if (status.HasValue) q = q.Where(o => o.Status == status.Value);
            return Task.FromResult<IReadOnlyList<Order>>(q.ToList());
        }

        public Task<Order?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            return Task.FromResult(Items.FirstOrDefault(o => o.Id == id));
        }

        public Task<Order?> GetByOrderNumberAsync(string orderNumber, CancellationToken ct = default)
        {
            return Task.FromResult(Items.FirstOrDefault(o => o.OrderNumber == orderNumber));
        }

        public Task<IReadOnlyList<Order>> GetByCustomerIdAsync(int customerId, CancellationToken ct = default)
        {
            return Task.FromResult<IReadOnlyList<Order>>(Items.Where(o => o.CustomerId == customerId).ToList());
        }

        public Task AddAsync(Order order, CancellationToken ct = default)
        {
            order.Id = Items.Count + 1;
            Items.Add(order);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Order order, CancellationToken ct = default) => Task.CompletedTask;

        public Task<string> GenerateNextOrderNumberAsync(CancellationToken ct = default)
        {
            return Task.FromResult($"OBJ-2026-{_counter++:D4}");
        }
    }

    private class FakeCustomerRepository : ICustomerRepository
    {
        public readonly List<Customer> Items = [];

        public Task<IReadOnlyList<Customer>> GetAllAsync(string? searchTerm = null, bool onlyActive = true, CancellationToken ct = default)
        {
            IEnumerable<Customer> q = Items;
            if (onlyActive) q = q.Where(c => c.IsActive);
            return Task.FromResult<IReadOnlyList<Customer>>(q.ToList());
        }

        public Task<Customer?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            return Task.FromResult(Items.FirstOrDefault(c => c.Id == id));
        }

        public Task<Customer?> GetByEmailAsync(string email, CancellationToken ct = default)
        {
            return Task.FromResult(Items.FirstOrDefault(c => c.Email == email));
        }

        public Task AddAsync(Customer customer, CancellationToken ct = default)
        {
            customer.Id = Items.Count + 1;
            Items.Add(customer);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Customer customer, CancellationToken ct = default) => Task.CompletedTask;
    }

    private class FakeStockRepository : IStockRepository
    {
        public readonly List<StockItem> Items = [];
        public readonly List<StockMovement> Movements = [];

        public Task<StockItem?> GetStockItemAsync(int productId, int warehouseId, CancellationToken ct = default)
        {
            return Task.FromResult(Items.FirstOrDefault(s => s.ProductId == productId && s.WarehouseId == warehouseId));
        }

        public Task<IReadOnlyList<StockItem>> GetStockItemsByProductAsync(int productId, CancellationToken ct = default)
        {
            return Task.FromResult<IReadOnlyList<StockItem>>(Items.Where(s => s.ProductId == productId).ToList());
        }

        public Task<IReadOnlyList<StockItem>> GetStockItemsByWarehouseAsync(int warehouseId, CancellationToken ct = default)
        {
            return Task.FromResult<IReadOnlyList<StockItem>>(Items.Where(s => s.WarehouseId == warehouseId).ToList());
        }

        public Task<IReadOnlyList<StockItem>> GetAllStockItemsAsync(string? searchTerm = null, int? warehouseId = null, bool lowStockOnly = false, CancellationToken ct = default)
        {
            return Task.FromResult<IReadOnlyList<StockItem>>(Items);
        }

        public Task<StockItem> GetOrCreateStockItemAsync(int productId, int warehouseId, CancellationToken ct = default)
        {
            var existing = Items.FirstOrDefault(s => s.ProductId == productId && s.WarehouseId == warehouseId);
            if (existing != null) return Task.FromResult(existing);

            var created = new StockItem(productId, warehouseId, initialQuantity: 0);
            created.Id = Items.Count + 1;
            Items.Add(created);
            return Task.FromResult(created);
        }

        public Task AddMovementAsync(StockMovement movement, CancellationToken ct = default)
        {
            movement.Id = Movements.Count + 1;
            Movements.Add(movement);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<StockMovement>> GetMovementsAsync(int? productId = null, int? warehouseId = null, int limit = 100, CancellationToken ct = default)
        {
            return Task.FromResult<IReadOnlyList<StockMovement>>(Movements);
        }

        public Task SaveChangesAsync(CancellationToken ct = default) => Task.CompletedTask;
    }

    private class FakeWarehouseRepository : IWarehouseRepository
    {
        public readonly List<Warehouse> Items = [];

        public Task<Warehouse?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            return Task.FromResult(Items.FirstOrDefault(w => w.Id == id));
        }

        public Task<Warehouse?> GetByCodeAsync(string code, CancellationToken ct = default)
        {
            return Task.FromResult(Items.FirstOrDefault(w => w.Code == code));
        }

        public Task<IReadOnlyList<Warehouse>> GetAllAsync(bool onlyActive = false, CancellationToken ct = default)
        {
            return Task.FromResult<IReadOnlyList<Warehouse>>(Items);
        }

        public Task<Warehouse?> GetMainWarehouseAsync(CancellationToken ct = default)
        {
            return Task.FromResult(Items.FirstOrDefault(w => w.IsMain && w.IsActive));
        }

        public Task AddAsync(Warehouse warehouse, CancellationToken ct = default)
        {
            warehouse.Id = Items.Count + 1;
            Items.Add(warehouse);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Warehouse warehouse, CancellationToken ct = default) => Task.CompletedTask;
    }

    private class FakeProductRepository : IProductRepository
    {
        public readonly List<Product> Items = [];

        public Task<Product?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            return Task.FromResult(Items.FirstOrDefault(p => p.Id == id));
        }

        public Task<Product?> GetByCodeAsync(string code, CancellationToken ct = default)
        {
            return Task.FromResult(Items.FirstOrDefault(p => p.Code == code));
        }

        public Task<IReadOnlyList<Product>> GetAllAsync(string? searchTerm = null, int? categoryId = null, bool onlyActive = false, CancellationToken ct = default)
        {
            return Task.FromResult<IReadOnlyList<Product>>(Items);
        }

        public Task<bool> ExistsCodeAsync(string code, int? excludeId = null, CancellationToken ct = default)
        {
            return Task.FromResult(Items.Any(p => p.Code == code));
        }

        public Task AddAsync(Product product, CancellationToken ct = default)
        {
            product.Id = Items.Count + 1;
            Items.Add(product);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Product product, CancellationToken ct = default) => Task.CompletedTask;
        public Task DeleteAsync(Product product, CancellationToken ct = default) => Task.CompletedTask;
    }

    private (OrderService Service, FakeOrderRepository OrderRepo, FakeStockRepository StockRepo) CreateService()
    {
        var orderRepo = new FakeOrderRepository();
        var customerRepo = new FakeCustomerRepository();
        var productRepo = new FakeProductRepository();
        var warehouseRepo = new FakeWarehouseRepository();
        var stockRepo = new FakeStockRepository();

        var customer = new Customer("Jan Novák", "jan.novak@example.cz", "+420777123456", "Pospíšilova 312", "Hradec Králové", "500 03") { Id = 1 };
        customerRepo.Items.Add(customer);

        var warehouse = new Warehouse("HK-MAIN", "Hlavní sklad Hradec Králové", isMain: true) { Id = 1 };
        warehouseRepo.Items.Add(warehouse);

        var product = new Product("HRA-001", "Dřevěná káča", "Popis káči", 80m, 150m) { Id = 1 };
        productRepo.Items.Add(product);

        var stockItem = new StockItem(productId: 1, warehouseId: 1, initialQuantity: 10) { Id = 1 };
        stockRepo.Items.Add(stockItem);

        var service = new OrderService(orderRepo, customerRepo, productRepo, warehouseRepo, stockRepo);
        return (service, orderRepo, stockRepo);
    }

    [Fact]
    public async Task CreateOrderAsync_SufficientStock_ShouldConfirmOrder_AndReserveStock()
    {
        var (service, orderRepo, stockRepo) = CreateService();

        var dto = new CreateOrderDto(
            CustomerId: 1,
            DeliveryAddress: "Pospíšilova 312",
            DeliveryCity: "Hradec Králové",
            DeliveryPostalCode: "500 03",
            CustomerNote: "Prosím zabalit jako dárek",
            Items: [new CreateOrderItemDto(ProductId: 1, Quantity: 4)]);

        var order = await service.CreateOrderAsync(dto);

        order.Should().NotBeNull();
        order.Status.Should().Be(OrderStatus.Confirmed);
        order.TotalAmount.Should().Be(4 * 150m);
        order.Items.Should().HaveCount(1);
        order.Items.First().UnitPrice.Should().Be(150m);

        orderRepo.Items.Should().HaveCount(1);

        var stock = await stockRepo.GetStockItemAsync(1, 1);
        stock!.QuantityOnHand.Should().Be(10);
        stock.ReservedQuantity.Should().Be(4);
        stock.AvailableQuantity.Should().Be(6); // 10 - 4
    }

    [Fact]
    public async Task CreateOrderAsync_InsufficientStock_ShouldThrowInsufficientStockException_AndNotCreateOrder()
    {
        var (service, orderRepo, stockRepo) = CreateService();

        var dto = new CreateOrderDto(
            CustomerId: 1,
            DeliveryAddress: "Pospíšilova 312",
            DeliveryCity: "Hradec Králové",
            DeliveryPostalCode: "500 03",
            CustomerNote: null,
            Items: [new CreateOrderItemDto(ProductId: 1, Quantity: 15)]); // Na skladě je jen 10 ks

        var act = async () => await service.CreateOrderAsync(dto);

        await act.Should().ThrowAsync<InsufficientStockException>()
            .Where(e => e.ProductName == "Dřevěná káča" && e.RequestedQuantity == 15 && e.AvailableQuantity == 10);

        orderRepo.Items.Should().BeEmpty(); // Objednávka nesmí být uložena

        var stock = await stockRepo.GetStockItemAsync(1, 1);
        stock!.ReservedQuantity.Should().Be(0);
        stock.AvailableQuantity.Should().Be(10);
    }

    [Fact]
    public async Task DispatchOrderAsync_ConfirmedOrder_ShouldTransitionToDispatched_AndIssuePhysicalStock()
    {
        var (service, orderRepo, stockRepo) = CreateService();

        var dto = new CreateOrderDto(
            CustomerId: 1,
            DeliveryAddress: "Pospíšilova 312",
            DeliveryCity: "Hradec Králové",
            DeliveryPostalCode: "500 03",
            CustomerNote: null,
            Items: [new CreateOrderItemDto(ProductId: 1, Quantity: 3)]);

        var order = await service.CreateOrderAsync(dto);

        await service.DispatchOrderAsync(order.Id, performedBy: "Petr Doležal");

        order.Status.Should().Be(OrderStatus.Dispatched);
        order.DispatchedAt.Should().NotBeNull();

        var stock = await stockRepo.GetStockItemAsync(1, 1);
        stock!.QuantityOnHand.Should().Be(7); // 10 - 3 fyzicky vydáno
        stock.ReservedQuantity.Should().Be(0); // Rezervace odepsána
        stock.AvailableQuantity.Should().Be(7);

        // Auditní log výdeje
        stockRepo.Movements.Should().HaveCount(1);
        var mov = stockRepo.Movements[0];
        mov.MovementType.Should().Be(StockMovementType.Issue);
        mov.Quantity.Should().Be(3);
        mov.ReferenceNumber.Should().Be(order.OrderNumber);
        mov.PerformedBy.Should().Be("Petr Doležal");
    }

    [Fact]
    public async Task CancelOrderAsync_ConfirmedOrder_ShouldReleaseReservedStock()
    {
        var (service, orderRepo, stockRepo) = CreateService();

        var dto = new CreateOrderDto(
            CustomerId: 1,
            DeliveryAddress: "Pospíšilova 312",
            DeliveryCity: "Hradec Králové",
            DeliveryPostalCode: "500 03",
            CustomerNote: null,
            Items: [new CreateOrderItemDto(ProductId: 1, Quantity: 4)]);

        var order = await service.CreateOrderAsync(dto);

        await service.CancelOrderAsync(order.Id, reason: "Zákazník si to rozmyslel");

        order.Status.Should().Be(OrderStatus.Cancelled);

        var stock = await stockRepo.GetStockItemAsync(1, 1);
        stock!.QuantityOnHand.Should().Be(10);
        stock.ReservedQuantity.Should().Be(0); // Rezervace uvolněna
        stock.AvailableQuantity.Should().Be(10);
    }
}
