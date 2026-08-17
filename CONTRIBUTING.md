# Contributing to ModuleDock

Thanks for your interest in contributing. ModuleDock is a small, focused library - contributions that improve plugin discovery, validation, Generic Host integration or diagnostics are welcome.

## Before you start

- Search [existing issues](https://github.com/kearns2000/ModuleDock/issues) to avoid duplicate work.
- For large changes (new lifecycle models, API changes, architecture), open an issue first to discuss approach.
- Keep pull requests focused. One feature or fix per PR is easier to review.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Any editor (VS, VS Code, Rider)

## Getting started

```bash
git clone https://github.com/kearns2000/ModuleDock.git
cd ModuleDock
dotnet build
dotnet test
```

Run the sample (the host build publishes the marine plugin into 'plugins/marine-rating'):

```bash
dotnet run --project samples/QuoteHost --urls http://127.0.0.1:5088
```

## Project layout

```text
src/ModuleDock.Abstractions/   # Plugin author contracts
src/ModuleDock/                # Host loader, validation, Generic Host integration
tests/ModuleDock.Tests/
tests/ModuleDock.IntegrationTests/
tests/Fixtures/                # Published plugin fixtures
samples/QuoteHost/
```

## Making changes

### Bug fixes

1. Add a failing test that reproduces the bug.
2. Fix the issue in 'src/ModuleDock/' or 'src/ModuleDock.Abstractions/'.
3. Ensure 'dotnet test --configuration Release' passes.

### Public API changes

- Keep the public surface small; implementation types stay 'internal'.
- Plugin authors should only need 'ModuleDock.Abstractions' plus the host contract assembly.
- Update 'README.md' for any user-visible API change.
- Avoid breaking changes in patch/minor releases without discussion.

### Plugin loading

Changes to discovery, path validation or 'AssemblyLoadContext' sharing rules need tests for:

- manifest validation
- duplicate IDs and capabilities
- path traversal
- shared contract type identity
- private dependency isolation

Do not add runtime unloading, file-system watching or a sandbox. Version 1 is a startup-loaded, trusted-plugin host.

## Code guidelines

- Use nullable reference types; avoid suppressing null warnings without reason.
- The build treats warnings as errors - keep it that way.
- Use primary constructors for injected dependencies.
- Name cancellation tokens 'stopToken'.
- Do not log plugin configuration values or secrets.
- Preserve inner exceptions when wrapping plugin failures.
- Match existing naming and file structure.

## Testing expectations

All PRs should pass:

```bash
dotnet restore
dotnet build --configuration Release --no-restore
dotnet test --configuration Release --no-build
dotnet format --verify-no-changes
```

Add tests when you:

- Fix a bug
- Change validation or diagnostics
- Touch assembly loading or shared-type rules
- Change failure policy behaviour

Use temporary directories and clean them in 'Dispose'. Do not depend on machine-specific paths, network access or test order.

## Pull request checklist

- [ ] 'dotnet build --configuration Release' succeeds
- [ ] 'dotnet test --configuration Release' passes
- [ ] Tests added or updated for the change
- [ ] README updated if public API or behaviour changed
- [ ] No unrelated formatting or drive-by refactors

## Code of conduct

This project follows the [Code of Conduct](CODE_OF_CONDUCT.md). By participating, you agree to uphold it.

## Questions

Open a [GitHub issue](https://github.com/kearns2000/ModuleDock/issues) for questions or ideas.
