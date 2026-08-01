using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnostics.Windows.Configs;
using DotNetCqrsEventSourcing.Infrastructure.Utilities;

namespace DotNetCqrsEventSourcing.Benchmarks;

[MemoryDiagnoser]
public class ReflectionUtilitiesBenchmarks
{
    private Assembly _testAssembly = null!;
    private Type[] _testTypes = null!;
    private Type _iTestInterface = null!;
    private Type _complexType = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Get the assembly containing ReflectionUtilities for testing
        _testAssembly = typeof(ReflectionUtilities).Assembly;

        // Get some test types from the assembly
        _testTypes = _testAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract)
            .Take(50)
            .ToArray();

        // Find an interface type to test with
        _iTestInterface = typeof(IDisposable);

        // Find a complex type with properties and methods for testing
        _complexType = typeof(List<string>);
    }

    [Benchmark]
    public IEnumerable<Type> GetTypesImplementing_Cached()
    {
        return ReflectionUtilities.GetTypesImplementing(_testAssembly, _iTestInterface);
    }

    [Benchmark]
    public IEnumerable<Type> GetTypesImplementing_Uncached()
    {
        ReflectionUtilities.ClearCaches();
        return ReflectionUtilities.GetTypesImplementing(_testAssembly, _iTestInterface);
    }

    [Benchmark]
    public PropertyInfo[] GetPublicProperties_Cached()
    {
        // First call will populate cache
        return ReflectionUtilities.GetPublicProperties(_complexType);
    }

    [Benchmark]
    public PropertyInfo[] GetPublicProperties_Uncached()
    {
        // Clear cache to test uncached performance
        ReflectionUtilities.ClearCaches();
        return ReflectionUtilities.GetPublicProperties(_complexType);
    }

    [Benchmark]
    public MethodInfo? FindMethod_Cached()
    {
        // First call will populate cache
        return ReflectionUtilities.FindMethod(_complexType, nameof(List<string>.Add), 1);
    }

    [Benchmark]
    public MethodInfo? FindMethod_Uncached()
    {
        // Clear cache to test uncached performance
        ReflectionUtilities.ClearCaches();
        return ReflectionUtilities.FindMethod(_complexType, nameof(List<string>.Add), 1);
    }

    [Benchmark]
    public Type[] GetGenericArguments()
    {
        return ReflectionUtilities.GetGenericArguments(typeof(Dictionary<string, List<int>>));
    }

    [Benchmark]
    public object CreateInstance()
    {
        return ReflectionUtilities.CreateInstance(typeof(List<string>));
    }

    [Benchmark]
    public bool IsGenericTypeOf_True()
    {
        return ReflectionUtilities.IsGenericTypeOf(typeof(List<string>), typeof(IEnumerable<>));
    }

    [Benchmark]
    public bool IsGenericTypeOf_False()
    {
        return ReflectionUtilities.IsGenericTypeOf(typeof(string), typeof(IEnumerable<>));
    }

    [Benchmark]
    public Type? GetGenericBaseType()
    {
        return ReflectionUtilities.GetGenericBaseType(typeof(List<string>), typeof(IEnumerable<>));
    }
}