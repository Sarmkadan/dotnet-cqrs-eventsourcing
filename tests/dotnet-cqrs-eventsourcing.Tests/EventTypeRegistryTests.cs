#nullable enable

using DotNetCqrsEventSourcing.Domain.Events;
using DotNetCqrsEventSourcing.Infrastructure.Events;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DotNetCqrsEventSourcing.Tests;

public class EventTypeRegistryTests
{
    private readonly EventTypeRegistry _registry;
    private readonly ILogger<EventTypeRegistry> _logger;

    public EventTypeRegistryTests()
    {
        _logger = NullLogger<EventTypeRegistry>.Instance;
        _registry = new EventTypeRegistry(_logger);
    }

    [Fact]
    public void Register_ShouldMapTypeToEventName()
    {
        // Arrange
        const string eventName = "TestEvent";

        // Act
        _registry.Register<TestEvent>(eventName);

        // Assert
        var resolvedType = _registry.Resolve(eventName);
        resolvedType.Should().NotBeNull();
        resolvedType.Should().Be(typeof(TestEvent));
    }

    [Fact]
    public void Resolve_ShouldReturnNull_WhenEventNameIsUnknown()
    {
        // Arrange
        const string unknownEventName = "NonExistentEvent";

        // Act
        var resolvedType = _registry.Resolve(unknownEventName);

        // Assert
        resolvedType.Should().BeNull();
    }

    [Fact]
    public void Resolve_ShouldReturnNull_WhenEventNameIsNull()
    {
        // Act
        var resolvedType = _registry.Resolve(null!);

        // Assert
        resolvedType.Should().BeNull();
    }

    [Fact]
    public void Resolve_ShouldReturnNull_WhenEventNameIsEmpty()
    {
        // Act
        var resolvedType = _registry.Resolve(string.Empty);

        // Assert
        resolvedType.Should().BeNull();
    }

    [Fact]
    public void Resolve_ShouldReturnNull_WhenEventNameIsWhitespace()
    {
        // Act
        var resolvedType = _registry.Resolve("   ");

        // Assert
        resolvedType.Should().BeNull();
    }

    [Fact]
    public void TryResolve_ShouldReturnFalse_WhenEventNameIsUnknown()
    {
        // Arrange
        const string unknownEventName = "NonExistentEvent";

        // Act
        var result = _registry.TryResolve(unknownEventName, out var resolvedType);

        // Assert
        result.Should().BeFalse();
        resolvedType.Should().BeNull();
    }

    [Fact]
    public void TryResolve_ShouldReturnTrueAndType_WhenEventNameIsKnown()
    {
        // Arrange
        const string eventName = "TestEvent";
        _registry.Register<TestEvent>(eventName);

        // Act
        var result = _registry.TryResolve(eventName, out var resolvedType);

        // Assert
        result.Should().BeTrue();
        resolvedType.Should().NotBeNull();
        resolvedType.Should().Be(typeof(TestEvent));
    }

    [Fact]
    public void Register_ShouldThrowInvalidOperationException_WhenDuplicateEventName()
    {
        // Arrange
        const string eventName = "DuplicateEvent";
        _registry.Register<TestEvent>(eventName);

        // Act & Assert
        var act = () => _registry.Register<AnotherTestEvent>(eventName);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"Event name '{eventName}' is already registered to '{typeof(TestEvent).FullName}'. Cannot re-register it to '{typeof(AnotherTestEvent).FullName}'.");
    }

    [Fact]
    public void Register_ShouldThrowArgumentException_WhenEventNameIsNull()
    {
        // Act & Assert
        var act = () => _registry.Register<TestEvent>(null!);
        act.Should().Throw<ArgumentException>()
            .WithMessage("Event name must not be null or whitespace. (Parameter 'eventName')");
    }

    [Fact]
    public void Register_ShouldThrowArgumentException_WhenEventNameIsEmpty()
    {
        // Act & Assert
        var act = () => _registry.Register<TestEvent>(string.Empty);
        act.Should().Throw<ArgumentException>()
            .WithMessage("Event name must not be null or whitespace. (Parameter 'eventName')");
    }

    [Fact]
    public void Register_ShouldThrowArgumentException_WhenEventNameIsWhitespace()
    {
        // Act & Assert
        var act = () => _registry.Register<TestEvent>("   ");
        act.Should().Throw<ArgumentException>()
            .WithMessage("Event name must not be null or whitespace. (Parameter 'eventName')");
    }

    [Fact]
    public void ScanAssembly_ShouldRegisterTypesWithEventNameAttribute()
    {
        // Arrange
        var assembly = typeof(TestEvent).Assembly;

        // Act
        _registry.ScanAssembly(assembly);

        // Assert
        var resolvedType = _registry.Resolve("TestEvent");
        resolvedType.Should().NotBeNull();
        resolvedType.Should().Be(typeof(TestEvent));
    }

    [Fact]
    public void ScanAssembly_ShouldNotRegisterTypesWithoutEventNameAttribute()
    {
        // Arrange
        var assembly = typeof(TestEvent).Assembly;

        // Act
        _registry.ScanAssembly(assembly);

        // Assert - TestEventWithoutAttribute should not be registered
        var resolvedType = _registry.Resolve("TestEventWithoutAttribute");
        resolvedType.Should().BeNull();
    }

    [Fact]
    public void GetAllRegistrations_ShouldReturnAllRegisteredMappings()
    {
        // Arrange
        _registry.Register<TestEvent>("TestEvent");
        _registry.Register<AnotherTestEvent>("AnotherTestEvent");

        // Act
        var registrations = _registry.GetAllRegistrations();

        // Assert
        registrations.Should().HaveCount(2);
        registrations.Should().ContainKey("TestEvent");
        registrations.Should().ContainKey("AnotherTestEvent");
        registrations["TestEvent"].Should().Be(typeof(TestEvent));
        registrations["AnotherTestEvent"].Should().Be(typeof(AnotherTestEvent));
    }

    [Fact]
    public void GetAllRegistrations_ShouldReturnEmptyDictionary_WhenNoRegistrations()
    {
        // Act
        var registrations = _registry.GetAllRegistrations();

        // Assert
        registrations.Should().BeEmpty();
    }

    [Fact]
    public void ScanAssembly_ShouldHandleEmptyAssembly()
    {
        // Arrange
        var emptyAssembly = typeof(object).Assembly;

        // Act - Should not throw
        var act = () => _registry.ScanAssembly(emptyAssembly);
        act.Should().NotThrow();

        // Assert
        var registrations = _registry.GetAllRegistrations();
        registrations.Should().BeEmpty();
    }

    [Fact]
    public void ScanAssembly_ShouldThrowArgumentNullException_WhenAssemblyIsNull()
    {
        // Act & Assert
        var act = () => _registry.ScanAssembly(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Resolve_ShouldBeCaseSensitive()
    {
        // Arrange
        const string eventName = "TestEvent";
        _registry.Register<TestEvent>(eventName);

        // Act & Assert - exact case should work
        var resolvedType = _registry.Resolve("TestEvent");
        resolvedType.Should().NotBeNull();

        // Act & Assert - different case should not work
        resolvedType = _registry.Resolve("testevent");
        resolvedType.Should().BeNull();

        resolvedType = _registry.Resolve("TESTEVENT");
        resolvedType.Should().BeNull();

        resolvedType = _registry.Resolve("Testevent");
        resolvedType.Should().BeNull();
    }

    [Fact]
    public void Register_ShouldAllowSameTypeWithDifferentNames()
    {
        // Arrange
        const string eventName1 = "FirstName";
        const string eventName2 = "SecondName";

        // Act
        _registry.Register<TestEvent>(eventName1);
        _registry.Register<TestEvent>(eventName2);

        // Assert
        var resolvedType1 = _registry.Resolve(eventName1);
        var resolvedType2 = _registry.Resolve(eventName2);

        resolvedType1.Should().Be(typeof(TestEvent));
        resolvedType2.Should().Be(typeof(TestEvent));
    }

    // Test event classes
    [EventName("TestEvent")]
    private class TestEvent : DomainEvent
    {
        public override string GetEventType() => "TestEvent";
    }

    [EventName("AnotherTestEvent")]
    private class AnotherTestEvent : DomainEvent
    {
        public override string GetEventType() => "AnotherTestEvent";
    }

    private class TestEventWithoutAttribute : DomainEvent
    {
        public override string GetEventType() => "TestEventWithoutAttribute";
    }
}