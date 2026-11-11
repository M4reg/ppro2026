using Drevenka.Domain.Entities;
using Drevenka.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace Drevenka.Domain.Tests.Entities;

public class CustomerTests
{
    [Fact]
    public void CreateCustomer_WithValidData_ShouldCreateSuccessfully()
    {
        var customer = new Customer(
            "Jana Nováková",
            "jana.novakova@priklad.cz",
            "+420 602 123 456",
            "Pospíšilova 312",
            "Hradec Králové",
            "500 03",
            "Pravidelný odběratel");

        customer.FullName.Should().Be("Jana Nováková");
        customer.Email.Should().Be("jana.novakova@priklad.cz");
        customer.Phone.Should().Be("+420 602 123 456");
        customer.IsActive.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateCustomer_WithEmptyName_ShouldThrowDomainException(string invalidName)
    {
        var act = () => new Customer(
            invalidName,
            "test@priklad.cz",
            "123456789",
            "Ulice 1",
            "Město",
            "12345");

        act.Should().Throw<DomainException>()
            .WithMessage("*Jméno zákazníka nesmí být prázdné*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid-email")]
    public void CreateCustomer_WithInvalidEmail_ShouldThrowDomainException(string invalidEmail)
    {
        var act = () => new Customer(
            "Test Zákazník",
            invalidEmail,
            "123456789",
            "Ulice 1",
            "Město",
            "12345");

        act.Should().Throw<DomainException>()
            .WithMessage("*E-mail zákazníka musí být platný*");
    }

    [Fact]
    public void DeactivateCustomer_ShouldSetIsActiveToFalse_WithoutDeleting()
    {
        var customer = new Customer(
            "Petr Zákazník",
            "petr@priklad.cz",
            "123456789",
            "Ulice 1",
            "Město",
            "12345");

        customer.Deactivate();

        customer.IsActive.Should().BeFalse();
    }
}

