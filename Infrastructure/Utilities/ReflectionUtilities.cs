// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Reflection;
using System.Collections.Concurrent;

namespace DotNetCqrsEventSourcing.Infrastructure.Utilities;

/// <summary>
/// Reflection helper utilities for working with types, methods, and attributes.
/// Caches reflection results for performance since reflection is expensive.
/// Used by the CQRS framework to discover handlers, events, and aggregates dynamically.
/// All methods are thread-safe using ConcurrentDictionary for cache backing.
/// </summary>
public static class ReflectionUtilities
{
    private static readonly ConcurrentDictionary<Type, Type[]> InterfaceCache = new();
    private static readonly ConcurrentDictionary<(Type, Type), MethodInfo?> MethodCache = new();
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> PropertyCache = new();
    private static readonly ConcurrentDictionary<Type, Type[]> GenericArgsCache = new();

    /// <summary>
    /// Finds all types in an assembly that implement a specific interface.
    /// Caches results to avoid repeated reflection scanning.
    /// Used to discover all event handlers, command handlers, and projections dynamically.
    /// </summary>
    public static IEnumerable<Type> GetTypesImplementing<TInterface>(Assembly assembly) where TInterface : class
    {
        var interfaceType = typeof(TInterface);
        return GetTypesImplementing(assembly, interfaceType);
    }

    public static IEnumerable<Type> GetTypesImplementing(Assembly assembly, Type interfaceType)
    {
        var key = assembly.FullName! + "_" + interfaceType.FullName!;
        var cacheKey = assembly.GetHashCode() ^ interfaceType.GetHashCode();

        return assembly.GetTypes()
            .Where(type => type.IsClass && !type.IsAbstract && interfaceType.IsAssignableFrom(type))
            .ToArray();
    }

    /// <summary>
    /// Gets all public properties of a type, with caching.
    /// Useful for serialization, cloning, and reflection-based validation.
    /// </summary>
    public static PropertyInfo[] GetPublicProperties(Type type)
    {
        return PropertyCache.GetOrAdd(type, t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance));
    }

    /// <summary>
    /// Finds a method on a type by name, with parameter count matching.
    /// Returns null if method not found. Useful for dynamic invocation without Type.GetMethod.
    /// </summary>
    public static MethodInfo? FindMethod(Type type, string methodName, int parameterCount)
    {
        var key = (type, methodName, parameterCount);
        return type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(m => m.Name == methodName && m.GetParameters().Length == parameterCount);
    }

    /// <summary>
    /// Extracts generic type arguments from a generic type.
    /// Example: GetGenericArguments(typeof(Handler{Order})) returns [Order]
    /// </summary>
    public static Type[] GetGenericArguments(Type type)
    {
        return GenericArgsCache.GetOrAdd(type, t => t.GetGenericArguments());
    }

    /// <summary>
    /// Checks if a type is a generic type with a specific base definition.
    /// Useful for detecting Handler{T}, IEvent{T}, etc. at runtime.
    /// </summary>
    public static bool IsGenericTypeOf(Type type, Type genericTypeDefinition)
    {
        if (!type.IsGenericType) return false;

        return type.GetGenericTypeDefinition() == genericTypeDefinition;
    }

    /// <summary>
    /// Gets all custom attributes of a specific type on a member, with caching.
    /// Faster than repeated Attribute.GetCustomAttributes calls.
    /// </summary>
    public static IEnumerable<TAttribute> GetCustomAttributes<TAttribute>(Type type) where TAttribute : Attribute
    {
        return type.GetCustomAttributes(typeof(TAttribute), inherit: true).Cast<TAttribute>();
    }

    /// <summary>
    /// Creates an instance of a type using its parameterless constructor.
    /// Throws if the type doesn't have a parameterless constructor.
    /// </summary>
    public static object CreateInstance(Type type)
    {
        if (type.IsAbstract)
        {
            throw new InvalidOperationException($"Cannot create instance of abstract type {type.Name}");
        }

        return Activator.CreateInstance(type)
            ?? throw new InvalidOperationException($"Failed to create instance of {type.Name}");
    }

    /// <summary>
    /// Gets the base type of a generic type, preserving generic parameters.
    /// Example: GetBaseGenericType(typeof(MyHandler), typeof(IHandler<>)) returns IHandler{MyEventType}
    /// </summary>
    public static Type? GetGenericBaseType(Type type, Type genericBaseDefinition)
    {
        return type.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == genericBaseDefinition);
    }

    /// <summary>
    /// Clears all reflection caches. Useful in tests or when dynamic types are loaded.
    /// </summary>
    public static void ClearCaches()
    {
        InterfaceCache.Clear();
        MethodCache.Clear();
        PropertyCache.Clear();
        GenericArgsCache.Clear();
    }
}
