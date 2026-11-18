using Drevenka.Domain.Entities;
using Drevenka.Domain.Exceptions;
using Drevenka.Domain.Interfaces;
using Drevenka.Domain.Services;
using FluentAssertions;
using Xunit;

namespace Drevenka.Domain.Tests.Services;

public class StockServiceTests
{
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
            IEnumerable<StockItem> q = Items;
            if (warehouseId.HasValue) q = q.Where(x => x.WarehouseId == warehouseId.Value);
            if (lowStockOnly) q = q.Where(x => x.IsLowStock);
            return Task.FromResult<IReadOnlyList<StockItem>>(q.ToList());
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
            IEnumerable<StockMovement> q = Movements;
            if (productId.HasValue) q = q.Where(x => x.ProductId == productId.Value);
            if (warehouseId.HasValue) q = q.Where(x => x.WarehouseId == warehouseId.Value);
            return Task.FromResult<IReadOnlyList<StockMovement>>(q.Take(limit).ToList());
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
            IEnumerable<Warehouse> q = Items;
            if (onlyActive) q = q.Where(w => w.IsActive);
            return Task.FromResult<IReadOnlyList<Warehouse>>(q.ToList());
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
            return Task.FromResult(Items.Any(p => p.Code == code && (!excludeId.HasValue || p.Id != excludeId.Value)));
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

    [Fact]
    public async Task ReceiveStockAsync_ValidData_ShouldIncreaseStock_AndRecordAuditMovement()
    {
        var stockRepo = new FakeStockRepository();
        var warehouseRepo = new FakeWarehouseRepository();
        var productRepo = new FakeProductRepository();

        var product = new Product("HRA-001", "Dřevěná káča", "Popis káči", 80m, 120m) { Id = 1 };
        productRepo.Items.Add(product);

        var warehouse = new Warehouse("HK-MAIN", "Hlavní sklad Hradec Králové", isMain: true) { Id = 1 };
        warehouseRepo.Items.Add(warehouse);

        var service = new StockService(stockRepo, warehouseRepo, productRepo);

        var movement = await service.ReceiveStockAsync(
            productId: 1,
            warehouseId: 1,
            quantity: 25,
            note: "Příjem z truhlárny",
            performedBy: "Petr Doležal");

        movement.MovementType.Should().Be(StockMovementType.Receipt);
        movement.Quantity.Should().Be(25);
        movement.PreviousQuantity.Should().Be(0);
        movement.NewQuantity.Should().Be(25);

        stockRepo.Movements.Should().HaveCount(1);
        var stockItem = await stockRepo.GetStockItemAsync(1, 1);
        stockItem!.QuantityOnHand.Should().Be(25);
    }

    [Fact]
    public async Task IssueStockAsync_ExceedingAvailableStock_ShouldThrowDomainException_ZakazVydejeDoZaporu()
    {
        var stockRepo = new FakeStockRepository();
        var warehouseRepo = new FakeWarehouseRepository();
        var productRepo = new FakeProductRepository();

        var product = new Product("HRA-001", "Dřevěná káča", "Popis káči", 80m, 120m) { Id = 1 };
        productRepo.Items.Add(product);

        var warehouse = new Warehouse("HK-MAIN", "Hlavní sklad Hradec Králové") { Id = 1 };
        warehouseRepo.Items.Add(warehouse);

        var stockItem = await stockRepo.GetOrCreateStockItemAsync(1, 1);
        stockItem.ReceiveStock(5); // Na skladě je 5 ks

        var service = new StockService(stockRepo, warehouseRepo, productRepo);

        var act = async () => await service.IssueStockAsync(
            productId: 1,
            warehouseId: 1,
            quantity: 10, // Pokus o výdej 10 ks -> Zákaz výdeje do záporu
            note: "Maloobchodní prodej",
            performedBy: "Petr Doležal");

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*Zákaz výdeje do záporu*");

        stockRepo.Movements.Should().BeEmpty(); // Pohyb nesmí vzniknout
        stockItem.QuantityOnHand.Should().Be(5); // Stav zůstává nedotčen
    }

    [Fact]
    public async Task TransferStockAsync_ValidTransfer_ShouldMoveBetweenWarehouses_AndCreateTwoMovements()
    {
        var stockRepo = new FakeStockRepository();
        var warehouseRepo = new FakeWarehouseRepository();
        var productRepo = new FakeProductRepository();

        var product = new Product("HRA-001", "Dřevěná káča", "Popis káči", 80m, 120m) { Id = 1 };
        productRepo.Items.Add(product);

        var wTrebechovice = new Warehouse("TREB-GAR", "Garáž Třebechovice") { Id = 1 };
        var wHradec = new Warehouse("HK-MAIN", "Hlavní sklad Hradec Králové", isMain: true) { Id = 2 };
        warehouseRepo.Items.AddRange([wTrebechovice, wHradec]);

        // V Třebechovicích je 30 ks
        var sourceStock = await stockRepo.GetOrCreateStockItemAsync(1, 1);
        sourceStock.ReceiveStock(30);

        var service = new StockService(stockRepo, warehouseRepo, productRepo);

        var (outMov, inMov) = await service.TransferStockAsync(
            productId: 1,
            sourceWarehouseId: 1,
            targetWarehouseId: 2,
            quantity: 12,
            note: "Závoz na centrálu",
            performedBy: "Skladník Pavel");

        outMov.MovementType.Should().Be(StockMovementType.TransferOut);
        outMov.Quantity.Should().Be(12);
        outMov.WarehouseId.Should().Be(1);

        inMov.MovementType.Should().Be(StockMovementType.TransferIn);
        inMov.Quantity.Should().Be(12);
        inMov.WarehouseId.Should().Be(2);

        sourceStock.QuantityOnHand.Should().Be(18); // 30 - 12 = 18

        var targetStock = await stockRepo.GetStockItemAsync(1, 2);
        targetStock!.QuantityOnHand.Should().Be(12); // 0 + 12 = 12

        stockRepo.Movements.Should().HaveCount(2);
    }

    [Fact]
    public async Task TransferStockAsync_SameSourceAndTarget_ShouldThrowDomainException()
    {
        var stockRepo = new FakeStockRepository();
        var warehouseRepo = new FakeWarehouseRepository();
        var productRepo = new FakeProductRepository();

        var service = new StockService(stockRepo, warehouseRepo, productRepo);

        var act = async () => await service.TransferStockAsync(1, 1, 1, 5, "Test", "Petr");

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*musí být různé*");
    }

    [Fact]
    public async Task AdjustStockAsync_InventoryDiscrepancy_ShouldUpdateCount_AndRecordAdjustment()
    {
        var stockRepo = new FakeStockRepository();
        var warehouseRepo = new FakeWarehouseRepository();
        var productRepo = new FakeProductRepository();

        var product = new Product("HRA-001", "Dřevěná káča", "Popis káči", 80m, 120m) { Id = 1 };
        productRepo.Items.Add(product);

        var warehouse = new Warehouse("TREB-GAR", "Garáž Třebechovice") { Id = 1 };
        warehouseRepo.Items.Add(warehouse);

        var stockItem = await stockRepo.GetOrCreateStockItemAsync(1, 1);
        stockItem.ReceiveStock(10); // Papírově evidováno 10 ks

        var service = new StockService(stockRepo, warehouseRepo, productRepo);

        // Inventurní manko: fyzicky napočítáno pouze 7 ks
        var movement = await service.AdjustStockAsync(
            productId: 1,
            warehouseId: 1,
            actualCount: 7,
            reason: "Nalezeny 3 ks poškozené vlhkostí",
            performedBy: "Petr Doležal");

        movement.MovementType.Should().Be(StockMovementType.InventoryDiscrepancy);
        movement.Quantity.Should().Be(3); // Rozdíl 3 ks
        movement.PreviousQuantity.Should().Be(10);
        movement.NewQuantity.Should().Be(7);
        movement.Note.Should().Contain("Nalezeny 3 ks poškozené vlhkostí");

        stockItem.QuantityOnHand.Should().Be(7);
    }
}
