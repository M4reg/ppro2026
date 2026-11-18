using Drevenka.Domain.Entities;
using Drevenka.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace Drevenka.Domain.Tests.Entities;

public class StockMovementTests
{
    [Fact]
    public void Constructor_WithValidReceipt_ShouldInitializeCorrectly()
    {
        var m = new StockMovement(
            stockItemId: 1,
            productId: 10,
            warehouseId: 2,
            movementType: StockMovementType.Receipt,
            quantity: 15,
            previousQuantity: 5,
            newQuantity: 20,
            note: "Naskladnění z dílny",
            referenceNumber: "DOKLAD-01",
            performedBy: "Petr Doležal");

        m.StockItemId.Should().Be(1);
        m.ProductId.Should().Be(10);
        m.WarehouseId.Should().Be(2);
        m.MovementType.Should().Be(StockMovementType.Receipt);
        m.Quantity.Should().Be(15);
        m.PreviousQuantity.Should().Be(5);
        m.NewQuantity.Should().Be(20);
        m.Note.Should().Be("Naskladnění z dílny");
        m.ReferenceNumber.Should().Be("DOKLAD-01");
        m.PerformedBy.Should().Be("Petr Doležal");
        m.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Constructor_InventoryDiscrepancyWithoutNote_ShouldThrowDomainException()
    {
        var act = () => new StockMovement(
            stockItemId: 1,
            productId: 1,
            warehouseId: 1,
            movementType: StockMovementType.InventoryDiscrepancy,
            quantity: 3,
            previousQuantity: 10,
            newQuantity: 7,
            note: null); // Chybí povinné odůvodnění manka

        act.Should().Throw<DomainException>()
            .WithMessage("*povinné zadat textové odůvodnění*");
    }

    [Theory]
    [InlineData(0, 5, 5)]
    [InlineData(-1, 5, 4)]
    public void Constructor_NonPositiveQuantity_ShouldThrowDomainException(int qty, int prev, int next)
    {
        var act = () => new StockMovement(
            stockItemId: 1,
            productId: 1,
            warehouseId: 1,
            movementType: StockMovementType.Receipt,
            quantity: qty,
            previousQuantity: prev,
            newQuantity: next);

        act.Should().Throw<DomainException>()
            .WithMessage("*Množství pohybu musí být větší než 0*");
    }

    [Fact]
    public void Constructor_NegativeState_ShouldThrowDomainException()
    {
        var act = () => new StockMovement(
            stockItemId: 1,
            productId: 1,
            warehouseId: 1,
            movementType: StockMovementType.Issue,
            quantity: 5,
            previousQuantity: 2,
            newQuantity: -3);

        act.Should().Throw<DomainException>()
            .WithMessage("*Stav skladu v pohybu nesmí být záporný*");
    }
}
