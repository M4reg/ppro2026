using Drevenka.Domain.Entities;
using Drevenka.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace Drevenka.Domain.Tests.Entities;

public class OrderTests
{
    [Fact]
    public void CreateOrder_WithValidData_ShouldInitializeAsNew()
    {
        var order = new Order(
            "OBJ-2026-0001",
            customerId: 1,
            deliveryAddress: "Pospíšilova 312",
            deliveryCity: "Hradec Králové",
            deliveryPostalCode: "500 03");

        order.OrderNumber.Should().Be("OBJ-2026-0001");
        order.Status.Should().Be(OrderStatus.New);
        order.TotalAmount.Should().Be(0m);
        order.TotalItemsCount.Should().Be(0);
    }

    [Fact]
    public void AddItem_ShouldSnapshotUnitPrice_AndCalculateTotal()
    {
        var order = new Order(
            "OBJ-2026-0001",
            customerId: 1,
            deliveryAddress: "Pospíšilova 312",
            deliveryCity: "Hradec Králové",
            deliveryPostalCode: "500 03");

        order.AddItem(productId: 1, quantity: 3, unitPrice: 120.00m);
        order.AddItem(productId: 2, quantity: 1, unitPrice: 450.00m);

        order.Items.Should().HaveCount(2);
        order.TotalItemsCount.Should().Be(4);
        order.TotalAmount.Should().Be(3 * 120.00m + 1 * 450.00m); // 810.00 Kč
    }

    [Fact]
    public void AddItem_DuplicateProduct_ShouldThrowDomainException()
    {
        var order = new Order(
            "OBJ-2026-0001",
            customerId: 1,
            deliveryAddress: "Pospíšilova 312",
            deliveryCity: "Hradec Králové",
            deliveryPostalCode: "500 03");

        order.AddItem(productId: 1, quantity: 2, unitPrice: 120.00m);

        var act = () => order.AddItem(productId: 1, quantity: 1, unitPrice: 120.00m);

        act.Should().Throw<DomainException>()
            .WithMessage("*již v objednávce existuje*");
    }

    [Fact]
    public void Confirm_WithoutItems_ShouldThrowDomainException()
    {
        var order = new Order(
            "OBJ-2026-0001",
            customerId: 1,
            deliveryAddress: "Pospíšilova 312",
            deliveryCity: "Hradec Králové",
            deliveryPostalCode: "500 03");

        var act = () => order.Confirm();

        act.Should().Throw<DomainException>()
            .WithMessage("*Objednávka musí obsahovat alespoň jednu položku*");
    }

    [Fact]
    public void ConfirmAndDispatch_ShouldTransitionStatusesCorrectly()
    {
        var order = new Order(
            "OBJ-2026-0001",
            customerId: 1,
            deliveryAddress: "Pospíšilova 312",
            deliveryCity: "Hradec Králové",
            deliveryPostalCode: "500 03");

        order.AddItem(productId: 1, quantity: 2, unitPrice: 100.00m);
        order.Confirm();
        order.Status.Should().Be(OrderStatus.Confirmed);
        order.ConfirmedAt.Should().NotBeNull();

        order.Dispatch();
        order.Status.Should().Be(OrderStatus.Dispatched);
        order.DispatchedAt.Should().NotBeNull();
    }

    [Fact]
    public void Cancel_DispatchedOrder_ShouldThrowDomainException()
    {
        var order = new Order(
            "OBJ-2026-0001",
            customerId: 1,
            deliveryAddress: "Pospíšilova 312",
            deliveryCity: "Hradec Králové",
            deliveryPostalCode: "500 03");

        order.AddItem(productId: 1, quantity: 1, unitPrice: 100.00m);
        order.Confirm();
        order.Dispatch();

        var act = () => order.Cancel("Důvod");

        act.Should().Throw<DomainException>()
            .WithMessage("*Již expedovanou objednávku nelze stornovat*");
    }
}

