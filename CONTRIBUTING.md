# Contributing

Thank you for considering contributing! Every contribution — bug reports, feature suggestions, documentation improvements, or code — is welcome.

## Requirements

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Git

## Building Locally

```bash
# Clone the repository
git clone https://github.com/your-username/dotnet-cqrs-eventsourcing.git
cd dotnet-cqrs-eventsourcing

# Restore dependencies
dotnet restore

# Build in release mode
dotnet build --configuration Release

# Or use the Makefile shortcut
make build
```

## Running Tests

```bash
# Run all tests
dotnet test --configuration Release --verbosity normal

# Run with detailed output and save results
dotnet test --configuration Release --verbosity normal --logger "trx;LogFileName=test-results.trx"

# Run a specific test project
dotnet test tests/dotnet-cqrs-eventsourcing.Tests/ --configuration Release
```

## Running Examples

```bash
# Run a specific example
dotnet run --project examples/01-BasicAccount/

# See all available examples
ls examples/
```

## Making Changes

1. Fork the repository on GitHub.
2. Clone your fork: `git clone https://github.com/your-username/dotnet-cqrs-eventsourcing.git`
3. Create a feature branch: `git checkout -b feature/my-feature`
4. Make your changes and add or update tests as needed.
5. Ensure all tests pass: `dotnet test`
6. Commit your changes with a descriptive message.
7. Push the branch and open a Pull Request against `main`.

## Pull Request Guidelines

- Keep PRs focused — one logical change per PR.
- Update documentation and examples if your change affects public APIs.
- All CI checks must pass before a PR can be merged.
- Provide a clear description of what changed and why.
- Reference any related issues using `Fixes #<issue-number>` or `Closes #<issue-number>`.

## Code Style

- Follow the `.editorconfig` rules in the repository root.
- Use 4 spaces for indentation; no tabs.
- Provide XML documentation comments for all public types and members.
- Keep any existing author headers intact — do not remove them.
- Prefer explicit types over `var` for non-obvious return types.

## Issues and Bug Reports

Use GitHub Issues to report bugs or request features. When filing a bug, include:

- .NET version (`dotnet --version`)
- A minimal reproduction (code snippet or project)
- Expected vs. actual behaviour

## License

By contributing, you agree that your contributions will be licensed under the [MIT License](LICENSE).
