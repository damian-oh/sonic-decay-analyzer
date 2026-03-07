# CLAUDE.md - Sonic Decay Analyzer

## Core Concept

This is **not** a tuner app. It is an acoustic degradation analysis engine that quantifies string wear through timbre fingerprinting (spectral centroid, high-frequency energy ratio, time-series decay) rather than pitch detection.

**Build**: `dotnet build src/SonicDecay.App/SonicDecay.App.csproj`

---

## Tech Stack

```
Frontend:  .NET MAUI (C#) - strict MVVM
Backend:   Native C# DSP via FftSharp (MIT, .NET Standard) - 48kHz/24-bit PCM
Database:  SQLite via sqlite-net-pcl
Charts:    LiveCharts2 (SkiaSharp)
Legacy:    Python engine (src/SonicDecay.Engine/) - desktop fallback/reference only
Tests:     C#: xUnit + Moq + FluentAssertions | Python: pytest
```

---

## Database Schema

5 tables, 3-tier relational model. Foreign keys and cascading deletes managed explicitly in Repository layer (no ORM extensions).

### Relationships

```
Guitar (1) ────< GuitarStringSetPairing (*) >──── StringSet (1)
                                                        |
                                          StringBaseline (6) ────< MeasurementLog (*)
```

### Key Constraints

- **One active pairing per guitar**: Only one `GuitarStringSetPairing` per guitar can have `IsActive = true`
- **Cascading deletes** (manual in Repository layer):
  - Guitar delete -> cascades to its Pairings
  - StringSet delete -> cascades to its Baselines and Pairings
  - StringBaseline delete -> cascades to its MeasurementLogs
- **Indexed FKs**: SetId, BaselineId, GuitarId
- **Thread safety**: All DB operations are async via `IDatabaseService`

---

## Architectural Standards

### Principles

1. **SOLID** (especially Interface Segregation)
2. **Clean Architecture** (separation of concerns)
3. **Repository Pattern** (explicit data access layer, no EF Core)
4. **MVVM Pattern** (strict: View <-> ViewModel <-> Model, no business logic in Views/code-behind)

### Naming Conventions

- **Interfaces**: `I` prefix (e.g., `IDatabaseService`)
- **Services**: `Service` suffix (e.g., `AudioCaptureService`)
- **Repositories**: `Repository` suffix (e.g., `StringSetRepository`)
- **ViewModels**: `ViewModel` suffix (e.g., `MainViewModel`)

### Documentation

- All public methods must have XML documentation comments
- Complex algorithms require inline mathematical notation
- Use precise acoustical terminology ("Spectral Centroid" not "average frequency")

### UI Patterns

- `BaseViewModel.ShowError(msg, timeout)` with auto-clear for error display
- `AsyncRelayCommand` supports `Action<Exception>? onError` callback
- Health status uses unicode symbols for color-blind accessibility
- Color resources centralized in `Colors.xaml`; reusable styles (`CardFrame`, `EmptyStateFrame`) in `Styles.xaml`

---

## Forbidden Patterns

**Do NOT**:
- Use EF Core or high-level ORMs (violates architectural transparency)
- Mix business logic in Views or Code-behind
- Implement pitch detection (this is timbre analysis only)
- Use synchronous file I/O in UI thread
- Hard-code string gauges (must be user-configurable)

**Do**:
- Maintain strict MVVM separation
- Use async/await for all I/O operations
- Implement proper IDisposable for audio resources
- Validate all user inputs before database writes
- Log all analysis engine errors to diagnostic stream

---

## Error Handling

- **Audio Capture**: Graceful degradation to lower sample rates
- **Analysis Engine**: Timeout detection with cancellation token support
- **Database**: Transactional integrity with retry logic
- **UI**: Non-blocking async operations with loading indicators

---

## Testing

**C#** (`tests/SonicDecay.App.Tests/`): WindowFunctions, FftProcessor, SpectralMetrics tests
**Python** (`src/SonicDecay.Engine/tests/`): FFT pipeline + metric calculation tests

### Algorithm Validation Tolerances
- Centroid: +/-1 Hz | HF Energy Ratio: +/-0.001 | Decay Percentage: +/-0.1%

### Remaining Coverage
- Repository CRUD, ViewModel state transitions, service isolation
- End-to-end audio capture -> analysis -> persistence
- Cross-platform permission handling

---

## Performance Targets

- **Audio Latency**: <100ms from trigger to analysis start
- **Analysis Speed**: <50ms for FFT + metric extraction
- **Database Writes**: <10ms per measurement log entry
- **UI Responsiveness**: 60fps during real-time updates

---

## Current Development Context

**Status**: Feature-complete. Production hardening phase.

**Completed**: Real-time spectral analysis, guitar/string management with pairings, decay chart visualization, predictive replacement recommendations, platform-specific audio capture, UI/UX polish (error handling, onboarding, notifications, delete confirmations)

**Remaining**:
- Expand C# unit tests (repositories, ViewModels, service layer)
- Integration tests for audio -> analysis -> persistence pipeline
- Performance profiling and optimization
- Production deployment preparation
