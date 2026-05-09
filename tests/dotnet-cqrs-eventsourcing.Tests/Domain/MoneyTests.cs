// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Tests.Domain;

using DotNetCqrsEventSourcing.Domain.ValueObjects;
using DotNetCqrsEventSourcing.Shared.Constants;
using DotNetCqrsEventSourcing.Shared.Exceptions;
using FluentAssertions;
using Xunit;

public class MoneyTests
{
    [Fact]
    public void Constructor_ValidAmountAndCurrency_CreatesInstance()
    {
        var money = new Money(100m, "USD");

        money.Amount.Should().Be(100m);
        money.Currency.Should().Be("USD");
    }

    [Fact]
    public void Constructor_NegativeAmount_ThrowsDomainException()
    {
        var act = () => new Money(-1m, "USD");

        act.Should().Throw<DomainException>()
            .WithMessage("*negative*")
            .And.Code.Should().Be("INVALID_AMOUNT");
    }

    [Theory]
    [InlineData("")]
    [InlineData("US")]
    [InlineData("USDD")]
    [InlineData("  ")]
    public void Constructor_InvalidCurrencyCode_ThrowsDomainException(string badCurrency)
    {
        var act = () => new Money(10m, badCurrency);

        act.Should().Throw<DomainException>()
            .And.Code.Should().Be("INVALID_CURRENCY_CODE");
    }

    [Fact]
    public void Constructor_AmountExceedsMaximum_ThrowsDomainException()
    {
        var act = () => new Money(CqrsConstants.MaximumBalance + 1m, "USD");

        act.Should().Throw<DomainException>()
            .And.Code.Should().Be("AMOUNT_EXCEEDS_MAXIMUM");
    }

    [Fact]
    public void Constructor_CurrencyCodeNormalizedToUpperCase()
    {
        var money = new Money(50m, "eur");

        money.Currency.Should().Be("EUR");
    }

    [Fact]
    public void Add_SameCurrency_ReturnsSummedAmount()
    {
        var a = new Money(100m, "USD");
        var b = new Money(50m, "USD");

        var result = a.Add(b);

        result.Amount.Should().Be(150m);
        result.Currency.Should().Be("USD");
    }

    [Fact]
    public void Add_DifferentCurrencies_ThrowsDomainException()
    {
        var usd = new Money(100m, "USD");
        var eur = new Money(50m, "EUR");

        var act = () => usd.Add(eur);

        act.Should().Throw<DomainException>()
            .And.Code.Should().Be("CURRENCY_MISMATCH");
    }

    [Fact]
    public void Subtract_SufficientAmount_ReturnsDifference()
    {
        var a = new Money(200m, "USD");
        var b = new Money(75m, "USD");

        var result = a.Subtract(b);

        result.Amount.Should().Be(125m);
    }

    [Fact]
    public void Subtract_WouldResultInNegative_ThrowsDomainException()
    {
        var a = new Money(50m, "USD");
        var b = new Money(100m, "USD");

        var act = () => a.Subtract(b);

        act.Should().Throw<DomainException>()
            .And.Code.Should().Be("INSUFFICIENT_FUNDS");
    }

    [Fact]
    public void IsGreaterThan_LargerAmount_ReturnsTrue()
    {
        var large = new Money(500m, "USD");
        var small = new Money(100m, "USD");

        large.IsGreaterThan(small).Should().BeTrue();
    }

    [Fact]
    public void IsLessThan_SmallerAmount_ReturnsTrue()
    {
        var small = new Money(10m, "USD");
        var large = new Money(100m, "USD");

        small.IsLessThan(large).Should().BeTrue();
    }

    [Fact]
    public void Equals_SameAmountAndCurrency_ReturnsTrue()
    {
        var a = new Money(100m, "USD");
        var b = new Money(100m, "USD");

        a.Should().Be(b);
        (a == b).Should().BeTrue();
    }

    [Fact]
    public void Equals_DifferentAmount_ReturnsFalse()
    {
        var a = new Money(100m, "USD");
        var b = new Money(200m, "USD");

        (a == b).Should().BeFalse();
        (a != b).Should().BeTrue();
    }
}
