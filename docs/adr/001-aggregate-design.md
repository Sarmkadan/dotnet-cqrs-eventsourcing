# ADR-001: Aggregate Design

**Status:** Accepted  
**Date:** 2024-01-01

## Context

Event-sourced systems store state as an ordered sequence of domain events.
The aggregate root is the consistency boundary: all invariants must hold within
a single aggregate, and changes are committed atomically via its event stream.

Poorly chosen aggregate boundaries cause either:
- **Too large** — high contention, slow rehydration, monolithic state.
- **Too small** — cross-aggregate invariants that cannot be enforced atomically,
  leading to complex sagas for simple operations.

## Decision

### 1. Aggregate boundaries are enforced by business invariants, not by data shape

An aggregate should encapsulate all state needed to enforce its own invariants.
If enforcing invariant X requires reading data from aggregate Y, then X and Y
belong in the same aggregate *or* the invariant must be relaxed to eventual
consistency.

### 2. Aggregates are identified by a single strongly-typed ID

Each aggregate type has an `Id : string` (GUID-formatted) as its root identifier.
All events in the stream reference this ID via `AggregateId`.

### 3. `AggregateRoot.Version` is a `long`

Version is a monotonically increasing counter that begins at 0 and increments
with every raised event.  Using `long` accommodates high-throughput aggregates
that accumulate millions of events over their lifetime (e.g., order or inventory
aggregates in busy SaaS systems) without overflow.

### 4. Cross-aggregate communication via domain events only

Aggregates must not hold references to other aggregates or call repositories
directly.  Coordination across aggregate roots happens through domain events
published after a successful commit.

### 5. Aggregate methods are commands; events are facts

- Command methods (e.g., `Deposit`, `Withdraw`) validate preconditions and call
  `RaiseEvent`.
- `ApplyEvent` updates internal state based on the raised event.
- This separation makes aggregates fully testable without infrastructure.

## Consequences

- Aggregates remain small and focused; rehydration cost is proportional to the
  number of events since the last snapshot.
- Cross-aggregate workflows use sagas or process managers, keeping each aggregate
  simple.
- `AggregateVersion` as `long` allows auditing systems and projections to handle
  very long-lived aggregates without integer overflow.
