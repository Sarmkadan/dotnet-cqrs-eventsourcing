# ADR-003: Projection Consistency Model

**Status:** Accepted  
**Date:** 2024-01-01

## Context

Read models (projections) are denormalised views built from domain events.  They
can be updated in two ways:

1. **Synchronous** — the projection is updated within the same unit of work that
   commits the event, guaranteeing the caller sees their own write immediately.
2. **Eventual** — the projection is updated asynchronously after the event is
   published to an event bus.

Each model has different trade-offs around consistency, coupling, and scalability.

## Decision

### 1. Default: eventual-consistency via `ReadModelProjectionEngine`

`ReadModelProjectionEngine` subscribes to `DomainEvent` on `IEventBus` and routes
each event to all registered `IReadModelProjectionRunner` implementations.
Projection runners are invoked asynchronously after the aggregate commit.

This is the **preferred model** for all public read endpoints because:
- The write path (aggregate commit) is decoupled from projection logic.
- Individual projectors can fail and retry without affecting the command path.
- New projectors can be added without changing command handlers.

### 2. Retry with exponential back-off

Transient projector failures are retried up to `MaxRetryAttempts` (default 3)
times, with a base delay of `RetryBaseDelayMilliseconds` (default 100 ms) that
doubles on each attempt.  This tolerates brief store unavailability without
silently dropping events.

### 3. Dead-letter store for persistent failures

Events that exhaust all retry attempts are routed to `IDeadLetterStore` for
manual inspection and reprocessing.  The dead-letter store is queryable by
projection name, event type, and aggregate ID so that operators can diagnose
root causes and replay only the affected events.

### 4. Checkpointing for resumable replay

The engine writes a `ProjectionCheckpoint` every `CheckpointInterval`
successfully processed events per projection.  Checkpoints record the last
processed event ID and aggregate version, enabling gap detection and
catch-up after a restart.

### 5. When to prefer synchronous updates

Use a synchronous read-model update (inside the same handler that commits the
aggregate) only when:
- The calling code *must* read its own write before returning to the user (e.g.,
  a confirmation screen that shows the updated balance).
- The projection is trivially cheap (e.g., an in-process cache invalidation).

In all other cases, eventual consistency is preferred.

## Consequences

- Read models may lag behind the write model by milliseconds under normal load.
- Callers that need strong read-your-writes consistency must either accept slight
  latency or use a synchronous projection for that specific use case.
- The dead-letter store gives operators visibility into failed events without
  data loss.
- Projection rebuild is idempotent: replaying the full event stream for an
  aggregate will converge to the correct state regardless of prior failures,
  provided projectors implement idempotent `Apply` logic.
