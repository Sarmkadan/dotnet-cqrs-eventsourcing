# ADR-002: Snapshotting Strategy

**Status:** Accepted  
**Date:** 2024-01-01

## Context

Replaying an entire event stream on every aggregate load is correct but expensive
when an aggregate accumulates thousands of events.  Snapshots capture the full
state at a particular version so that only events *after* the snapshot need to be
replayed on the next load.

The trade-off is between:
- **Snapshot frequency** — more frequent snapshots reduce replay cost but
  increase storage writes.
- **Snapshot size** — full snapshots can be large; incremental snapshots are
  smaller but require chain traversal.

## Decision

### 1. Snapshot interval is configurable per deployment

The default threshold is **100 events** between snapshots, tunable via
`SnapshotWorker` constructor parameter `eventsThresholdForSnapshot`.
Teams with expensive rehydration should lower this to 10–25; teams with cheap
rehydration can raise it to 200–500.

### 2. Both full and incremental snapshots are supported

- `AggregateSnapshot` — a complete serialisation of the aggregate state.
  Always correct; preferred when storage cost is acceptable.
- `IncrementalSnapshot` — a delta against the previous snapshot.
  Chains are collapsed into a new full snapshot once they reach
  `MaxIncrementalChainLength` (default 10) to keep traversal cost bounded.

### 3. Snapshots are compressed by default

`SnapshotCompressionService` applies GZip compression to snapshot data above
the `MinimumSizeThreshold` (default 512 bytes).  The compression level and
threshold are configurable via `SnapshotCompressionOptions`.

### 4. `Version` is stored and restored as `long`

Snapshot `Version` maps directly to `AggregateRoot.Version` which is a `long`.
Any persistence or log model that records this field **must** use `long`, not
`int`, to avoid `OverflowException` on aggregates with more than
2 147 483 647 events.

### 5. Checksum integrity verification

Every snapshot stores a SHA-256 checksum computed over `AggregateId`, `Version`,
`AggregateType`, and `AggregateData`.  Restoration validates the checksum before
applying the snapshot to detect storage corruption early.

## Consequences

- Rehydration cost is bounded to O(events since last snapshot) rather than
  O(total lifetime events).
- Storage overhead is proportional to snapshot frequency and aggregate state size,
  partially offset by compression.
- Incremental snapshot chains must be collapsed periodically; the
  `IncrementalSnapshotChain.ShouldCollapse` guard enforces this automatically.
