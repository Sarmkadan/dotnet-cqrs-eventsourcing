# IHttpClientFactory

`IHttpClientFactory` provides a centralized mechanism for managing, configuring, and creating `HttpClient` instances within the `dotnet-cqrs-eventsourcing` project. It promotes efficient resource management by orchestrating the lifetimes of underlying `HttpMessageHandler` objects, mitigating socket exhaustion issues, and facilitating the application of standardized resilience policies to outbound HTTP traffic.

## API

### StandardHttpClientFactory
The primary implementation class responsible for the instantiation and lifecycle management of `HttpClient` instances according to configured application settings.

### HttpClient CreateClient()
Creates and returns a new `HttpClient` instance configured with default settings and handlers.

*   **Returns:** A configured `HttpClient` instance.

### HttpClient CreateClientWithBaseAddress(string baseAddress)
Creates and returns a `HttpClient` instance pre-configured with the specified base URL.

*   **Parameters:** 
    *   `baseAddress`: The base URI for requests made by the returned client.
*   **Returns:** A configured `HttpClient` instance.

### HttpClient CreateAuthenticatedClient()
Creates and returns a `HttpClient` instance pre-configured with required authentication headers, typically retrieved from the current security context or configuration.

*   **Returns:** A configured `HttpClient` instance.
*   **Throws:** `InvalidOperationException` if the necessary authentication context is unavailable or improperly configured.

### static IServiceCollection AddStandardHttpClients(IServiceCollection services)
An extension method to register the `IHttpClientFactory` implementation and required dependencies into the dependency injection container.

*   **Parameters:**
    *   `services`: The `IServiceCollection` to register the services into.
*   **Returns:** The `IServiceCollection` for further chaining.

### static IServiceCollection AddHttpClientResilience(IServiceCollection services)
An extension method that configures standardized resilience, timeout, and retry policies for all `HttpClient` instances managed by the factory.

*   **Parameters:**
    *   `services`: The `IServiceCollection` to configure.
*   **Returns:** The `IServiceCollection` for further chaining.

## Usage

### Registering Services in Dependency Injection
```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddStandardHttpClients();
    services.AddHttpClientResilience();
}
```

### Consuming the Factory in a Service
```csharp
public class ExternalApiService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public ExternalApiService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<string> GetDataAsync(string endpoint)
    {
        var client = _httpClientFactory.CreateClientWithBaseAddress("https://api.example.com");
        var response = await client.GetAsync(endpoint);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }
}
```

## Notes

*   **Thread Safety:** `HttpClient` instances created by the factory are intended for reuse and are thread-safe regarding the `SendAsync` method.
*   **Handler Management:** The factory manages the lifetime of the underlying `HttpMessageHandler` objects. Avoid disposing of `HttpClient` instances directly, as this does not dispose of the underlying handler and may lead to socket exhaustion if handled incorrectly outside the factory.
*   **Resilience Configuration:** The policies applied via `AddHttpClientResilience` are applied globally to clients created by the factory. Ensure that custom timeout configurations on individual clients do not conflict with the resilience policies registered at the container level.
