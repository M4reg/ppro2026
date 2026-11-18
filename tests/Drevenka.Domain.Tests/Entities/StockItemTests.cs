using Drevenka.Domain.Entities;
using Drevenka.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace Drevenka.Domain.Tests.Entities;

public class StockItemTests
{
    [Fact]
    public void Constructor_WithValidData_ShouldInitializeCorrectly()
    {
        var item = new StockItem(productId: 1, warehouseId: 2, initialQuantity: 20, minStockLevel: 5);

        item.ProductId.Should().Be(1);
        item.WarehouseId.Should().Be(2);
        item.QuantityOnHand.Should().Be(20);
        item.ReservedQuantity.Should().Be(0);
        item.AvailableQuantity.Should().Be(20);
        item.MinStockLevel.Should().Be(5);
        item.IsLowStock.Should().BeFalse();
    }

    [Theory]
    [InlineData(0, 1, 10)]
    [InlineData(-1, 1, 10)]
    [InlineData(1, 0, 10)]
    [InlineData(1, -1, 10)]
    public void Constructor_InvalidIds_ShouldThrowDomainException(int productId, int warehouseId, int qty)
    {
        var act = () => new StockItem(productId, warehouseId, qty);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Constructor_NegativeInitialQuantity_ShouldThrowDomainException()
    {
        var act = () => new StockItem(productId: 1, warehouseId: 1, initialQuantity: -5);
        act.Should().Throw<DomainException>().WithMessage("*Počáteční zásoba nesmí být záporná*");
    }

    [Fact]
    public void ReceiveStock_PositiveQuantity_ShouldIncreaseQuantityOnHand()
    {
        var item = new StockItem(productId: 1, warehouseId: 1, initialQuantity: 10);
        item.ReceiveStock(15);

        item.QuantityOnHand.Should().Be(25);
        item.AvailableQuantity.Should().Be(25);
        item.UpdatedAt.Should().NotBeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void ReceiveStock_NonPositiveQuantity_ShouldThrowDomainException(int qty)
    {
        var item = new StockItem(productId: 1, warehouseId: 1, initialQuantity: 10);
        var act = () => item.ReceiveStock(qty);
        act.Should().Throw<DomainException>().WithMessage("*Přijímané množství musí být větší než 0*");
    }

    [Fact]
    public void IssueStock_ValidQuantity_ShouldDecreaseQuantityOnHand()
    {
        var item = new StockItem(productId: 1, warehouseId: 1, initialQuantity: 10);
        item.IssueStock(4);

        item.QuantityOnHand.Should().Be(6);
        item.AvailableQuantity.Should().Be(6);
        item.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void IssueStock_ExceedingQuantityOnHand_ShouldThrowDomainException_ZakazVydejeDoZaporu()
    {
        var item = new StockItem(productId: 1, warehouseId: 1, initialQuantity: 5);

        var act = () => item.IssueStock(6);

        act.Should().Throw<DomainException>()
            .WithMessage("*Zákaz výdeje do záporu*");
    }

    [Fact]
    public void ReserveStock_ValidQuantity_ShouldIncreaseReservedQuantity_AndDecreaseAvailable()
    {
        var item = new StockItem(productId: 1, warehouseId: 1, initialQuantity: 10);
        item.ReserveStock(4);

        item.QuantityOnHand.Should().Be(10);
        item.ReservedQuantity.Should().Be(4);
        item.AvailableQuantity.Should().Be(6);
    }

    [Fact]
    public void ReserveStock_ExceedingAvailableQuantity_ShouldThrowDomainException()
    {
        var item = new StockItem(productId: 1, warehouseId: 1, initialQuantity: 10);
        item.ReserveStock(8);

        var act = () => item.ReserveStock(3); // Požadováno 3, ale volné jen 2

        act.Should().Throw<DomainException>()
            .WithMessage("*Objednávku nelze pokrýt*");
    }

    [Fact]
    public void ReleaseReservation_ShouldDecreaseReservedQuantity()
    {
        var item = new StockItem(productId: 1, warehouseId: 1, initialQuantity: 10);
        item.ReserveStock(5);
        item.ReleaseReservation(2);

        item.ReservedQuantity.Should().Be(3);
        item.AvailableQuantity.Should().Be(7);
    }

    [Fact]
    public void ShipReserved_ShouldDecreaseBothPhysicalAndReserved()
    {
        var item = new StockItem(productId: 1, warehouseId: 1, initialQuantity: 10);
        item.ReserveStock(5);

        item.ShipReserved(3);

        item.QuantityOnHand.Should().Be(7);
        item.ReservedQuantity.Should().Be(2);
        item.AvailableQuantity.Should().Be(5);
    }

    [Fact]
    public void ShipReserved_ExceedingReserved_ShouldThrowDomainException()
    {
        var item = new StockItem(productId: 1, warehouseId: 1, initialQuantity: 10);
        item.ReserveStock(3);

        var act = () => item.ShipReserved(4);

        act.Should().Throw<DomainException>()
            .WithMessage("*Nelze expedovat více než zarezervované množství*");
    }

    [Fact]
    public void AdjustStock_ShouldSetPhysicalQuantity_AndEnforceNonNegative()
    {
        var item = new StockItem(productId: 1, warehouseId: 1, initialQuantity: 10);
        item.AdjustStock(14); // Inventurní přebytek

        item.QuantityOnHand.Should().Be(14);

        var act = () => item.AdjustStock(-1);
        act.Should().Throw<DomainException>().WithMessage("*nesmí být záporný*");
    }

    [Fact]
    public void IsLowStock_ShouldReflectMinStockLevel()
    {
        var item = new StockItem(productId: 1, warehouseId: 1, initialQuantity: 6, minStockLevel: 5);
        item.IsLowStock.Should().BeFalse();

        item.IssueStock(1); // 5 ks <= 5
        item.IsLowStock.Should().BeTrue();

        item.IssueStock(2); // 3 ks <= 5
        item.IsLowStock.Should().BeTrue();
    }
}
