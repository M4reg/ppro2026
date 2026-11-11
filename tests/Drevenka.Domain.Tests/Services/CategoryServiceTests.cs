using Drevenka.Domain.Entities;
using Drevenka.Domain.Exceptions;
using Drevenka.Domain.Interfaces;
using Drevenka.Domain.Services;
using FluentAssertions;
using Xunit;

namespace Drevenka.Domain.Tests.Services;

public class CategoryServiceTests
{
    private class FakeCategoryRepository : ICategoryRepository
    {
        public readonly List<Category> Items = [];

        public Task<Category?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            return Task.FromResult(Items.FirstOrDefault(c => c.Id == id));
        }

        public Task<Category?> GetByNameAsync(string name, CancellationToken ct = default)
        {
            var norm = name.Trim().ToLower();
            return Task.FromResult(Items.FirstOrDefault(c => c.Name.ToLower() == norm));
        }

        public Task<IReadOnlyList<Category>> GetAllAsync(bool onlyActive = false, CancellationToken ct = default)
        {
            IEnumerable<Category> q = Items;
            if (onlyActive) q = q.Where(c => c.IsActive);
            return Task.FromResult<IReadOnlyList<Category>>(q.OrderBy(c => c.Name).ToList());
        }

        public Task<bool> ExistsNameAsync(string name, int? excludeId = null, CancellationToken ct = default)
        {
            var norm = name.Trim().ToLower();
            return Task.FromResult(Items.Any(c => c.Name.ToLower() == norm && (!excludeId.HasValue || c.Id != excludeId.Value)));
        }

        public Task AddAsync(Category category, CancellationToken ct = default)
        {
            if (category.Id == 0)
            {
                category.Id = Items.Count > 0 ? Items.Max(c => c.Id) + 1 : 1;
            }
            Items.Add(category);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Category category, CancellationToken ct = default)
        {
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Category category, CancellationToken ct = default)
        {
            Items.Remove(category);
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task CreateCategoryAsync_WithUniqueName_ShouldSucceed()
    {
        // Arrange
        var repo = new FakeCategoryRepository();
        var service = new CategoryService(repo);

        // Act
        var category = await service.CreateCategoryAsync("Dřevěná vozidla", "Vozítka na kolečkách");

        // Assert
        category.Should().NotBeNull();
        category.Name.Should().Be("Dřevěná vozidla");
        category.Id.Should().Be(1);
        repo.Items.Should().ContainSingle(c => c.Name == "Dřevěná vozidla");
    }

    [Fact]
    public async Task CreateCategoryAsync_WithDuplicateName_ShouldThrowDomainException()
    {
        // Arrange
        var repo = new FakeCategoryRepository();
        repo.Items.Add(new Category("Dárky do 300 Kč") { Id = 1 });
        var service = new CategoryService(repo);

        // Act
        Func<Task> act = async () => await service.CreateCategoryAsync("dárky do 300 kč");

        // Assert
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*již v systému existuje*");
    }

    [Fact]
    public async Task UpdateCategoryAsync_WithNewUniqueName_ShouldSucceed()
    {
        // Arrange
        var repo = new FakeCategoryRepository();
        var cat = new Category("Původní", "Popis") { Id = 1 };
        repo.Items.Add(cat);
        var service = new CategoryService(repo);

        // Act
        var updated = await service.UpdateCategoryAsync(1, "Nový název", "Nový popis", isActive: true);

        // Assert
        updated.Name.Should().Be("Nový název");
        cat.Name.Should().Be("Nový název");
    }

    [Fact]
    public async Task UpdateCategoryAsync_WithDuplicateNameFromOther_ShouldThrowDomainException()
    {
        // Arrange
        var repo = new FakeCategoryRepository();
        repo.Items.Add(new Category("Existující") { Id = 1 });
        repo.Items.Add(new Category("Moje kategorie") { Id = 2 });
        var service = new CategoryService(repo);

        // Act
        Func<Task> act = async () => await service.UpdateCategoryAsync(2, "Existující", null, isActive: true);

        // Assert
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*již existuje u jiné položky*");
    }

    [Fact]
    public async Task DeleteCategoryAsync_ShouldRemoveFromRepository()
    {
        // Arrange
        var repo = new FakeCategoryRepository();
        var cat = new Category("Kategorie ke smazání") { Id = 1 };
        repo.Items.Add(cat);
        var service = new CategoryService(repo);

        // Act
        await service.DeleteCategoryAsync(1);

        // Assert
        repo.Items.Should().BeEmpty();
    }
}

