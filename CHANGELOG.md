# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2025-09-12

### Added
- Comprehensive documentation suite (architecture, deployment, API reference, FAQ)
- Production example programs in `examples/` directory (07 scenarios)
- Docker and docker-compose configuration for containerized deployment
- CI/CD pipeline with GitHub Actions (build, test, NuGet publish)
- CodeQL security scanning workflow
- Dependabot configuration for automated dependency updates
- Idempotency key handling for preventing duplicate operations
- Request context middleware for correlation ID tracking
- Health check and diagnostics endpoints
- Snapshot compression service for storage efficiency
- CSV and JSON export formatters for event streams
- Webhook dispatcher for external integration
- Performance monitoring utilities

### Changed
- Promoted to stable release; public API is now considered stable
- Enhanced error messages throughout for better diagnostics
- Improved logging coverage across infrastructure layer

### Fixed
- Race condition in concurrent event processing under high load
- Memory leak in event handler subscription lifecycle
- Null reference exception during projection rebuild on empty streams

### Security
- Added request validation decorators
- Implemented rate limiting middleware
- Enhanced input sanitization in value objects

## [0.7.0] - 2025-07-25

### Added
- ProjectionService for building optimized read models from event streams
- SnapshotService for aggregate state caching with configurable intervals
- IncrementalSnapshot support for partial state persistence
- CacheService abstraction for pluggable caching backends
- Validation decorator for command pre-processing
- RequestResponseLog model for structured audit trail
- DateTimeExtensions and StringExtensions utility methods
- ReflectionUtilities for dynamic type handling
- SerializationUtilities for consistent JSON serialization

### Changed
- Improved AggregateRoot base class event application pipeline
- Enhanced EventStore with `fromVersion` pagination parameter
- Refactored repositories to generic `IRepository<T>` interface
- Strengthened value object operator overloading for Money and Balance

### Fixed
- Event deserialization issues with polymorphic event types
- Aggregate version tracking inconsistencies on concurrent replays
- EventBus subscription memory leaks on repeated subscribe/unsubscribe cycles

## [0.5.0] - 2025-06-06

### Added
- Background workers: `ProjectionWorker` and `SnapshotWorker` using `IHostedService`
- Rate limiting middleware with configurable request thresholds
- Error handling middleware with structured JSON error responses
- Logging middleware for request/response tracing
- `DomainEventPublisher` and `EventDispatcher` for infrastructure event routing
- `HttpClientFactory` wrapper and `WebhookDispatcher` for external integrations
- `PaginationHelper` for cursor-based result pagination
- `GuardClauses` utility for precondition checks

### Changed
- Separated infrastructure concerns into dedicated sub-namespaces
- Switched event bus subscription storage from `List` to `ConcurrentDictionary`

### Fixed
- Snapshot retrieval returning stale data when aggregate version advanced past snapshot

## [0.4.0] - 2025-05-09

### Added
- Controller layer: `AccountsController`, `EventsController`, `QueriesController`
- `HealthController` and `DiagnosticsController` for operational visibility
- `BaseApiController` with shared response helpers
- `LoggingDecorator` wrapping `IAccountService` for transparent operation logging
- `CommandExtensions` for fluent command building
- `GetAccountQuery` and query handler infrastructure
- `CreateAccountCommand` with validation attributes

### Changed
- `AccountService` now returns `Result<T>` instead of throwing on domain errors
- Consolidated dependency injection registration into `DependencyInjection.cs`

## [0.3.0] - 2025-04-18

### Added
- `EventBus` pub/sub system with async multi-handler support
- `EventHandlers` for account domain event reactions (notifications, audit)
- `AccountService` orchestrating command dispatch and event persistence
- `IAccountService` interface for testability and DI
- `IEventBus` and `IEventStore` service abstractions
- `DatabaseConfiguration` for future persistence backend wiring
- `InfrastructureConfiguration` registration helper

### Changed
- Moved event store write path to use `expectedVersion` for optimistic concurrency
- Improved `EventEnvelope` metadata: added `CorrelationId` and `UserId` fields

### Fixed
- `AccountCreated` event missing `InitialBalance` field on deserialization

## [0.2.0] - 2025-03-28

### Added
- `AggregateRoot<T>` base class with uncommitted event tracking and version management
- `Account` aggregate with `CreateAccount`, `Deposit`, `Withdraw`, `CloseAccount` operations
- `DomainEvent` abstract base and `EventEnvelope` wrapper with full metadata
- `AccountCreated`, `MoneyDeposited`, `MoneyWithdrawn`, `AccountClosed` domain events
- `Money` and `Balance` immutable value objects with operator overloading
- `Transaction` value object capturing individual ledger entries
- `AggregateSnapshot` and `IncrementalSnapshot` domain models
- `InMemoryEventRepository` for development and testing
- `AccountRepository` generic repository implementation
- `Result<T>` monadic error type eliminating exception-based control flow
- `DomainException` and `CqrsException` typed exception hierarchy
- `CqrsConstants` and `EventType` enum for shared constants
- `ValidationExtensions` for fluent guard expressions

### Changed
- Switched event identity from `int` to `string` aggregate ID for flexibility

## [0.1.0] - 2025-03-07

### Added
- Initial project structure with layered architecture (Domain, Application, Data, Infrastructure, Shared)
- .NET 10 project configuration with nullable reference types enabled
- `.editorconfig` for consistent code style across editors
- MIT license, README skeleton, and CONTRIBUTING guidelines
- `.gitignore` tuned for .NET projects

---

## Upgrade Guide

### 0.1.0 → 0.2.0
- No API existed yet; adopt `AggregateRoot<T>` as the base for all aggregate classes.

### 0.2.0 → 0.3.0
- Register services via `DependencyInjection.cs`; replace direct `new EventStore()` construction.

### 0.3.0 → 0.4.0
- Replace direct `AccountService` instantiation with `IAccountService` from DI.
- Switch call sites from exception handling to checking `Result<T>.IsSuccess`.

### 0.4.0 → 0.5.0
- No breaking changes; opt into rate limiting and error middleware in `Program.cs`.

### 0.5.0 → 0.7.0
- Configure `SnapshotService` snapshot interval (recommended: 50–100 events).
- Migrate read-model code to `IProjectionService.BuildProjectionAsync`.
- Update event handler registration to use new bus subscription syntax.

### 0.7.0 → 1.0.0
- No breaking changes.
- Consider enabling correlation ID tracking via `RequestContextMiddleware`.
- Review snapshot intervals for high-volume aggregates.

---

## Versioning

This project follows [Semantic Versioning](https://semver.org/):

- **MAJOR**: Breaking changes to public API
- **MINOR**: New features, backwards compatible
- **PATCH**: Bug fixes, no new features

---

## Contributing

See [README.md](README.md#contributing) for contribution guidelines.

---

Built by [Vladyslav Zaiets](https://sarmkadan.com) - CTO & Software Architect
