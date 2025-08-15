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
