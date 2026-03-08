# Coding Standards

## Architecture Layers

| Layer | Directory | Responsibility |
|-------|-----------|---------------|
| Models | `Models/` | Entity classes with SQLite attributes |
| Views | `Views/` | XAML pages with minimal code-behind |
| ViewModels | `ViewModels/` | UI state, commands, data binding |
| Services | `Services/Interfaces/`, `Services/Implementations/` | Business logic and platform abstractions |
| Repositories | `Services/Implementations/*Repository.cs` | Data access (one per entity) |
| Spectral | `Services/Spectral/` | DSP algorithms (FFT, windowing, metrics) |

## MVVM Pattern

### BaseViewModel

All ViewModels inherit from `BaseViewModel`, which provides:

- **`SetProperty<T>(ref field, value)`** - Sets a backing field and raises `PropertyChanged` only when the value changes. Accepts an optional `onChanged` callback.
- **`IsBusy`** - Boolean for loading indicators.
- **`Title`** - Page title for navigation display.
- **`ErrorMessage`** - Current error message, auto-cleared by `ShowError`.
- **`ShowError(message, dismissAfterMs)`** - Displays an error that auto-clears after a timeout (default 5000ms). Uses equality check to avoid clearing a newer error.

### Command Types

- **`RelayCommand`** - Synchronous command with optional `canExecute` predicate.
- **`RelayCommand<T>`** - Typed parameter variant.
- **`AsyncRelayCommand`** - Async command with double-execution prevention (`_isExecuting` guard). Accepts optional `Action<Exception> onError` callback; unhandled exceptions are routed to the callback or logged to `Debug.WriteLine`.
- **`AsyncRelayCommand<T>`** - Typed parameter async variant.

## Naming Conventions

| Element | Convention | Example |
|---------|-----------|---------|
| Interfaces | `I` prefix | `IDatabaseService` |
| Services | `Service` suffix | `AudioCaptureService` |
| Repositories | `Repository` suffix | `StringSetRepository` |
| ViewModels | `ViewModel` suffix | `MainViewModel` |
| Private fields | `_camelCase` | `_isExecuting` |
| Async methods | `Async` suffix | `InitializeAsync()` |
| Constants | `PascalCase` | `SampleRate` |
| Test methods | `MethodName_Scenario_ExpectedResult` | `Compute_WithValidSignal_ReturnsCentroid` |

## Dependency Injection

All dependencies are injected via constructor with null-coalescing throw:

```csharp
public MyService(IDependency dep)
{
    _dep = dep ?? throw new ArgumentNullException(nameof(dep));
}
```

### Registration (in `MauiProgram.cs`)

| Lifetime | Used For |
|----------|----------|
| Singleton | `IDatabaseService`, `ISeedDataService`, `IPermissionService`, `IAudioCaptureService`, `IAnalysisService`, `INotificationService`, `IPythonEnginePool` |
| Transient | All Repositories, `IMeasurementService`, `IRecommendationService`, all ViewModels, all Views |

## Repository Pattern

- One repository per entity (5 total: Guitar, StringSet, StringBaseline, MeasurementLog, GuitarStringSetPairing)
- All operations are async (`Task<T>` return types)
- Each method calls `await InitializeAsync()` to ensure table creation
- Manual cascade deletes (no ORM magic) - see [database-schema.md](database-schema.md)
- No EF Core or high-level ORMs

## Documentation

- All public methods require XML documentation comments (`/// <summary>`)
- Use precise acoustical terminology ("Spectral Centroid" not "average frequency")
- Complex algorithms include inline mathematical notation in comments

## Error Handling

- **Audio capture**: Graceful degradation to lower sample rates if preferred rate unavailable
- **Analysis engine**: Timeout detection with `CancellationToken` support
- **Database**: Async operations with retry logic for transactional integrity
- **UI**: Non-blocking operations; errors displayed via `BaseViewModel.ShowError()` with auto-clear

## Testing

- **Framework**: xUnit + FluentAssertions + Moq
- **Test naming**: `MethodName_Scenario_ExpectedResult`
- **Algorithm validation tolerances**:
  - Spectral Centroid: +/- 1 Hz
  - HF Energy Ratio: +/- 0.001
  - Decay Percentage: +/- 0.1%

## Forbidden Patterns

- No EF Core or high-level ORMs
- No business logic in Views or code-behind
- No pitch detection (this is timbre analysis only)
- No synchronous file I/O on the UI thread
- No hard-coded string gauges (must be user-configurable)

See [CLAUDE.md](../CLAUDE.md) for the complete architectural contract.
