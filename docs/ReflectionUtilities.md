# ReflectionUtilities

The `ReflectionUtilities` class provides a centralized set of static helper methods for performing common reflection operations within the `dotnet-cqrs-eventsourcing` project. It abstracts away verbose `System.Reflection` boilerplate, offering type-safe utilities for discovering implementing types, inspecting generic arguments, resolving base types, instantiating objects, and managing reflection caches. This class is designed to streamline metadata inspection tasks required by the event sourcing infrastructure while maintaining performance through internal caching mechanisms.

## API

### `GetTypesImplementing<TInterface>()`
Scans the current application domain to find all concrete types that implement the specified interface `TInterface`.
*   **Parameters**: None (generic type parameter `TInterface` defines the search constraint).
*   **Returns**: `IEnumerable<Type>` containing all found types.
*   **Throws**: Throws if `TInterface` is not an interface type.

### `GetTypesImplementing(Type interfaceType)`
Scans the current application domain to find all concrete types that implement the specified `interfaceType`.
*   **Parameters**: `interfaceType` – The `System.Type` of the interface to search for.
*   **Returns**: `IEnumerable<Type>` containing all found types.
*   **Throws**: `ArgumentException` if `interfaceType` is not an interface; `ArgumentNullException` if `interfaceType` is null.

### `GetPublicProperties(Type type)`
Retrieves all public instance properties declared on or inherited by the specified type.
*   **Parameters**: `type` – The `System.Type` to inspect.
*   **Returns**: `PropertyInfo[]` array of public properties.
*   **Throws**: `ArgumentNullException` if `type` is null.

### `FindMethod(Type type, string methodName, Type[] parameterTypes)`
Locates a specific method on a type by name and parameter signature.
*   **Parameters**: 
    *   `type` – The `System.Type` to search.
    *   `methodName` – The name of the method.
    *   `parameterTypes` – An array of `Type` objects representing the method's parameter signature.
*   **Returns**: `MethodInfo?` containing the method information if found; otherwise, `null`.
*   **Throws**: `ArgumentNullException` if `type` or `methodName` is null.

### `GetGenericArguments(Type type)`
Extracts the generic type arguments from a generic type definition or a constructed generic type.
*   **Parameters**: `type` – The `System.Type` to analyze.
*   **Returns**: `Type[]` array of generic arguments.
*   **Throws**: `InvalidOperationException` if the type is not generic; `ArgumentNullException` if `type` is null.

### `IsGenericTypeOf(Type type, Type genericTypeDefinition)`
Determines whether a given type is a constructed generic type based on the specified generic type definition.
*   **Parameters**: 
    *   `type` – The type to check.
    *   `genericTypeDefinition` – The open generic type definition (e.g., `typeof(List<>)`).
*   **Returns**: `bool` indicating `true` if `type` is a generic instance of `genericTypeDefinition`; otherwise `false`.
*   **Throws**: `ArgumentNullException` if either argument is null; `ArgumentException` if `genericTypeDefinition` is not a generic type definition.

### `GetCustomAttributes<TAttribute>(Type type)`
Retrieves all custom attributes of type `TAttribute` applied to the specified type.
*   **Parameters**: `type` – The `System.Type` to inspect.
*   **Returns**: `IEnumerable<TAttribute>` containing the found attributes.
*   **Throws**: `ArgumentNullException` if `type` is null.

### `CreateInstance(Type type, params object[] args)`
Creates an instance of the specified type using the best-matching constructor for the provided arguments.
*   **Parameters**: 
    *   `type` – The `System.Type` to instantiate.
    *   `args` – Optional array of arguments to pass to the constructor.
*   **Returns**: `object` representing the new instance.
*   **Throws**: `ArgumentNullException` if `type` is null; `MissingMethodException` if no suitable constructor is found; `TargetInvocationException` if the constructor throws an exception.

### `GetGenericBaseType(Type type, Type genericTypeDefinition)`
Resolves the base type of a class that inherits from a specific generic base class, returning the constructed generic type.
*   **Parameters**: 
    *   `type` – The derived `System.Type`.
    *   `genericTypeDefinition` – The open generic base type definition.
*   **Returns**: `Type?` representing the constructed generic base type if found; otherwise `null`.
*   **Throws**: `ArgumentNullException` if `type` is null.

### `ClearCaches()`
Invalidates internal caches used by reflection methods to optimize performance. This forces subsequent reflection calls to re-scan assemblies.
*   **Parameters**: None.
*   **Returns**: `void`.
*   **Throws**: None.

## Usage

### Discovering Event Handlers
The following example demonstrates how to locate all classes implementing a specific handler interface within the assembly, a common pattern in CQRS architectures for registering handlers automatically.

```csharp
using System;
using System.Collections.Generic;
using System.Linq;

// Assume IEventHandler<T> is defined elsewhere in the project
public interface IEventHandler<T> { }

public class OrderCreatedHandler : IEventHandler<OrderCreated> { }
public class UserRegisteredHandler : IEventHandler<UserRegistered> { }

public class HandlerRegistry
{
    public void RegisterHandlers()
    {
        // Find all types implementing IEventHandler<> regardless of the generic argument
        var handlerTypes = ReflectionUtilities.GetTypesImplementing(typeof(IEventHandler<>));

        foreach (var type in handlerTypes)
        {
            var instance = ReflectionUtilities.CreateInstance(type);
            // Register instance with the mediator or event bus
            Console.WriteLine($"Registered: {type.Name}");
        }
    }
}
```

### Inspecting Generic Aggregates
This example illustrates resolving the generic arguments of an aggregate root to determine the event types it handles, utilizing cache management for dynamic scenarios.

```csharp
using System;
using System.Reflection;

public class AggregateRoot<TId> { }
public class OrderAggregate : AggregateRoot<Guid> { }

public class TypeInspector
{
    public void InspectAggregate()
    {
        var aggregateType = typeof(OrderAggregate);
        var baseDefinition = typeof(AggregateRoot<>);

        // Resolve the constructed base type (e.g., AggregateRoot<Guid>)
        var constructedBase = ReflectionUtilities.GetGenericBaseType(aggregateType, baseDefinition);

        if (constructedBase != null)
        {
            var idType = ReflectionUtilities.GetGenericArguments(constructedBase)[0];
            Console.WriteLine($"Aggregate ID Type: {idType.Name}");
        }

        // If assemblies were dynamically reloaded, clear caches before next inspection
        ReflectionUtilities.ClearCaches();
    }
}
```

## Notes

*   **Caching Behavior**: Methods involving assembly scanning (such as `GetTypesImplementing`) utilize internal caching to minimize performance overhead. In environments where assemblies are dynamically loaded or unloaded at runtime, `ClearCaches()` must be invoked to ensure reflection results reflect the current application state.
*   **Thread Safety**: The read-only reflection methods are thread-safe for concurrent execution. However, `ClearCaches()` is not atomic relative to ongoing read operations; it should be called during a quiescent period or within a locking mechanism if called while other threads are actively performing type discovery.
*   **Null Handling**: Most methods explicitly throw `ArgumentNullException` for null inputs rather than returning null, ensuring fail-fast behavior during configuration phases. The only methods returning `null` by design are `FindMethod` and `GetGenericBaseType` when a match is not found.
*   **Generic Constraints**: `IsGenericTypeOf` and `GetGenericBaseType` require the `genericTypeDefinition` parameter to be an open generic type (e.g., `typeof(List<>)`). Passing a closed generic type (e.g., `typeof(List<int>)`) to these methods will result in an `ArgumentException`.
