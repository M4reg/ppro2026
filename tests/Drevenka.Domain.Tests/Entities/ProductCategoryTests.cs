using Drevenka.Domain.Entities;
using Drevenka.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace Drevenka.Domain.Tests.Entities;

public class ProductCategoryTests
{
    [Fact]
    public void AddCategory_WithValidId_ShouldAddToProductCategories()
    {
        // Arrange
        var product = new Product("KAC-001", "Dřevěná káča", null, 40m, 100m) { Id = 1 };

        // Act
        product.AddCategory(10);

        // Assert
        product.ProductCategories.Should().ContainSingle(pc => pc.CategoryId == 10 && pc.ProductId == 1);
        product.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void AddCategory_WithDuplicateId_ShouldNotAddTwice()
    {
        // Arrange
        var product = new Product("KAC-001", "Dřevěná káča", null, 40m, 100m) { Id = 1 };

        // Act
        product.AddCategory(10);
        product.AddCategory(10);

        // Assert
        product.ProductCategories.Should().ContainSingle(pc => pc.CategoryId == 10);
    }

    [Fact]
    public void AddCategory_WithInvalidId_ShouldThrowDomainException()
    {
        // Arrange
        var product = new Product("KAC-001", "Dřevěná káča", null, 40m, 100m);

        // Act
        Action act = () => product.AddCategory(0);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*Neplatné ID kategorie*");
    }

    [Fact]
    public void RemoveCategory_ShouldRemoveFromProductCategories()
    {
        // Arrange
        var product = new Product("KAC-001", "Dřevěná káča", null, 40m, 100m) { Id = 1 };
        product.AddCategory(10);
        product.AddCategory(20);

        // Act
        product.RemoveCategory(10);

        // Assert
        product.ProductCategories.Should().ContainSingle(pc => pc.CategoryId == 20);
    }

    [Fact]
    public void SetCategories_ShouldSynchronizeCorrectly()
    {
        // Arrange
        var product = new Product("KAC-001", "Dřevěná káča", null, 40m, 100m) { Id = 1 };
        product.AddCategory(10);
        product.AddCategory(20);

        // Act - ponechá 20, odebere 10, přidá 30
        product.SetCategories([20, 30]);

        // Assert
        product.ProductCategories.Should().HaveCount(2);
        product.ProductCategories.Select(pc => pc.CategoryId).Should().BeEquivalentTo([20, 30]);
    }
}

