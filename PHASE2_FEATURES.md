# Phase 2: Features & Infrastructure

## Overview

Phase 2 adds comprehensive infrastructure, middleware, utilities, and production-ready features to the CQRS + Event Sourcing framework. This phase transforms the core foundation into a production-grade system with proper error handling, monitoring, caching, and integration capabilities.

**Total New Files: 30+**  
**Total Lines of Code: 5000+**

---

## Architecture Components

### 1. Presentation Layer (4 controllers)

#### **BaseApiController**
- Standardized HTTP response handling
- Result{T} pattern integration
- Consistent error formatting
- Created/200/400 status code mapping

#### **AccountsController**
- Command endpoints: Create, Deposit, Withdraw
- Query endpoints: GetAccount, GetEvents
- Idempotency support via X-Idempotency-Key header
- Event replay endpoint for admin operations

#### **EventsController**
- Event stream inspection: Get, Count, Export
- Event timeline visualization
- CSV/JSON export formats
- Event type analysis

#### **QueriesController**
- Account listing with pagination
- Search functionality
- Balance queries with caching
- Transaction history with date filtering
- Statistics and reporting endpoints

#### **HealthController**
- Kubernetes-style health probes (live, ready, detailed)
- Dependency health checks
- Application info and version
- Uptime tracking

#### **DiagnosticsController**
- Performance metrics dashboard
- Cache statistics
- System information
- Health summaries

---

### 2. Middleware Pipeline (5 components)

#### **LoggingMiddleware**
- Request/response logging with timing
- Automatic request ID generation
- Configurable log level based on HTTP status
- Excludes health checks and static files
- Request body capture (max 10KB)

#### **ErrorHandlingMiddleware**
- Global exception handler
- Domain exception → 400 mapping
- Unexpected exception → 500 mapping
- Unique error IDs for tracking
- Structured error responses

#### **RateLimitingMiddleware**
- Token bucket algorithm
- Per-IP rate limiting (60 req/min default)
- Exponential backoff for retries
- Automatic bucket cleanup
- Thread-safe concurrent collection

#### **RequestContextMiddleware**
- Correlation ID extraction/generation
- Request context propagation via AsyncLocal
- Response header injection
- User ID extraction

#### **IdempotencyMiddleware**
- Idempotency-Key header support
- Cached response replay
- Configurable retention (24 hours default)
- Automatic expiration cleanup

---

### 3. Utilities (7 files)

#### **GuardClauses**
- NotNull, NotNullOrEmpty validation
- InRange, NotNegative, NotZero
- Condition and Matches validation
- Consistent error messages

#### **StringExtensions**
- ToSlug, ToCamelCase, ToPascalCase, ToSnakeCase
- Truncate, Repeat, RemoveWhitespace
- Email and GUID validation
- EnsureStartsWith/EndsWith
- AlphanumericOnly and IsNumeric

#### **DateTimeExtensions**
- UTC normalization and rounding
- TruncateTo, IsPast, IsFuture, IsToday
- AgeInYears calculation
- ISO8601 and RFC7231 formatting
- Start/End of day/month/year
- GetNextOccurrenceOfDay
- GetRelativeTime (e.g., "2 hours ago")
- TimeSpan human-readable formatting

#### **ReflectionUtilities**
- GetTypesImplementing with caching
- GetPublicProperties, FindMethod
- GetGenericArguments, IsGenericTypeOf
- CreateInstance and GetBaseGenericType
- Cached reflection results

#### **SerializationUtilities**
- ToJson/FromJson (System.Text.Json)
- FromJsonNewtonsoft (compatibility)
- DeepClone via serialization
- MergeJson for PATCH operations
- Todictionary conversion

#### **CqrsHelpers**
- GetCommandHandlers, GetEventHandlers
- GetHandlerMetadata with caching
- RegisterEventType/ResolveEventType
- ExtractAggregateId from command/event
- GetTargetAggregateType
- ValidateCommand with error collection

#### **PaginationHelper**
- PagedResult{T} model
- Paginate for IEnumerable
- PaginateQuery for IQueryable
- ValidatePaginationParams
- GetOffsetAndLimit
- ToPagedResult extensions
- ToApiResponse formatting

---

### 4. Caching Layer

#### **InMemoryCacheService**
- ICacheService implementation
- Thread-safe with ConcurrentDictionary
- TTL-based expiration
- GetOrCreateAsync (cache-aside pattern)
- RemoveByPattern with wildcard support
- Automatic eviction timer (5 min intervals)
- CacheStatistics tracking

---

### 5. Event System

#### **DomainEventPublisher**
- Observer pattern for domain events
- Subscribe/Unsubscribe pattern
- PublishAsync with error resilience
- Handler error isolation (failures don't block others)
- GetSubscriberCount for testing

#### **EventDispatcher**
- Coordinates persistence + publishing
- At-least-once delivery semantics
- Batch event dispatching
- DispatchManyAsync with causal ordering

---

### 6. Integration Layer

#### **HttpClientFactory**
- Standardized HTTP client configuration
- Timeouts (30s default)
- Standard headers (Accept, User-Agent)
- CreateClientWithBaseAddress
- CreateAuthenticatedClient with Bearer token
- Connection keep-alive
- Request ID header injection

#### **WebhookDispatcher**
- Fire-and-forget webhook delivery
- Retry logic with exponential backoff (3 attempts)
- HMAC signature generation
- Idempotency-Key header for deduplication
- WebhookRegistration model
- Active flag for disabling webhooks

---

### 7. Formatters

#### **JsonFormatter**
- IJsonFormatter interface
- Camel case property naming
- Null value handling
- Enum string serialization
- Custom DateTime (ISO8601) converter
- Custom Decimal converter
- Pretty-print option

#### **CsvFormatter**
- ICsvFormatter interface
- Automatic column detection from properties
- CSV value escaping (quotes, newlines, commas)
- [CsvIgnore] attribute support
- [CsvColumn] for custom naming
- WithSemicolonDelimiter/WithTabDelimiter
- Format with or without headers

---

### 8. Background Workers

#### **SnapshotWorker**
- IHostedService implementation
- Periodic snapshot creation (5 min default)
- Event threshold for snapshots (100 events default)
- Manual CreateSnapshotAsync trigger
- Aggregate state serialization

#### **ProjectionWorker**
- IHostedService implementation
- Event-driven projection updates
- PauseAsync/ResumeAsync controls
- RebuildProjectionAsync for recovery
- Graceful degradation

---

### 9. Observability & Monitoring

#### **PerformanceMonitor**
- IPerformanceMonitor interface
- Per-operation metrics collection
- InvocationCount, SuccessRate, DurationMs
- MinDurationMs/MaxDurationMs tracking
- Concurrent metric storage
- PerformanceScope for timing
- PerformanceHealthCheck

#### **RequestResponseLog**
- RequestLog model (headers, body, method, path)
- ResponseLog model (status code, duration)
- ApiAuditLog combining both
- OperationLog with status tracking
- ErrorLog with exception details
- EventAuditLog for event stream

---

### 10. Decorators

#### **ValidationDecorator**
- Command property validation
- Business rule validation
- IsModificationCommand detection
- Result{T} error wrapping

---

### 11. Configuration

#### **InfrastructureConfiguration**
- AddInfrastructure service registration
- UseInfrastructure middleware setup
- Modular component registration
- AddCaching, AddEventServices, AddFormatters
- AddIntegration, AddWorkers, AddObservability
- ConfigureCqrsFramework fluent builder

---

### 12. Extensions & Helpers

#### **CommandExtensions**
- ExecuteCommandAsync with error handling
- GetOrCreateCorrelationId
- EnrichEvent with context
- Validate method
- CreateEventFromCommand factory
- GetEventDisplayName formatting
- GetEventSummary for logging

#### **ResultExtensions**
- Map{T} for successful result transformation
- BindAsync for monadic chaining
- TapAsync for side effects

#### **AggregateExtensions**
- ValidateStateForOperation
- GetAggregateSummary

---

## Key Features

### Error Handling
- Global exception handler with correlation tracking
- Domain exceptions → 400 Bad Request
- Unexpected exceptions → 500 Internal Server Error
- Error IDs for log correlation

### Request Tracking
- Correlation ID propagation
- Request ID generation
- AsyncLocal context throughout async chain
- User ID extraction from headers/claims

### Caching Strategy
- In-memory cache with TTL
- Cache-aside pattern implementation
- Pattern-based invalidation
- Automatic expiration cleanup
- Hit tracking and statistics

### Rate Limiting
- Per-IP token bucket algorithm
- 60 requests/minute default
- Exponential backoff for retries
- Graceful degradation (429 Too Many Requests)

### Idempotency
- Idempotency-Key header support
- Response caching for retries
- 24-hour retention default
- Configurable per-operation

### Performance Monitoring
- Operation-level metrics
- Success rate calculation
- Duration tracking (min/avg/max)
- Health check integration

### Integration
- HTTP client factory with sensible defaults
- Webhook dispatching with retry
- HMAC signature verification
- External event handling

---

## Usage Examples

### Using GuardClauses
```csharp
public void CreateAccount(string name, decimal initialBalance)
{
    GuardClauses.NotNullOrEmpty(name, nameof(name));
    GuardClauses.NotNegative(initialBalance, nameof(initialBalance));
    // Safe to proceed
}
```

### Using Caching
```csharp
var account = await _cacheService.GetOrCreateAsync(
    key: $"account:{accountId}",
    factory: ct => _eventStore.GetEventsAsync(accountId, ct),
    expiration: TimeSpan.FromMinutes(5)
);
```

### Using Pagination
```csharp
var pagedResult = accounts.ToPagedResult(pageNumber: 1, pageSize: 20);
return Ok(pagedResult.ToApiResponse());
```

### Using Command Extensions
```csharp
var command = new CreateAccountCommand("John Doe", 1000m, "USD");
var errors = command.Validate();
if (errors.Count > 0) return BadRequest(errors);

var result = await handler.ExecuteCommandAsync(cancellationToken);
return Response(result);
```

---

## Testing & Validation

All components are designed with testability in mind:
- Dependency injection enables mocking
- GuardClauses prevent invalid state
- Result{T} pattern eliminates null checks
- Async/await throughout for async testing
- Performance metrics for bottleneck identification

---

## Production Readiness

✅ Error handling with correlation tracking  
✅ Request logging with sensitive data filtering  
✅ Rate limiting to prevent abuse  
✅ Idempotency for safe retries  
✅ Health checks for orchestration  
✅ Performance monitoring and metrics  
✅ Caching with TTL and cleanup  
✅ Webhook integration with retry  
✅ Background workers for maintenance tasks  
✅ Structured logging throughout  

---

## Future Enhancements

- Redis integration for distributed caching
- Database-backed event store persistence
- Distributed tracing (OpenTelemetry)
- CQRS command bus implementation
- Saga/Process Manager pattern
- Multi-tenant support
- GraphQL API layer
- gRPC service interfaces
