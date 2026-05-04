# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.2.0] - 2026-05-04

### Added
- Comprehensive documentation suite (architecture, deployment, API reference)
- Production example programs in `examples/` directory
- Docker and docker-compose configuration for containerized deployment
- CI/CD pipeline with GitHub Actions (build, test, Docker push)
- Snapshot service for performance optimization of large event streams
- Event batching support for improved persistence performance
- Health check endpoints and monitoring integration points
- Idempotency key handling for preventing duplicate operations
- Request context middleware for correlation ID tracking
- Comprehensive FAQ documentation

### Changed
- Updated all code with consistent header format
- Enhanced error messages for better diagnostics
- Improved logging throughout infrastructure layer
- Refactored configuration system for better extensibility

### Fixed
- Race condition in concurrent event processing
- Memory leak in event handler subscription
- Null reference exception in projection rebuild

### Security
- Added request validation decorators
- Implemented rate limiting middleware
- Enhanced input sanitization in value objects

## [1.1.0] - 2026-04-15

### Added
- ProjectionService for building optimized read models
- SnapshotService for aggregate state caching
- Event versioning support with event type resolution
- CacheService abstraction for distributed caching
- Validation decorators for command processing
- RequestResponseLog for audit trail
- DateTimeExtensions and StringExtensions utilities
- ReflectionUtilities for dynamic type handling
- SerializationUtilities for consistent JSON handling

### Changed
- Upgraded to .NET 10 with latest language features
- Improved AggregateRoot base class with better event handling
- Enhanced EventStore with pagination support
- Refactored repositories with generic IRepository<T> interface
- Updated value objects with better operator overloading

### Fixed
- Event deserialization issues with polymorphic types
- Aggregate version tracking inconsistencies
- EventBus subscription memory leaks

## [1.0.0] - 2026-03-20

### Added
- Core CQRS implementation with Command/Query separation
- Event Sourcing framework with EventStore and EventRepository
- Aggregate Root pattern with DomainEvent base class
- Account aggregate example with Money and Balance value objects
- Transaction value object with immutable design
- EventEnvelope for event metadata and versioning
- In-memory event repository for development
- EventBus pub/sub system for event handling
- Optimistic concurrency control with version checking
- AccountService for business operations
- Result<T> pattern for error handling
- DomainException and CqrsException hierarchy
- Dependency injection integration via ConfigureServices
- Logging integration with Microsoft.Extensions.Logging
- Controller layer with AccountsController, EventsController, QueriesController
- Health check and diagnostics endpoints
- Request/response logging middleware
- Error handling middleware with structured exception responses
- Rate limiting middleware
- Initial documentation and README

### Features
- Type-safe C# implementation with full async support
- Minimal external dependencies (Microsoft.Extensions, Newtonsoft.Json)
- Extension methods for convenient API surface
- Guard clauses for input validation
- Pagination helpers for large result sets
- CSV and JSON formatters for event export
- Webhook dispatcher for external integration
- Performance monitoring utilities
- Event handlers with event type resolution
- Query handlers for read operations
- Command handlers with business logic

## [0.1.0] - 2026-03-01

### Added
- Project scaffolding and structure
- Basic architectural layers (Domain, Application, Data, Infrastructure)
- Initial project manifest and phase planning
- .NET 10 project configuration

---

## Upgrade Guide

### 0.1.0 -> 1.0.0
- Initialize DI with `services.AddCqrsFramework()`
- Use `Result<T>` for error handling instead of exceptions
- Implement `IAccountService` interface instead of direct AccountService usage

### 1.0.0 -> 1.1.0
- Update snapshots strategy: configure `SnapshotService` snapshot interval
- Migrate to `IProjectionService` for read models
- Update event handlers to use new event bus subscription syntax

### 1.1.0 -> 1.2.0
- No breaking changes
- Consider enabling rate limiting middleware
- Update to use new correlation ID from request context
- Review snapshot intervals (recommended: 50-100 events)

---

## Future Roadmap

### 2.0.0 (Planned)
- Distributed event store support (multiple instances, replication)
- Saga support for distributed transactions
- Dead letter queue handling
- Event schema versioning and migration tools
- Temporal query support ("as of" specific timestamp)
- Built-in GDPR right-to-be-forgotten support
- GraphQL endpoint for queries
- Event compression for historical events
- Multi-tenant support

### 2.1.0 (Planned)
- Automatic snapshot strategy optimization
- Performance analytics dashboard
- Event store sharding support
- Projections hot reload without restart
- Event store backup and recovery tools
- Integration tests templates

### 3.0.0 (Future)
- Code generation tools for aggregates and events
- Visual event stream debugging tools
- Advanced monitoring and observability dashboard
- Enterprise licensing and commercial support
- Certified training materials
- Reference architecture implementations

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
