# Contributing

Thanks for contributing! Bug reports, feature ideas, documentation, and code are all welcome.

## Prerequisites

- .NET SDK 10.0 (projects target `net10.0`; `global.json` pins 10.0.100 and allows newer patch releases).
- Git.

## Building

From the repository root:

```bash
dotnet build
```

For an optimized build, use `dotnet build -c Release`.

## Running tests

```bash
dotnet test
```

Tests use xUnit, FluentAssertions, and Moq. Test files mirror the source layout and end in `Tests.cs`.

## Coding conventions

- 4-space indentation, LF line endings, UTF-8. `.editorconfig` is authoritative.
- File-scoped namespaces (`namespace DotNetCqrsEventSourcing.Domain.Events;`).
- Nullable reference types are enabled. Keep nullability annotations accurate.
- CQRS naming:
  - Commands: `*Command` (for example, `CreateAccountCommand`).
  - Queries: `*Query`.
  - Handlers: `*Handler`.
  - Events: `*Event`, past tense (for example, `AccountCreatedEvent`).
- Keep the layer dependency direction: Domain <- Application <- ReadModels/Infrastructure <- Presentation.

## Commit messages

- Use the imperative mood with a short subject line (about 72 characters or fewer), for example `Add account close command`.
- Conventional prefixes such as `feat:`, `fix:`, `docs:`, and `chore:` are used in this repository.

## Pull request process

1. Branch from `main`.
2. Keep each PR focused on one logical change.
3. Make sure CI passes. It builds and runs the tests in Release mode.
4. Describe what changed and why, and link any related issue.

## Issues

Use GitHub Issues to report bugs or request features. Include a minimal reproduction and your .NET SDK version.

By contributing, you agree that your work will be licensed under the [MIT License](LICENSE).
