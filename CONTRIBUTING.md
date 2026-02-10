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

## Testing

Due to Unity's licensing constraints, tests must be run locally:

1. Open the project in Unity 2019.4+
2. Open Window > General > Test Runner
3. Run all EditMode tests

Please confirm tests pass before submitting PRs.

## Code Style

- Follow existing patterns and conventions in the codebase
- Keep changes focused and minimal
- No major refactors without prior discussion in an issue

## PR Process

1. **Fork** the repository
2. **Create a branch** from `master` with a descriptive name (e.g., `fix/null-reference`, `feat/new-api`)
3. **Make your changes** with clear, atomic commits
4. **Run all tests** to ensure nothing is broken
5. **Submit a PR** against `master` with a clear description of the changes

## Code of Conduct

- Be respectful and constructive in all interactions
- Welcome newcomers and help them get started
- Focus on the technical merits of contributions
- Assume good intent from other contributors

Thank you for contributing!
