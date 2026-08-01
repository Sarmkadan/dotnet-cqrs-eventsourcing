[MemoryDiagnoser]
public class ReflectionUtilitiesBenchmarks
{
    [Benchmark]
    public void Benchmark_GetType()
    {
        // Setup test data
        var types = new[] { typeof(string), typeof(int), typeof(List<string>) };
        // Benchmark code
        for (int i = 0; i < 1000; i++)
        {
            var type = types[i % types.Length];
        }
    }

    [Benchmark]
    public void Benchmark_GetTypes()
    {
        // Setup test data
        var types = new[] { typeof(string), typeof(int), typeof(List<string>) };
        // Benchmark code
        for (int i = 0; i < 1000; i++)
        {
            var type = types[i % types.Length];
            var typesList = new List<string> { "string", "int", "List<string>" };
            foreach (var t in typesList)
            {
                // Do something with t
            }
        }
    }

    [Benchmark]
    public void Benchmark_GetType_Params([Params(10, 100, 1000)] int n)
    {
        // Setup test data
        var types = new[] { typeof(string), typeof(int), typeof(List<string>) };
        // Benchmark code
        for (int i = 0; i < n; i++)
        {
            var type = types[i % types.Length];
        }
    }
}
