Projection rebuild from scratch with catch-up/live switchover

Add first-class rebuild: reset a projection's checkpoint and read model, replay history in batches, and atomically switch from catch-up to live subscription without missing or double-processing events between the last replayed position and the live head. Expose as a CLI command and document the cutover algorithm in ARCHITECTURE.md.