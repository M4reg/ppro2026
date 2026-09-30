using Drevenka.Domain.Entities;
using Drevenka.Domain.Exceptions;
using Drevenka.Domain.Interfaces;
using Drevenka.Domain.Services;
using FluentAssertions;
using Xunit;

namespace Drevenka.Domain.Tests.Services;

public class ProductServiceTests
{
    private class FakeProductRepository : IProductRepository
    {
        public readonly List<Product> Items = [];

        public Task<Product?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            return Task.FromResult(Items.FirstOrDefault(p => p.Id == id));
        }

        public Task<Product?> GetByCodeAsync(string code, CancellationToken ct = default)
        {
            var norm = code.Trim().ToUpperInvariant();
            return Task.FromResult(Items.FirstOrDefault(p => p.Code == norm));
        }

        public Task<IReadOnlyList<Product>> GetAllAsync(string? searchTerm = null, bool onlyActive = false, CancellationToken ct = default)
        {
            IEnumerable<Product> q = Items;
            if (onlyActive) q = q.Where(x => x.IsActive);
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var s = searchTerm.ToLower();
                q = q.Where(x => x.Name.ToLower().Contains(s) || x.Code.ToLower().Contains(s));
            }
            return Task.FromResult<IReadOnlyList<Product>>(q.ToList());
        }

        public Task<bool> ExistsCodeAsync(string code, int? excludeId = null, CancellationToken ct = default)
        {
            var norm = code.Trim().ToUpperInvariant();
            return Task.FromResult(Items.Any(p => p.Code == norm && (!excludeId.HasValue || p.Id != excludeId.Value)));
        }

        public Task AddAsync(Product product, CancellationToken ct = default)
        {
            if (product.Id == 0)
            {
                product.Id = Items.Count > 0 ? Items.Max(p => p.Id) + 1 : 1;
            }
            Items.Add(product);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Product product, CancellationToken ct = default)
        {
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Product product, CancellationToken ct = default)
        {
            Items.Remove(product);
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task CreateProductAsync_WithUniqueSku_ShouldSucceed()
    {
        // Arrange
        var repo = new FakeProductRepository();
        var service = new ProductService(repo);

        // Act
        var product = await service.CreateProductAsync("VLK-001", "Dřevěný vláček", "Popis vláčku", 100m, 250m);

        // Assert
        product.Should().NotBeNull();
        product.Code.Should().Be("VLK-001");
        product.Id.Should().BeGreaterThan(0);
        repo.Items.Should().ContainSingle(p => p.Code == "VLK-001");
    }

    [Fact]
    public async Task CreateProductAsync_WithDuplicateSku_ShouldThrowDomainException()
    {
        // Arrange
        var repo = new FakeProductRepository();
        var existing = new Product("VLK-001", "Původní vláček", null, 100m, 200m) { Id = 1 };
        repo.Items.Add(existing);

        var service = new ProductService(repo);

        // Act
        Func<Task> act = async () => await service.CreateProductAsync("vlk-001", "Druhý vláček", null, 120m, 240m);

        // Assert
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*již v systému existuje*");
    }

    [Fact]
    public async Task UpdateProductAsync_WithConflictingSku_ShouldThrowDomainException()
    {
        // Arrange
        var repo = new FakeProductRepository();
        var product1 = new Product("VLK-001", "Vláček 1", null, 100m, 200m) { Id = 1 };
        var product2 = new Product("VLK-002", "Vláček 2", null, 150m, 300m) { Id = 2 };
        repo.Items.AddRange([product1, product2]);

        var service = new ProductService(repo);

        // Act: Zkusíme změnit product2 kód na VLK-001
        Func<Task> act = async () => await service.UpdateProductAsync(
            product2.Id,
            "VLK-001",
            product2.Name,
            product2.Description,
            product2.PurchasePrice,
            product2.SellingPrice,
            product2.IsActive
        );

        // Assert
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*již existuje u jiné položky*");
    }
}
