# CLAUDE.md

CQRS + Event Sourcing framework in C# (.NET 10) with a banking `Account` aggregate as the reference domain; all default stores are in-memory.

## Build

- SDK: .NET 10.0.100 (`global.json`, rollForward latestMinor). Solution file: `dotnet-cqrs-eventsourcing.slnx`.
- `make build` / `dotnet build` (Debug), `make build-release` / `dotnet build -c Release`
- `make run` / `dotnet run` - runs the CLI host (`Program.cs`)
- `make pack` - NuGet package to `./nupkg`
- `make docker-build`, `make docker-run`, `make docker-down` - Docker / docker-compose stack
- `make examples` - runs examples 01-03

## Test

- `make test` or `dotnet test --no-build --verbosity normal` (after build); `dotnet test` also works directly.
- Test project: `tests/dotnet-cqrs-eventsourcing.Tests/` - xUnit 2.9, FluentAssertions 8, Moq.
- Test files mirror source layout (`Domain/`, `Application/`, `Infrastructure/`) and end in `*Tests.cs`.
- Benchmarks: `dotnet-cqrs-eventsourcing.Benchmarks/` (BenchmarkDotNet, `dotnet run -c Release` there).

## Lint / Format

- `make format` -> `dotnet format`. Style from `.editorconfig`: 4-space indent, LF, UTF-8, final newline; 2 spaces for csproj/json/yml.
- `<Nullable>enable</Nullable>` everywhere; files start with `#nullable enable`.
- `make lint` calls `dotnet analyze`, which is not a real dotnet command - treat as broken.

## Key directories

- `Program.cs` - entry point: builds config (`appsettings.json` + env vars), DI via `services.AddCqrsFramework(configuration)`, registers `ICliCommand`s into `CliCommandRegistry`.
- `Domain/` - `AggregateRoot`, `AggregateRoots/Account.cs`, `Events/` (`DomainEvent`, `EventEnvelope`, `AccountEvents.cs`, `[EventName]` attribute), `ValueObjects/`, `Snapshots/`, `Sagas/`. No infrastructure dependencies.
- `Application/` - `Commands/`, `Queries/`, `Handlers/`, `Services/` (event store, event bus, `AccountService`), `Sagas/`, `Decorators/`, `Extensions/`.
- `ReadModels/` - `ProjectionEngine`, `ReadModelProjectionEngine`, `AccountProjector`, `IReadModelStore<T>` / `InMemoryReadModelStore`, dead-letter store and replay.
- `Infrastructure/` - `EventStore.cs`, `Cli/` (CLI commands), `Middleware/`, `Workers/`, `Caching/`, `Compression/`, `Idempotency/`, `Observability/`, `Utilities/`.
- `Presentation/Controllers/` - `BaseApiController`, Accounts/Events/Queries/Diagnostics/Health controllers.
- `Shared/` - `Results/`, `Exceptions/`, `Enums/`, `Constants/`, `Extensions/`.
- `Configuration/` - `DotnetCqrsEventsourcingOptions` (section `DotnetCqrsEventsourcing`), `appsettings.example.json`.
- `docs/ARCHITECTURE.md`, `docs/adr/` - architecture and decision records; `docs/*.md` per-class docs.
- `examples/01..07-*` - runnable sample projects.

## Conventions

- Root namespace `DotNetCqrsEventSourcing.<Layer>.<Folder>` (note casing differs from the project name).
- Layer dependency direction: Domain <- Application <- ReadModels/Infrastructure <- Presentation.
- Persistence is pluggable: implement `IEventRepository` / `IReadModelStore<T>`; defaults are `InMemory*`.
- Naming: `*Command`, `*Query`, `*Event` (past tense, e.g. `AccountCreatedEvent`), `*Projector`, `*Service`, `*Controller`, `I*` interfaces, `*Extensions` static helper classes, `*JsonExtensions` for serialization helpers, `*Validation` for guard/validation partials.
- Each source file carries the author header comment block; keep it when adding files.
- Commits: conventional prefixes (`docs:`, `chore:`, `feat:`, `fix:`).
