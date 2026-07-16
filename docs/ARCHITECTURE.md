# Architecture

This document describes the solution as it exists in the code today - what the
pieces are, how data flows through them, and why they are shaped the way they
are. For the narrative introduction to CQRS/ES concepts see
[architecture.md](architecture.md); this file is the map of the actual code.

## What this is

A single-assembly CQRS + Event Sourcing framework with a banking `Account`
aggregate as the reference domain. The entry point (`Program.cs`) is a console
app that either runs a scripted demo of the full lifecycle (create, deposit,
withdraw, snapshot, close) or dispatches to CLI commands when arguments are
passed (currently `rebuild-readmodels` via `ReadModelRebuilderCommand`).

Everything is in one project (`dotnet-cqrs-eventsourcing.csproj`), with layers
enforced by folder/namespace convention rather than assembly boundaries:

```
Domain/          aggregates, events, value objects, snapshots - no infra deps
Application/     services (EventStore, EventBus, AccountService...), sagas, queries
Data/            repository abstractions + in-memory implementations
ReadModels/      projection engine, projectors, read-model stores, dead letters
Infrastructure/  event dispatch, workers, caching, middleware, CLI, integration
Presentation/    ASP.NET controllers (Accounts, Queries, Events, Diagnostics, Health)
Shared/          Result<T>, guard clauses, constants, common exceptions
Configuration/   DI wiring (AddCqrsFramework) + options
```

Supporting projects: `tests/` (xUnit), `dotnet-cqrs-eventsourcing.Benchmarks/`
(BenchmarkDotNet, event-store append/replay), and `examples/` (seven runnable
walkthroughs from basic aggregates to full scenarios).

## Write path

```
caller
  └─> IAccountService (AccountService)
        ├─ new Account() / repo.GetByIdAsync -> domain method (Deposit, Withdraw, ...)
        │    Account raises DomainEvents via AggregateRoot.RaiseEvent
        ├─> IRepository<Account> (AccountRepository)
        │     └─> IEventStore.AppendEventsAsync
        │           └─> IEventRepository (InMemoryEventRepository)
        │                 wraps each event in an EventEnvelope with serialized payload
        └─> IEventBus.PublishEventsAsync   (after successful save)
              └─ subscribers: ProjectionService, ReadModelProjectionEngine, custom handlers
```

Key points, grounded in the code:

- **Validation lives in the domain.** `Account.CreateAccount`, `Money`, and
  `Balance` throw `DomainException`; `AccountService` catches and translates
  into `Result.Failure` with an error code (`CREATE_ACCOUNT_FAILED` etc.).
  Services never return raw exceptions to callers.
- **Results, not exceptions, at layer boundaries.** `Shared/Results/Result<T>`
  is the return type of every service and repository method. Exceptions are an
  intra-layer mechanism only.
- **Optimistic concurrency.** `AggregateRoot.Version` increments per applied
  event; `EventStore`/`InMemoryEventRepository` reject appends whose
  `AggregateVersion` conflicts with the stored stream (see
  `examples/06-Concurrency`).
- **Event serialization.** `EventStore` serializes events into
  `EventEnvelope`s and resolves types on read via `EventTypeRegistry`, which
  scans the domain assembly for `[EventName("...")]`-decorated events. Rationale:
  stable string names instead of `Type.GetType()`, so events survive renames
  and refactors (see `docs/adr/` for the aggregate design ADR).
- **Multi-tenancy hook.** `AggregateRoot.TenantId`, when set, flows into every
  raised event and onto `EventEnvelope.PartitionKey` for per-tenant stream
  isolation. Single-tenant deployments just leave it null.

## Read path(s)

There are deliberately **two** projection mechanisms:

1. **`ProjectionService`** (Application/Services) - a minimal
   dictionary-of-dictionaries projection keyed by aggregate id. It is what the
   demo wires up in `DependencyInjection.ConfigureEventHandlers` (subscribes to
   all `DomainEvent`s on the `IEventBus`). Good for illustrating the concept;
   not meant for real read models.
2. **`ReadModelProjectionEngine`** (ReadModels/) - the real one. Typed
   projectors (`IReadModelProjector<T>`, e.g. `AccountProjector` ->
   `AccountReadModel`) run behind `IReadModelProjectionRunner`s with
   configurable retry (`MaxRetryAttempts`), bounded concurrency
   (`MaxConcurrentProjectors`), checkpointing, and a dead-letter store
   (`IDeadLetterStore` / `InMemoryDeadLetterStore`) for events that exhaust
   retries. `ProjectionDiagnosticsService` exposes lag/health, surfaced by
   `DiagnosticsController`. Registered via
   `AddReadModelProjections().AddAccountProjections()` and activated with
   `UseReadModelProjections()`.

Queries go through `IAccountReadModelQueryService` (read models) or
`GetAccountQuery`/`QueriesController`, never through the write-side aggregate.

Rebuilds: replay is a first-class operation. `ReadModelRebuilderCommand` (CLI)
and `IProjectionWorker` re-read the event stream from `IEventStore` and re-run
projectors; consistency expectations are documented in
`docs/adr/003-projection-consistency-model.md` (eventual consistency, at-least-once
handler delivery - projectors must be idempotent).

## Snapshots and compaction

- `ISnapshotService`/`SnapshotService` store `AggregateSnapshot`s (plus
  `IncrementalSnapshot` for delta snapshots); strategy trade-offs are in
  `docs/adr/002-snapshotting-strategy.md`.
- `ISnapshotCompressionService` (Infrastructure/Compression) gzips snapshot
  payloads; opt-in via `SnapshotCompressionOptions`.
- `IEventStoreCompactionService` trims event streams below the latest snapshot
  version - the trade: you give up full history for bounded storage, which is
  why it is a separate, explicitly-invoked service rather than a background
  default.
- `SnapshotWorker` (hosted service) automates periodic snapshotting when the
  app runs under a host that starts `IHostedService`s.

## Event dispatch: two seams, one rationale

- `IEventBus` (Application) is the in-process pub/sub used by services -
  subscribe by event type, fire-and-forget semantics for handlers.
- `IEventDispatcher` (Infrastructure/Events) composes `IEventStore` +
  `IDomainEventPublisher` into a persist-then-publish unit: if persistence
  fails, handlers never run; if a handler fails, the event stays persisted.
  This is the seam to replace with an outbox/real broker later without touching
  application services.

`SagaOrchestrator` (Application/Sagas) coordinates multi-aggregate processes:
sagas implement `ISagaHandler`, persist state via `ISagaRepository`
(`InMemorySagaRepository` provided), and react to events with compensation on
failure.

## Composition root and hosting

`Configuration/DependencyInjection.AddCqrsFramework(IConfiguration)` registers
the core: repositories, event store, event bus, projection/snapshot/compaction
services, saga orchestrator, `AccountService`, and the `EventTypeRegistry`
assembly scan. Options (`DotnetCqrsEventsourcingOptions`) bind from the
`DotnetCqrsEventsourcing` config section with data-annotation validation and
`ValidateOnStart`.

`Infrastructure/Configuration/InfrastructureConfiguration.AddInfrastructure`
layers on the web-host concerns: caching (`ICacheService`), event dispatcher,
formatters (JSON/CSV), HTTP clients + `IWebhookDispatcher`, hosted workers,
`IPerformanceMonitor` + health checks, and idempotency handling.
`UseInfrastructure` orders the middleware pipeline: global error handling ->
request context -> request logging -> rate limiting -> idempotency.

Everything is registered as singletons on purpose: all default stores are
in-memory, so per-request lifetimes would just fragment state.

## Extension points

| To swap/add | Implement | Registered in |
|---|---|---|
| Real event storage (SQL, EventStoreDB) | `IEventRepository` | `AddCqrsFramework` |
| Durable read models | `IReadModelStore<T>` | `AddAccountProjections` |
| New read model | `IReadModelProjector<T>` + runner | `ReadModelExtensions` |
| Message broker / outbox | `IDomainEventPublisher` or `IEventDispatcher` | `AddEventServices` |
| Distributed cache | `ICacheService` | `AddCaching` |
| New CLI command | `ICliCommand` | `Program.cs` |
| New aggregate | derive `AggregateRoot`, raise `DomainEvent`s | your own module |
| Cross-cutting command behavior | decorators (`LoggingDecorator`, `ValidationDecorator`) | DI |

## Known limitations

- **All persistence is in-memory.** Event repository, read-model store, saga
  repository, dead-letter store, idempotency store - process restart loses
  everything. This is by design for a reference implementation, but nothing
  here is production storage.
- **Single assembly.** Layering is by convention; nothing stops
  `Application` from referencing `Infrastructure` (and `EventStore` does use
  `Infrastructure.Events.EventTypeRegistry`). Splitting into projects would
  make the boundaries compiler-enforced at the cost of solution complexity -
  not worth it while the code doubles as teaching material.
- **The console entry point does not host the web layer.** `Program.cs` builds
  a bare `ServiceCollection`; the controllers, middleware, and hosted workers
  in `Presentation/` and `Infrastructure/` are wired for an ASP.NET host that
  a consumer would build (`AddInfrastructure` + `UseInfrastructure`) - the demo
  itself never starts Kestrel.
- **Two projection systems** (see Read path). `ProjectionService` is
  pedagogical; new code should target `ReadModelProjectionEngine`.
- **At-least-once handler delivery.** Handlers/projectors must be idempotent;
  there is no exactly-once machinery beyond the idempotency-key middleware on
  the HTTP edge.
