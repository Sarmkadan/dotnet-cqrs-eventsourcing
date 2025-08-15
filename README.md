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