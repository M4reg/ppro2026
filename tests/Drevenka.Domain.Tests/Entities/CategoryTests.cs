using Drevenka.Domain.Entities;
using Drevenka.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace Drevenka.Domain.Tests.Entities;

public class CategoryTests
{
    [Fact]
    public void Constructor_WithValidArguments_ShouldCreateCategorySuccessfully()
    {
        // Act
        var category = new Category("Pro nejmenší", "Hračky pro batolata");

        // Assert
        category.Name.Should().Be("Pro nejmenší");
        category.Description.Should().Be("Hračky pro batolata");
        category.IsActive.Should().BeTrue();
        category.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        category.UpdatedAt.Should().BeNull();
        category.ProductCategories.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Constructor_WithEmptyName_ShouldThrowDomainException(string? invalidName)
    {
        // Act
        Action act = () => new Category(invalidName!);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*Název kategorie je povinný*");
    }

    [Fact]
    public void Update_WithValidValues_ShouldUpdatePropertiesAndSetUpdatedAt()
    {
        // Arrange
        var category = new Category("Starý název", "Starý popis");

        // Act
        category.Update("Nový název", "Nový popis", isActive: false);

        // Assert
        category.Name.Should().Be("Nový název");
        category.Description.Should().Be("Nový popis");
        category.IsActive.Should().BeFalse();
        category.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Deactivate_And_Activate_ShouldToggleStatusAndSetUpdatedAt()
    {
        // Arrange
        var category = new Category("Kategorie", "Popis");

        // Act & Assert 1
        category.Deactivate();
        category.IsActive.Should().BeFalse();
        category.UpdatedAt.Should().NotBeNull();

        // Act & Assert 2
        category.Activate();
        category.IsActive.Should().BeTrue();
    }
}

