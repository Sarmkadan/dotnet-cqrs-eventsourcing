# Deployment Guide

Production deployment strategies for CQRS + Event Sourcing applications.

## Local Development

### Prerequisites

- .NET 10 SDK
- SQL Server, PostgreSQL, or MongoDB (for production event store)
- Optional: Docker Desktop

### Development Setup

```bash
git clone https://github.com/sarmkadan/dotnet-cqrs-eventsourcing.git
cd dotnet-cqrs-eventsourcing
dotnet restore
dotnet build
dotnet run
```

### Running with In-Memory Store

Default configuration uses in-memory event store (suitable for development only):

```csharp
var services = new ServiceCollection();
services.AddCqrsFramework(); // Uses InMemoryEventRepository by default
var serviceProvider = services.BuildServiceProvider();
```

## Docker Deployment

### Building Docker Image

Create `Dockerfile`:

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS builder
WORKDIR /src

COPY . .
RUN dotnet restore
RUN dotnet build -c Release

FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app
COPY --from=builder /src/bin/Release/net10.0/publish .

EXPOSE 8080
ENTRYPOINT ["./DotNetCqrsEventSourcing"]
```

Build and run:

```bash
docker build -t dotnet-cqrs:1.0 .
docker run -p 8080:8080 dotnet-cqrs:1.0
```

### Docker Compose Setup

```yaml
version: '3.9'

services:
  api:
    build:
      context: .
      dockerfile: Dockerfile
    ports:
      - "8080:8080"
    environment:
      - EventStoreConnection=Server=sqlserver:1433;Database=CqrsEventStore
      - RedisConnection=redis:6379
    depends_on:
      - sqlserver
      - redis
    networks:
      - cqrs-network

  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      - SA_PASSWORD=YourPassword123!
      - ACCEPT_EULA=Y
    ports:
      - "1433:1433"
    volumes:
      - sqldata:/var/opt/mssql
    networks:
      - cqrs-network

  redis:
    image: redis:7-alpine
    ports:
      - "6379:6379"
    volumes:
      - redisdata:/data
    networks:
      - cqrs-network

volumes:
  sqldata:
  redisdata:

networks:
  cqrs-network:
    driver: bridge
```

Start services:

```bash
docker-compose up -d
```

## Cloud Deployment

### Azure App Service

1. Create App Service plan:
```bash
az appservice plan create \
  --name CqrsAppPlan \
  --resource-group mygroup \
  --sku B2
```

2. Create web app:
```bash
az webapp create \
  --resource-group mygroup \
  --plan CqrsAppPlan \
  --name my-cqrs-app
```

3. Configure SQL Database:
```bash
az sql server create \
  --resource-group mygroup \
  --name my-sql-server \
  --admin-user sqladmin \
  --admin-password SecurePassword123!

az sql db create \
  --resource-group mygroup \
  --server my-sql-server \
  --name CqrsEventStore
```

4. Publish application:
```bash
dotnet publish -c Release -o publish
az webapp deployment source config-zip \
  --resource-group mygroup \
  --name my-cqrs-app \
  --src-path ./publish.zip
```

### AWS Deployment

1. Create EC2 instance:
```bash
aws ec2 run-instances \
  --image-id ami-0c55b159cbfafe1f0 \
  --instance-type t3.medium \
  --key-name my-key-pair
```

2. Install .NET 10:
```bash
sudo wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
sudo chmod +x dotnet-install.sh
sudo ./dotnet-install.sh --version latest
```

3. Deploy application:
```bash
git clone https://github.com/sarmkadan/dotnet-cqrs-eventsourcing.git
cd dotnet-cqrs-eventsourcing
dotnet build -c Release
dotnet run -c Release
```

### Google Cloud Run

1. Containerize:
```bash
docker build -t gcr.io/my-project/dotnet-cqrs:latest .
docker push gcr.io/my-project/dotnet-cqrs:latest
```

2. Deploy to Cloud Run:
```bash
gcloud run deploy dotnet-cqrs \
  --image gcr.io/my-project/dotnet-cqrs:latest \
  --platform managed \
  --region us-central1 \
  --allow-unauthenticated
```

## Database Configuration

### SQL Server Event Store

```csharp
services.AddSingleton<IEventRepository>(sp =>
{
    var connectionString = "Server=.;Database=CqrsEventStore";
    return new SqlServerEventRepository(connectionString);
});
```

Create tables:

```sql
CREATE TABLE Events (
    AggregateId NVARCHAR(100) NOT NULL,
    Version INT NOT NULL,
    EventType NVARCHAR(100) NOT NULL,
    Data NVARCHAR(MAX) NOT NULL,
    Timestamp DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    PRIMARY KEY (AggregateId, Version)
);

CREATE INDEX IX_Events_Timestamp ON Events(Timestamp);
CREATE INDEX IX_Events_EventType ON Events(EventType);
```

### PostgreSQL Event Store

```csharp
services.AddSingleton<IEventRepository>(sp =>
{
    var connectionString = "Host=localhost;Database=cqrs_event_store;User Id=postgres";
    return new PostgresEventRepository(connectionString);
});
```

Create tables:

```sql
CREATE TABLE events (
    aggregate_id VARCHAR(100) NOT NULL,
    version INT NOT NULL,
    event_type VARCHAR(100) NOT NULL,
    data JSONB NOT NULL,
    timestamp TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (aggregate_id, version)
);

CREATE INDEX idx_events_timestamp ON events(timestamp);
CREATE INDEX idx_events_event_type ON events(event_type);
```

### MongoDB Event Store

```csharp
services.AddSingleton<IEventRepository>(sp =>
{
    var connectionString = "mongodb://localhost:27017";
    return new MongoEventRepository(connectionString, "cqrs_db");
});
```

Create collection:

```javascript
db.createCollection("events");
db.events.createIndex({ aggregateId: 1, version: 1 }, { unique: true });
db.events.createIndex({ timestamp: 1 });
db.events.createIndex({ eventType: 1 });
```

## Caching Strategy

### Redis Configuration

```csharp
services.AddSingleton<ICacheService>(sp =>
{
    var connectionString = Environment.GetEnvironmentVariable("RedisConnection") 
        ?? "localhost:6379";
    return new RedisCacheService(connectionString);
});
```

Cache invalidation:

```csharp
public class ProjectionCacheInvalidator : IEventHandler<DomainEvent>
{
    public async Task HandleAsync(DomainEvent @event)
    {
        var cacheKey = $"projection:{@event.AggregateId}";
        await _cache.InvalidateAsync(cacheKey);
    }
}
```

## Monitoring & Observability

### Application Insights

```csharp
services.AddApplicationInsightsTelemetry(configuration);

services.AddLogging(builder =>
{
    builder.AddApplicationInsights(
        context => context.Properties.Add("Application", "CqrsEventSourcing")
    );
});
```

### Structured Logging

```csharp
using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole();
    builder.AddDebug();
});

var logger = loggerFactory.CreateLogger<AccountService>();
logger.LogInformation("Account created: {AccountId}", accountId);
logger.LogError("Withdrawal failed: {Reason}", reason);
```

### Health Checks

```csharp
services.AddHealthChecks()
    .AddCheck("EventStore", async () =>
    {
        var eventStore = services.GetRequiredService<IEventStore>();
        try
        {
            await eventStore.GetAllEventsAsync();
            return HealthCheckResult.Healthy();
        }
        catch
        {
            return HealthCheckResult.Unhealthy();
        }
    });
```

## Performance Optimization

### Snapshot Strategy for Production

```csharp
const int snapshotInterval = 100;

if (account.Version % snapshotInterval == 0)
{
    await snapshotService.CreateSnapshotAsync(
        account,
        account.Id,
        account.Version
    );
}
```

### Event Store Indexing

SQL Server:
```sql
CREATE NONCLUSTERED INDEX IX_Events_AggregateId_Version 
ON Events(AggregateId, Version);

CREATE NONCLUSTERED INDEX IX_Events_EventType_Timestamp 
ON Events(EventType, Timestamp);
```

PostgreSQL:
```sql
CREATE INDEX idx_events_aggregate_version ON events(aggregate_id, version);
CREATE INDEX idx_events_type_timestamp ON events(event_type, timestamp);
```

### Connection Pooling

```csharp
var connectionString = new SqlConnectionStringBuilder
{
    DataSource = "server.database.windows.net",
    InitialCatalog = "CqrsDb",
    UserID = "username",
    Password = "password",
    Encrypt = true,
    TrustServerCertificate = false,
    Connection Timeout = 30,
    Pooling = true,
    Max Pool Size = 100,
    Min Pool Size = 5
}.ConnectionString;
```

## Backup & Recovery

### Event Store Backup

```bash
# SQL Server
sqlcmd -S myserver -d CqrsEventStore -Q \
  "BACKUP DATABASE [CqrsEventStore] TO DISK = 'C:\backup\cqrs.bak'"

# PostgreSQL
pg_dump -h localhost -U postgres cqrs_event_store > backup.sql

# MongoDB
mongodump --uri="mongodb://localhost:27017" -d cqrs_db -o backup/
```

### Recovery Procedures

From backup:
```bash
# SQL Server
RESTORE DATABASE [CqrsEventStore] FROM DISK = 'C:\backup\cqrs.bak'

# PostgreSQL
psql -U postgres cqrs_event_store < backup.sql

# MongoDB
mongorestore --uri="mongodb://localhost:27017" backup/
```

Event stream recovery:
```csharp
// Rebuild projections from event store
var projectionService = serviceProvider.GetRequiredService<IProjectionService>();
await projectionService.RebuildProjectionsAsync();

// Verify integrity
var eventStore = serviceProvider.GetRequiredService<IEventStore>();
var allEvents = await eventStore.GetAllEventsAsync();
Console.WriteLine($"Recovered {allEvents.Count} events");
```

## Environment Configuration

### appsettings.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning"
    }
  },
  "EventStore": {
    "ConnectionString": "Server=.;Database=CqrsEventStore",
    "Type": "SqlServer"
  },
  "Cache": {
    "ConnectionString": "localhost:6379",
    "Type": "Redis"
  },
  "Snapshots": {
    "Interval": 100,
    "Enabled": true
  }
}
```

### Production Checklist

- [ ] SQL database created and indexed
- [ ] Redis cache configured
- [ ] Snapshots enabled (interval: 50-100 events)
- [ ] Logging configured (structured logs)
- [ ] Health checks configured
- [ ] Backup procedures automated
- [ ] SSL certificates configured
- [ ] Rate limiting enabled
- [ ] Request logging enabled
- [ ] Error handling middleware active
- [ ] Monitoring alerts configured
- [ ] Disaster recovery plan documented

## Scaling Considerations

### Horizontal Scaling

Multiple application instances with:
- Shared SQL event store
- Shared Redis cache
- Load balancer (nginx, HAProxy)
- Distributed session handling

### Event Bus at Scale

Replace in-process event bus with:

**RabbitMQ:**
```csharp
services.AddSingleton<IEventBus>(sp =>
    new RabbitMqEventBus(
        "amqp://user:pass@localhost:5672"
    )
);
```

**Azure Service Bus:**
```csharp
services.AddSingleton<IEventBus>(sp =>
    new ServiceBusEventBus(
        Environment.GetEnvironmentVariable("ServiceBusConnection")
    )
);
```

**Kafka:**
```csharp
services.AddSingleton<IEventBus>(sp =>
    new KafkaEventBus(
        "localhost:9092"
    )
);
```

## Troubleshooting Deployment

### Connection Timeout
```csharp
// Increase timeout
var options = new SqlServerOptions
{
    CommandTimeout = 60,
    ConnectionTimeout = 30
};
```

### Out of Memory
- Reduce snapshot interval
- Implement event stream pagination
- Enable query result streaming

### Slow Event Store Queries
- Add database indexes
- Enable query result caching
- Consider read replicas for projections

### Event Bus Bottleneck
- Use durable queues (RabbitMQ, Service Bus)
- Implement batch processing
- Add consumer workers

---

See `docker-compose.yml` and `Dockerfile` for complete containerization setup.
