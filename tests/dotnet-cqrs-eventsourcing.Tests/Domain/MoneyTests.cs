#nullable enable
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

/// <summary>
/// Tests for the Money class.
/// </summary>
public sealed class MoneyTests
{
    /// <summary>
    /// Verifies that a valid amount and currency create a Money instance.
    /// </summary>
    [Fact]
    public void Constructor_ValidAmountAndCurrency_CreatesInstance()
    {
        var money = new Money(100m, "USD");

        money.Amount.Should().Be(100m);
        money.Currency.Should().Be("USD");
    }

    /// <summary>
    /// Verifies that a negative amount throws a DomainException.
    /// </summary>
    [Fact]
    public void Constructor_NegativeAmount_ThrowsDomainException()
    {
        var act = () => new Money(-1m, "USD");

        act.Should().Throw<DomainException>()
            .WithMessage("*negative*")
            .And.Code.Should().Be("INVALID_AMOUNT");
    }

    /// <summary>
    /// Verifies that an invalid currency code throws a DomainException.
    /// </summary>
    /// <param name="badCurrency">The invalid currency code.</param>
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

    /// <summary>
    /// Verifies that an amount exceeding the maximum throws a DomainException.
    /// </summary>
    [Fact]
    public void Constructor_AmountExceedsMaximum_ThrowsDomainException()
    {
        var act = () => new Money(CqrsConstants.MaximumBalance + 1m, "USD");

        act.Should().Throw<DomainException>()
            .And.Code.Should().Be("AMOUNT_EXCEEDS_MAXIMUM");
    }

    /// <summary>
    /// Verifies that the currency code is normalized to uppercase.
    /// </summary>
    [Fact]
    public void Constructor_CurrencyCodeNormalizedToUpperCase()
    {
        var money = new Money(50m, "eur");

        money.Currency.Should().Be("EUR");
    }

    /// <summary>
    /// Verifies that adding two Money instances with the same currency returns the summed amount.
    /// </summary>
    [Fact]
    public void Add_SameCurrency_ReturnsSummedAmount()
    {
        var a = new Money(100m, "USD");
        var b = new Money(50m, "USD");

        var result = a.Add(b);

        result.Amount.Should().Be(150m);
        result.Currency.Should().Be("USD");
    }

    /// <summary>
    /// Verifies that adding two Money instances with different currencies throws a DomainException.
    /// </summary>
    [Fact]
    public void Add_DifferentCurrencies_ThrowsDomainException()
    {
        var usd = new Money(100m, "USD");
        var eur = new Money(50m, "EUR");

        var act = () => usd.Add(eur);

        act.Should().Throw<DomainException>()
            .And.Code.Should().Be("CURRENCY_MISMATCH");
    }

    /// <summary>
    /// Verifies that subtracting a sufficient amount returns the difference.
    /// </summary>
    [Fact]
    public void Subtract_SufficientAmount_ReturnsDifference()
    {
        var a = new Money(200m, "USD");
        var b = new Money(75m, "USD");

        var result = a.Subtract(b);

        result.Amount.Should().Be(125m);
    }

    /// <summary>
    /// Verifies that subtracting an amount that would result in a negative amount throws a DomainException.
    /// </summary>
    [Fact]
    public void Subtract_WouldResultInNegative_ThrowsDomainException()
    {
        var a = new Money(50m, "USD");
        var b = new Money(100m, "USD");

        var act = () => a.Subtract(b);

        act.Should().Throw<DomainException>()
            .And.Code.Should().Be("INSUFFICIENT_FUNDS");
    }

    /// <summary>
    /// Verifies that IsGreaterThan returns true for a larger amount.
    /// </summary>
    [Fact]
    public void IsGreaterThan_LargerAmount_ReturnsTrue()
    {
        var large = new Money(500m, "USD");
        var small = new Money(100m, "USD");

        large.IsGreaterThan(small).Should().BeTrue();
    }

    /// <summary>
    /// Verifies that IsLessThan returns true for a smaller amount.
    /// </summary>
    [Fact]
    public void IsLessThan_SmallerAmount_ReturnsTrue()
    {
        var small = new Money(10m, "USD");
        var large = new Money(100m, "USD");

        small.IsLessThan(large).Should().BeTrue();
    }

    /// <summary>
    /// Verifies that Equals returns true for two Money instances with the same amount and currency.
    /// </summary>
    [Fact]
    public void Equals_SameAmountAndCurrency_ReturnsTrue()
    {
        var a = new Money(100m, "USD");
        var b = new Money(100m, "USD");

        a.Should().Be(b);
        (a == b).Should().BeTrue();
    }

    /// <summary>
    /// Verifies that Equals returns false for two Money instances with different amounts.
    /// </summary>
    [Fact]
    public void Equals_DifferentAmount_ReturnsFalse()
    {
        var a = new Money(100m, "USD");
        var b = new Money(200m, "USD");

        (a == b).Should().BeFalse();
        (a != b).Should().BeTrue();
    }
}
