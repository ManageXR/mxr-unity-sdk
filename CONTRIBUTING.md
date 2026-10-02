# Contributing to ManageXR Unity SDK

Welcome! We appreciate your interest in contributing to the ManageXR Unity SDK. This guide will help you get started.

## How to Contribute

- **Bug Reports**: Open an issue with a clear description, steps to reproduce, and expected vs actual behavior
- **Feature Requests**: Open an issue describing the use case and proposed solution
- **Pull Requests**: Fork the repo, create a branch, make changes, and submit a PR
- **Questions**: Open a discussion or issue for general questions

## Development Setup

1. **Clone the repository**
   ```bash
   git clone https://github.com/your-fork/mxr-unity-sdk.git
   cd mxr-unity-sdk
   ```

2. **Open in Unity** (Unity 2019.4 or later)
   - Open the project root folder in Unity Editor

3. **Import samples** (optional)
   - Go to Package Manager > ManageXR Unity SDK > Samples > Import

## Branches

- **`v2`** — V2 work (protocol, Admin App integration). Open PRs here until V2 ships.
- **`master`** — v1.x shipping line. Open PRs here for v1.x fixes and features.

`v2` will merge into `master` when V2 is ready. Until that merge, keep the two lines separate: do not send V2 protocol changes to `master`, and do not send v1.x-only work to `v2`.

## Testing

Protocol tests (framing, envelope, handshake, guarantee rule, subscriptions, diagnostics) are plain NUnit and need no Unity install:

```bash
dotnet test dotnet/MXR.SDK.Protocol.sln
```

<details>
<summary>Filter a subset of protocol tests</summary>

`--filter` is a substring match on the fully qualified test name. Class-name fragments:

`FrameCodec` · `EnvelopeModel` · `EnvelopeCodec` · `GuaranteeRule` · `AckFactory` · `AckTracker` · `ReplyTracker` · `SubscriptionRegistry` · `HandshakeNegotiator` · `ConnectionDiagnostics`

```bash
dotnet test dotnet/MXR.SDK.Protocol.sln --filter HandshakeNegotiator
dotnet test dotnet/MXR.SDK.Protocol.sln --filter FullyQualifiedName~FrameCodecTests
dotnet test dotnet/MXR.SDK.Protocol.sln --filter Name~SetIncompatible
dotnet test dotnet/MXR.SDK.Protocol.sln --filter "FullyQualifiedName~AckTracker|FullyQualifiedName~ReplyTracker"
dotnet test dotnet/MXR.SDK.Protocol.sln --list-tests
dotnet test dotnet/MXR.SDK.Protocol.sln --configuration Release
```

`Name~` matches the method name only. `|` is OR, `&` is AND. `--configuration Release` matches CI.

</details>

This suite is a **PR gate**: a failing **Protocol tests** GitHub Actions check blocks merge. The tests need the .NET 8 SDK only; no Unity install or Unity license.

Editor-mode tests under `Assets/MXR.SDK/Tests/Editor/` still need Unity:

1. Open the project in Unity 2019.4+
2. Open Window > General > Test Runner
3. Run EditMode tests

Please confirm protocol tests pass before submitting PRs that touch `Assets/MXR.SDK/Protocol/`.

## Code Style

- Follow existing patterns and conventions in the codebase
- Keep changes focused and minimal
- No major refactors without prior discussion in an issue

## PR Process

1. **Fork** the repository
2. **Create a branch** from `v2` (V2 work) or `master` (v1.x) with a descriptive name (e.g., `fix/null-reference`, `feat/new-api`)
3. **Make your changes** with clear, atomic commits
4. **Run all tests** to ensure nothing is broken. Protocol tests (`dotnet test`) are the PR gate; a red **Protocol tests** check blocks merge.
5. **Submit a PR** against the same base: `v2` for V2 work, `master` for v1.x. After `v2` merges into `master`, new work targets `master`.

## Code of Conduct

- Be respectful and constructive in all interactions
- Welcome newcomers and help them get started
- Focus on the technical merits of contributions
- Assume good intent from other contributors

Thank you for contributing!
