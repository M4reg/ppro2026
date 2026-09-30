using Drevenka.Domain.Entities;
using Drevenka.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace Drevenka.Domain.Tests.Entities;

public class ProductTests
{
    [Fact]
    public void Constructor_WithValidArguments_ShouldCreateProductSuccessfully()
    {
        // Arrange & Act
        var product = new Product("kac-001", "Dřevěná káča", "Tradiční hračka", 45m, 120m);

        // Assert
        product.Code.Should().Be("KAC-001"); // SKU by mělo být normalizováno na velká písmena
        product.Name.Should().Be("Dřevěná káča");
        product.Description.Should().Be("Tradiční hračka");
        product.PurchasePrice.Should().Be(45m);
        product.SellingPrice.Should().Be(120m);
        product.IsActive.Should().BeTrue();
        product.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        product.UpdatedAt.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Constructor_WithEmptyCode_ShouldThrowDomainException(string? invalidCode)
    {
        // Act
        Action act = () => new Product(invalidCode!, "Káča", null, 50m, 100m);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*Kód produktu (SKU) je povinný*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Constructor_WithEmptyName_ShouldThrowDomainException(string? invalidName)
    {
        // Act
        Action act = () => new Product("KAC-001", invalidName!, null, 50m, 100m);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*Název produktu je povinný*");
    }

    [Fact]
    public void Constructor_WithNegativePurchasePrice_ShouldThrowDomainException()
    {
        // Act
        Action act = () => new Product("KAC-001", "Káča", null, -10m, 100m);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*Nákupní cena produktu nesmí být záporná*");
    }

    [Fact]
    public void Constructor_WithNegativeSellingPrice_ShouldThrowDomainException()
    {
        // Act
        Action act = () => new Product("KAC-001", "Káča", null, 50m, -5m);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*Prodejní cena produktu nesmí být záporná*");
    }

    [Fact]
    public void Update_WithValidValues_ShouldUpdatePropertiesAndSetUpdatedAt()
    {
        // Arrange
        var product = new Product("KAC-001", "Původní název", null, 40m, 90m);

        // Act
        product.Update("kac-001-novy", "Nový název", "Nový popis", 50m, 110m, isActive: false);

        // Assert
        product.Code.Should().Be("KAC-001-NOVY");
        product.Name.Should().Be("Nový název");
        product.Description.Should().Be("Nový popis");
        product.PurchasePrice.Should().Be(50m);
        product.SellingPrice.Should().Be(110m);
        product.IsActive.Should().BeFalse();
        product.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Deactivate_And_Activate_ShouldToggleStatusAndSetUpdatedAt()
    {
        // Arrange
        var product = new Product("KAC-001", "Káča", null, 40m, 90m);
        product.IsActive.Should().BeTrue();

        // Act: Deaktivace
        product.Deactivate();
        product.IsActive.Should().BeFalse();
        product.UpdatedAt.Should().NotBeNull();

        // Act: Reaktivace
        product.Activate();
        product.IsActive.Should().BeTrue();
    }
}
