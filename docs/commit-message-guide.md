# Commit Message Guide

## Format

```
type(scope): subject

Optional body explaining "why" not "what".

Co-Authored-By: Claude Opus 4.6 <noreply@anthropic.com>
```

## Types

| Type | Purpose | Example |
|------|---------|---------|
| `feat` | New feature or capability | `feat(app): add inline string selector to DecayChartPage` |
| `fix` | Bug fix | `fix(app): guard against concurrent analysis in OnBufferCaptured` |
| `docs` | Documentation only | `docs: add MIT license to the project` |
| `chore` | Maintenance, cleanup, tooling | `chore(app): remove dead SonicDecay.App.Pages namespace registration` |
| `test` | Adding or updating tests | `test(app): add spectral centroid edge case tests` |
| `refactor` | Code restructuring without behavior change | `refactor(app): extract audio buffer processing into helper` |
| `perf` | Performance improvement | `perf(app): reduce FFT allocation in hot path` |

## Scopes

| Scope | Applies To |
|-------|-----------|
| `app` | `src/SonicDecay.App/` (main application code) |
| `platforms` | Platform-specific code (`Platforms/Android/`, `Platforms/iOS/`, etc.) |
| `claude` | `CLAUDE.md` changes |
| `readme` | `README.md` changes |

Scope is optional for global or cross-cutting changes (e.g., `docs: add MIT license to the project`).

## Rules

1. Use **imperative mood** in the subject ("add feature" not "added feature")
2. Start with **lowercase** after the colon
3. No trailing **period** in the subject line
4. Keep the subject line to **50-60 characters** when possible
5. Use the body to explain **why** the change was made, not what changed
6. Separate subject from body with a **blank line**

## Good Examples

### Features
```
feat(app): add inline string selector to DecayChartPage
feat(app): add delete with cascade warnings to list pages
feat(app): add auto-clearing error messages to BaseViewModel
feat(app): centralize chart, urgency, and baseline colors in Colors.xaml
```

### Bug Fixes
```
fix(app): guard against concurrent analysis in OnBufferCaptured
fix(app): add null guard for SelectedStringSet in chart navigation
fix(app): correct inverted HF ratio trend calculation
fix(platforms): fix audio capture state management on iOS, macOS, and Windows
```

### Documentation & Maintenance
```
docs: add MIT license to the project
docs(claude): update documentation to reflect the updated project status
chore(app): remove dead SonicDecay.App.Pages namespace registration
```

## Anti-Patterns

- `Update code` - too vague, no type or scope
- `Fixed the bug in the thing` - past tense, non-specific
- `feat: Added new feature for doing stuff.` - past tense, trailing period, vague
- `WIP` - should not be committed; use branches instead
- `misc changes` - every commit should have a clear purpose
