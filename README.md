// existing content ...

## SnapshotCompressionOptions

The `SnapshotCompressionOptions` class configures snapshot compression behavior, including compression level, size thresholds, and incremental snapshot chain limits. It is used when registering the snapshot compression service to customize compression settings.

### Usage Example

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSnapshotCompression(options => 
        {
            options.Level = CompressionLevel.Fastest;
            options.MinimumSizeThreshold = 1024;
            options.MaxIncrementalChainLength = 5;
            options.AutoCompress = true;
        });
    }
}
```

## ValidationDecorator

The `ValidationDecorator<TCommand, TResult>` class provides command validation before execution, ensuring that all command properties are valid and domain invariants are satisfied. It validates that required properties are not null, value types have valid values, and business rules are enforced before allowing command handlers to execute. This decorator prevents invalid state from propagating to the domain layer and event stream.

### Usage Example

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // Register the validation decorator in the DI container
        services.AddTransient(typeof(ICommandHandler<CreateOrderCommand, Result<Order>>), 
                           typeof(ValidationDecorator<CreateOrderCommand, Result<Order>>));

        // The actual handler implementation
        services.AddTransient<ICommandHandler<CreateOrderCommand, Result<Order>>, 
                           CreateOrderCommandHandler>();
    }
}

// Example command handler that would be wrapped by the decorator
public class CreateOrderCommandHandler : ICommandHandler<CreateOrderCommand, Result<Order>>
{
    private readonly IOrderRepository _orderRepository;

    public CreateOrderCommandHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<Result<Order>> Handle(CreateOrderCommand command, CancellationToken cancellationToken)
    {
        // This handler will be automatically wrapped with validation
        var order = new Order(command.CustomerId, command.ProductId, command.Quantity);
        await _orderRepository.AddAsync(order, cancellationToken);

        return Result<Order>.Success(order);
    }
}

// Usage in application code
var command = new CreateOrderCommand
{
    CustomerId = "cust-123",
    ProductId = "prod-456",
    Quantity = 2
};

var result = await commandBus.SendAsync(command);

if (result.IsFailure)
{
    Console.WriteLine($"Failed to create order: {string.Join(", ", result.Errors)}");
}
```

## RequestLog

The `RequestLog` class captures detailed information about HTTP requests for audit trail, debugging, and compliance purposes. It records the complete request context including headers, body content, user information, and timing details. This model is typically used in middleware to log incoming API requests before processing and correlate them with corresponding responses.

### Usage Example

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // Register request logging middleware
        services.AddTransient<RequestResponseLoggingMiddleware>();
    }
}

// Example usage in a controller or middleware
public class OrdersController : ControllerBase
{
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(ILogger<OrdersController> logger)
    {
        _logger = logger;
    }

    [HttpPost("orders")]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        var requestId = Guid.NewGuid().ToString();
        var correlationId = HttpContext.TraceIdentifier;
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        // Create request log
        var requestLog = new RequestLog
        {
            RequestId = requestId,
            CorrelationId = correlationId,
            Timestamp = DateTime.UtcNow,
            Method = HttpContext.Request.Method,
            Path = HttpContext.Request.Path.ToString(),
            QueryString = HttpContext.Request.QueryString.ToString(),
            Headers = HttpContext.Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString()),
            Body = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync(),
            UserId = userId,
            ClientIp = clientIp
        };

        _logger.LogInformation("Incoming request: {RequestId} {Method} {Path}", 
            requestLog.RequestId, requestLog.Method, requestLog.Path);

        // Process request...
        return Ok(new { Message = "Order created" });
    }
}

// Example of creating a request log manually
var requestLog = new RequestLog
{
    RequestId = Guid.NewGuid().ToString(),
    CorrelationId = "corr-12345",
    Timestamp = DateTime.UtcNow,
    Method = "POST",
    Path = "/api/v1/orders",
    QueryString = "?priority=true",
    Headers = new Dictionary<string, string>
    {
        { "Content-Type", "application/json" },
        { "Authorization", "Bearer token123" },
        { "X-Request-Id", "req-123" }
    },
    Body = "{\"customerId\": \"cust-123\", \"productId\": \"prod-456\", \"quantity\": 2}",
    UserId = "user-789",
    ClientIp = "192.168.1.100"
};

```