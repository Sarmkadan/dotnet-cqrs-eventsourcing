#nullable enable
using DotNetCqrsEventSourcing.Domain.Events;
using DotNetCqrsEventSourcing.Infrastructure.Events;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DotNetCqrsEventSourcing.Tests;

/// <summary>
/// Contains unit tests for the <see cref="EventTypeRegistry"/> class.
/// </summary>
public class EventTypeRegistryTests
{
    private readonly EventTypeRegistry _registry;
    private readonly ILogger<EventTypeRegistry> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventTypeRegistryTests"/> class.
    /// </summary>
    public EventTypeRegistryTests()
    {
        _logger = NullLogger<EventTypeRegistry>.Instance;
        _registry = new EventTypeRegistry(_logger);
    }

    /// <summary>
    /// Verifies that registering a type maps it to the specified event name and can be resolved correctly.
    /// </summary>
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

    /// <summary>
    /// Verifies that resolving an unknown event name throws an <see cref="UnknownEventTypeException"/>.
    /// </summary>
    [Fact]
    public void Resolve_ShouldThrowUnknownEventTypeException_WhenEventNameIsUnknown()
    {
        // Arrange
        const string unknownEventName = "NonExistentEvent";

        // Act & Assert
        var act = () => _registry.Resolve(unknownEventName);
        act.Should().Throw<UnknownEventTypeException>()
            .Where(ex => ex.EventTypeName == unknownEventName);
    }

    /// <summary>
    /// Verifies that resolving a null event name throws an <see cref="UnknownEventTypeException"/> with a message indicating the name cannot be null or empty.
    /// </summary>
    [Fact]
    public void Resolve_ShouldThrowUnknownEventTypeException_WhenEventNameIsNull()
    {
        // Act & Assert
        var act = () => _registry.Resolve(null!);
        act.Should().Throw<UnknownEventTypeException>()
            .WithMessage("*cannot be null or empty*");
    }

    /// <summary>
    /// Verifies that resolving an empty event name throws an <see cref="UnknownEventTypeException"/> with a message indicating the name cannot be null or empty.
    /// </summary>
    [Fact]
    public void Resolve_ShouldThrowUnknownEventTypeException_WhenEventNameIsEmpty()
    {
        // Act & Assert
        var act = () => _registry.Resolve(string.Empty);
        act.Should().Throw<UnknownEventTypeException>()
            .WithMessage("*cannot be null or empty*");
    }

    /// <summary>
    /// Verifies that resolving a whitespace-only event name throws an <see cref="UnknownEventTypeException"/> with the event name set to the whitespace string.
    /// </summary>
    [Fact]
    public void Resolve_ShouldThrowUnknownEventTypeException_WhenEventNameIsWhitespace()
    {
        // Act & Assert
        var act = () => _registry.Resolve(" ");
        act.Should().Throw<UnknownEventTypeException>()
            .Where(ex => ex.EventTypeName == " ");
    }

    /// <summary>
    /// Verifies that trying to resolve an unknown event name returns false and a null output type.
    /// </summary>
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

    /// <summary>
    /// Verifies that trying to resolve a known event name returns true and the correct registered type.
    /// </summary>
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

    /// <summary>
    /// Verifies that registering a duplicate event name throws an <see cref="InvalidOperationException"/> with a descriptive message.
    /// </summary>
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

    /// <summary>
    /// Verifies that registering a null event name throws an <see cref="ArgumentException"/> with a message indicating the name must not be null or whitespace.
    /// </summary>
    [Fact]
    public void Register_ShouldThrowArgumentException_WhenEventNameIsNull()
    {
        // Act & Assert
        var act = () => _registry.Register<TestEvent>(null!);
        act.Should().Throw<ArgumentException>()
            .WithMessage("Event name must not be null or whitespace. (Parameter 'eventName')");
    }

    /// <summary>
    /// Verifies that registering an empty event name throws an <see cref="ArgumentException"/> with a message indicating the name must not be null or whitespace.
    /// </summary>
    [Fact]
    public void Register_ShouldThrowArgumentException_WhenEventNameIsEmpty()
    {
        // Act & Assert
        var act = () => _registry.Register<TestEvent>(string.Empty);
        act.Should().Throw<ArgumentException>()
            .WithMessage("Event name must not be null or whitespace. (Parameter 'eventName')");
    }

    /// <summary>
    /// Verifies that registering a whitespace-only event name throws an <see cref="ArgumentException"/> with a message indicating the name must not be null or whitespace.
    /// </summary>
    [Fact]
    public void Register_ShouldThrowArgumentException_WhenEventNameIsWhitespace()
    {
        // Act & Assert
        var act = () => _registry.Register<TestEvent>(" ");
        act.Should().Throw<ArgumentException>()
            .WithMessage("Event name must not be null or whitespace. (Parameter 'eventName')");
    }

    /// <summary>
    /// Verifies that scanning an assembly registers types decorated with the <see cref="EventNameAttribute"/>.
    /// </summary>
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

    /// <summary>
    /// Verifies that scanning an assembly does not register types that lack the <see cref="EventNameAttribute"/>.
    /// </summary>
    [Fact]
    public void ScanAssembly_ShouldNotRegisterTypesWithoutEventNameAttribute()
    {
        // Arrange
        var assembly = typeof(TestEvent).Assembly;

        // Act
        _registry.ScanAssembly(assembly);

        // Assert - TestEventWithoutAttribute should not be registered
        var act = () => _registry.Resolve("TestEventWithoutAttribute");
        act.Should().Throw<UnknownEventTypeException>();
    }

    /// <summary>
    /// Verifies that getting all registrations returns a dictionary containing all registered event name to type mappings.
    /// </summary>
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

    /// <summary>
    /// Verifies that getting all registrations returns an empty dictionary when no registrations have been made.
    /// </summary>
    [Fact]
    public void GetAllRegistrations_ShouldReturnEmptyDictionary_WhenNoRegistrations()
    {
        // Act
        var registrations = _registry.GetAllRegistrations();

        // Assert
        registrations.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that scanning an assembly with no attributed types does not throw an exception.
    /// </summary>
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

    /// <summary>
    /// Verifies that scanning a null assembly throws an <see cref="ArgumentNullException"/>.
    /// </summary>
    [Fact]
    public void ScanAssembly_ShouldThrowArgumentNullException_WhenAssemblyIsNull()
    {
        // Act & Assert
        var act = () => _registry.ScanAssembly(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    /// Verifies that event name resolution is case-sensitive.
    /// </summary>
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
        var act = () => _registry.Resolve("testevent");
        act.Should().Throw<UnknownEventTypeException>();

        act = () => _registry.Resolve("TESTEVENT");
        act.Should().Throw<UnknownEventTypeException>();

        act = () => _registry.Resolve("Testevent");
        act.Should().Throw<UnknownEventTypeException>();
    }

    /// <summary>
    /// Verifies that the same event type can be registered under different names.
    /// </summary>
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

    /// <summary>
    /// Verifies that resolving a malicious event name (containing assembly-qualified type syntax) throws an <see cref="UnknownEventTypeException"/> with a descriptive message.
    /// </summary>
    [Fact]
    public void Resolve_ShouldThrowUnknownEventTypeException_WithDescriptiveMessage()
    {
        // Arrange
        const string maliciousTypeName = "System.Diagnostics.Process, System.Diagnostics.Process";

        // Act & Assert
        var act = () => _registry.Resolve(maliciousTypeName);
        var exception = act.Should().Throw<UnknownEventTypeException>().Which;

        exception.EventTypeName.Should().Be(maliciousTypeName);
        exception.Message.Should().Contain(maliciousTypeName);
        exception.Message.Should().Contain("Only explicitly registered event types can be deserialized");
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