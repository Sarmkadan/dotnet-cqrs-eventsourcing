// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetCqrsEventSourcing.Shared.Extensions;
using FluentAssertions;

namespace DotNetCqrsEventSourcing.Tests.Shared;

public class ValidationExtensionsTests
{
    [Fact]
    public void NotNull_NullValue_ThrowsArgumentNullException()
    {
        string? value = null;
        var act = () => value.NotNull("param");
        act.Should().Throw<ArgumentNullException>().WithParameterName("param");
    }

    [Fact]
    public void NotNull_NonNullValue_ReturnsSameInstance()
    {
        var obj = new object();
        obj.NotNull("param").Should().BeSameAs(obj);
    }

    [Fact]
    public void NotNullOrEmpty_NullString_ThrowsArgumentException()
    {
        string? value = null;
        var act = () => value.NotNullOrEmpty("name");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void NotNullOrEmpty_EmptyString_ThrowsArgumentException()
    {
        var act = () => "".NotNullOrEmpty("name");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void NotNullOrEmpty_WhitespaceString_ThrowsArgumentException()
    {
        var act = () => "   ".NotNullOrEmpty("name");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void NotNullOrEmpty_ValidString_ReturnsSameString()
    {
        "hello".NotNullOrEmpty("name").Should().Be("hello");
    }

    [Fact]
    public void NotNegative_NegativeValue_ThrowsArgumentException()
    {
        var act = () => (-1m).NotNegative("amount");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void NotNegative_ZeroValue_ReturnsZero()
    {
        0m.NotNegative("amount").Should().Be(0);
    }

    [Fact]
    public void NotNegative_PositiveValue_ReturnsSameValue()
    {
        100m.NotNegative("amount").Should().Be(100);
    }

    [Fact]
    public void InRange_ValueBelowMin_ThrowsArgumentException()
    {
        var act = () => 0m.InRange(1, 100, "qty");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void InRange_ValueAboveMax_ThrowsArgumentException()
    {
        var act = () => 101m.InRange(1, 100, "qty");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void InRange_ValueAtMin_ReturnsValue()
    {
        1m.InRange(1, 100, "qty").Should().Be(1);
    }

    [Fact]
    public void InRange_ValueAtMax_ReturnsValue()
    {
        100m.InRange(1, 100, "qty").Should().Be(100);
    }

    [Fact]
    public void ValidGuid_InvalidFormat_ThrowsArgumentException()
    {
        var act = () => "not-a-guid".ValidGuid("id");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ValidGuid_ValidFormat_ReturnsSameString()
    {
        var guid = Guid.NewGuid().ToString();
        guid.ValidGuid("id").Should().Be(guid);
    }

    [Fact]
    public void ValidEmail_InvalidEmail_ThrowsArgumentException()
    {
        var act = () => "not-an-email".ValidEmail("email");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ValidEmail_ValidEmail_ReturnsSameString()
    {
        "user@example.com".ValidEmail("email").Should().Be("user@example.com");
    }
}
