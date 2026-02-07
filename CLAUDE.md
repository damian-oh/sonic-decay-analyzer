# CLAUDE.md - Sonic Decay Analyzer

## Project Identity

**Name:** Sonic Decay Analyzer
**Philosophy:** "I focus on the logic behind the art. I build architectures that streamline complexity."
**Mission:** Replace subjective guitar string assessment with objective spectral data analysis to predict optimal replacement cycles.

---

## Core Concept

This is **not** a tuner app. This is an **acoustic degradation analysis engine** that quantifies string wear through timbre fingerprinting rather than pitch detection.

### Key Scientific Principles

- **Spectral Centroid**: Tracks the "center of mass" of the frequency spectrum to measure tonal darkening as strings age
- **High-Frequency Energy Ratio**: Measures amplitude ratio of upper harmonics (5kHz-15kHz) against fundamental frequency (f₀)
- **Time-Series Analysis**: Compares current measurements against baseline "fresh string" signatures to calculate decay rates

---

## Technical Stack & Architecture

### Platform Architecture
```
Frontend:  .NET MAUI (C#) - MVVM Pattern (strict)
Backend:   Native C# DSP via FftSharp (cross-platform)
Database:  SQLite via sqlite-net-pcl
Legacy:    Python engine retained for reference/desktop debugging
```

### Directory Structure
```
SonicDecayAnalyzer/
├── src/
│   ├── SonicDecay.App/          # C# MAUI Frontend
│   │   ├── Models/              # Data entities (5 models)
│   │   ├── ViewModels/          # MVVM business logic (9 ViewModels + RelayCommand)
│   │   ├── Views/               # XAML layouts (8 pages)
│   │   ├── Converters/          # Value converters (14 classes)
│   │   │   └── ValueConverters.cs
│   │   ├── Services/
│   │   │   ├── Interfaces/      # Service contracts (14 interfaces)
│   │   │   ├── Implementations/ # Service implementations (15 classes)
│   │   │   └── Spectral/        # Native C# DSP (FftSharp)
│   │   │       ├── WindowFunctions.cs
│   │   │       ├── FftProcessor.cs
│   │   │       └── SpectralMetrics.cs
│   │   ├── Platforms/           # iOS/Android/Windows/MacCatalyst audio capture
│   │   ├── Resources/Styles/    # Colors.xaml, Styles.xaml
│   │   ├── App.xaml(.cs)
│   │   ├── AppShell.xaml(.cs)
│   │   ├── MauiProgram.cs       # DI + service registration
│   │   └── GlobalXmlns.cs       # Global XAML namespace registration
│   │
│   ├── SonicDecay.Engine/       # Python DSP (reference/desktop fallback)
│   │   ├── analysis.py          # FFT & metric extraction
│   │   ├── spectral.py          # Metric calculations
│   │   ├── cli.py               # CLI interface
│   │   ├── server.py            # Server mode for process pooling
│   │   ├── requirements.txt     # NumPy, SciPy dependencies
│   │   └── tests/               # Algorithm validation (pytest)
│   │
│   └── SonicDecay.Data/         # SQL reference scripts
│       ├── Schema.sql
│       └── SeedData.sql
│
└── tests/
    └── SonicDecay.App.Tests/    # xUnit + Moq + FluentAssertions
        └── Services/Spectral/   # WindowFunctions, FftProcessor, SpectralMetrics tests
```

---

## Database Schema

SQLite via sqlite-net-pcl with 5 tables in a 3-tier relational model. Foreign keys and cascading deletes are managed explicitly in the Repository layer (no ORM extensions).

### Tables

| Table | Purpose | Key Fields |
|-------|---------|------------|
| **Guitars** | Instrument registry | Name, Make, Model, Type (Electric/Acoustic/Classical/Bass), Notes |
| **StringSets** | String metadata | Brand, Model, individual gauges (GaugeE1-E6) |
| **GuitarStringSetPairings** | Junction: which strings are on which guitar | GuitarId (FK), SetId (FK), InstalledAt, RemovedAt, IsActive |
| **StringBaselines** | Fresh-string spectral fingerprint per string | SetId (FK), StringNumber (1-6), FundamentalFreq, InitialCentroid, InitialHighRatio |
| **MeasurementLogs** | Time-series decay data | BaselineId (FK), CurrentCentroid, CurrentHighRatio, DecayPercentage, PlayTimeHours, Note |

### Relationships

```
Guitar (1) ────< GuitarStringSetPairing (*) >──── StringSet (1)
                                                        │
                                          StringBaseline (6) ────< MeasurementLog (*)
```

### Key Constraints

- **One active pairing per guitar**: Only one `GuitarStringSetPairing` per guitar can have `IsActive = true`
- **Cascading deletes** (manual in Repository layer):
  - Guitar delete → cascades to its Pairings
  - StringSet delete → cascades to its Baselines and Pairings
  - StringBaseline delete → cascades to its MeasurementLogs
- **Indexed FKs**: SetId, BaselineId, GuitarId are indexed for query performance
- **Thread safety**: All DB operations are async via `IDatabaseService`

---

## Code Quality Standards

### Architectural Principles

1. **SOLID Principles** (especially Interface Segregation)
2. **Clean Architecture** (separation of concerns)
3. **Repository Pattern** (explicit data access layer)
4. **MVVM Pattern** (strict separation: View ↔ ViewModel ↔ Model)

### Naming Conventions

- **Interfaces**: Prefix with `I` (e.g., `IDatabaseService`)
- **Services**: Suffix with `Service` (e.g., `AudioCaptureService`)
- **Repositories**: Suffix with `Repository` (e.g., `StringSetRepository`)
- **ViewModels**: Suffix with `ViewModel` (e.g., `MainViewModel`)

### Documentation Requirements

- All public methods must have XML documentation comments
- Complex algorithms require inline mathematical notation
- Use precise acoustical terminology (not casual language)

---

## Implemented Services & Components

### Service Layer (14 Interfaces, 15 Implementations)

| Interface | Implementation | Purpose |
|-----------|----------------|---------|
| `IDatabaseService` | `DatabaseService` | SQLite connection singleton |
| `IPermissionService` | `PermissionService` | Microphone access |
| `IAudioCaptureService` | `AudioCaptureService` | 48kHz PCM capture |
| `IAnalysisService` | `NativeAnalysisService` | Primary C# FFT analysis |
| `IAnalysisService` | `AnalysisService` | Python fallback (desktop) |
| `IMeasurementService` | `MeasurementService` | Analysis + DB coordination |
| `IRecommendationService` | `RecommendationService` | Decay prediction |
| `INotificationService` | `NotificationService` | Success alerts via DisplayAlert |
| `IPythonEnginePool` | `PythonEnginePool` | Python subprocess pooling |
| `IStringSetRepository` | `StringSetRepository` | StringSet CRUD |
| `IStringBaselineRepository` | `StringBaselineRepository` | Baseline CRUD |
| `IMeasurementLogRepository` | `MeasurementLogRepository` | MeasurementLog CRUD |
| `IGuitarRepository` | `GuitarRepository` | Guitar CRUD |
| `IGuitarStringSetPairingRepository` | `GuitarStringSetPairingRepository` | Pairing CRUD |
| `ISeedDataService` | `SeedDataService` | Preset string data |

### Value Converters (14 Classes in ValueConverters.cs)

| Converter | Purpose |
|-----------|---------|
| `InvertedBoolConverter` | Boolean inversion |
| `StringNotEmptyConverter` | String validation |
| `IntToBoolConverter` | Integer to boolean |
| `StringNumberIndexConverter` | 1-6 to 0-5 index conversion |
| `CaptureButtonTextConverter` | Start/Stop button labels |
| `DecayToColorConverter` | Health status colors (green/yellow/red) |
| `SaveButtonTextConverter` | Save/Update button labels |
| `NotNullConverter` | Null-state visibility |
| `PresetToColorConverter` | Gauge preset button backgrounds |
| `PresetToTextColorConverter` | Gauge preset button text |
| `BoolToColorConverter` | Boolean-based styling |
| `BoolToTextColorConverter` | Boolean-based text styling |
| `IsNotNullConverter` | Non-null visibility (extends NotNullConverter) |
| `ExpandChevronConverter` | Collapse/expand chevron |

### UI Patterns

- `BaseViewModel` has `ShowError(msg, timeout)` with auto-clear and `ClearError()`
- `AsyncRelayCommand` supports `Action<Exception>? onError` callback
- Color resources centralized in `Colors.xaml` (chart, health, urgency, baseline themes)
- `CardFrame` and `EmptyStateFrame` reusable styles in `Styles.xaml`
- Health status uses unicode symbols for color-blind accessibility

---

## Audio Processing Specifications

### Capture Requirements
- **Sample Rate**: 48,000 Hz
- **Bit Depth**: 24-bit PCM
- **Buffer Size**: 4096 samples (85.3ms @ 48kHz)
- **Trigger**: RMS threshold crossing (adaptive)

### Analysis Pipeline
1. **Windowing**: Apply Hamming window to reduce spectral leakage
2. **FFT**: 8192-point transform (zero-padded from 4096)
3. **Metric Extraction**:
   - Centroid calculation across 20Hz-20kHz
   - HF ratio isolation at 5kHz-15kHz band
4. **Normalization**: Compensate for input gain variance

---

## Integration Constraints

### Spectral Analysis Implementation

**Primary: Native C# (NativeAnalysisService)**
- Cross-platform compatible (iOS, Android, Windows, macOS)
- Uses FftSharp library (MIT license, .NET Standard)
- No external dependencies or subprocess management
- Registered as default `IAnalysisService` in DI

**Fallback: Python Subprocess (AnalysisService)**
- Desktop-only (Windows, macOS)
- JSON-based message passing via CLI
- Useful for debugging and reference validation
- Swap via `MauiProgram.cs` DI registration

### Error Handling Strategy

- **Audio Capture**: Graceful degradation to lower sample rates
- **Analysis Engine**: Timeout detection with cancellation token support
- **Database**: Transactional integrity with retry logic
- **UI**: Non-blocking async operations with loading indicators

---

## Testing

### Existing Tests

**C# (xUnit + Moq + FluentAssertions)** in `tests/SonicDecay.App.Tests/`:
- `WindowFunctionsTests` - Window function validation
- `FftProcessorTests` - FFT computation tests
- `SpectralMetricsTests` - Centroid, HF ratio, decay metric tests

**Python (pytest)** in `src/SonicDecay.Engine/tests/`:
- `test_analysis.py` - FFT pipeline tests
- `test_spectral.py` - Metric calculation tests

### Remaining Test Coverage

- Repository CRUD operations
- ViewModel state transitions
- Service layer isolation tests
- End-to-end audio capture → analysis → persistence
- Cross-platform permission handling

### Algorithm Validation Tolerances
- Centroid: ±1 Hz
- HF Energy Ratio: ±0.001
- Decay Percentage: ±0.1%

---

## Performance Targets

- **Audio Latency**: <100ms from trigger to analysis start
- **Analysis Speed**: <50ms for FFT + metric extraction
- **Database Writes**: <10ms per measurement log entry
- **UI Responsiveness**: 60fps during real-time updates

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

## Git Commit Standards

### Format

Conventional Commits: `<type>(<scope>): <description>`

### Types

- **feat**: New feature
- **fix**: Bug fix
- **docs**: Documentation only
- **style**: Code style/formatting
- **refactor**: Code restructuring
- **test**: Test additions/corrections
- **chore**: Build/tooling changes

### Scopes

- **App**: Changes in `SonicDecay.App/` (ViewModels, Views, Services)
- **Engine**: Changes in `SonicDecay.Engine/` (Python analysis logic)
- **Data**: Changes in `SonicDecay.Data/` (Schema, seed data)
- **Models**: Changes to entity definitions
- **Platforms**: Platform-specific code (iOS/Android/Windows)
- **Docs**: Documentation files

### Body Guidelines

- Explain **why** the change was made, not **what** (the diff shows what)
- Reference architectural decisions from CLAUDE.md when relevant
- Note any breaking changes or migration requirements

---

## Communication Guidelines

When working with this project:

1. **Use precise terminology**: "Spectral Centroid" not "average frequency"
2. **Cite constraints**: "Per the repository pattern requirement..."
3. **Question assumptions**: If something seems to violate SOLID, ask
4. **Provide rationale**: Explain architectural decisions with reference to project philosophy

---

## Current Development Context

**Project Status**: Feature-complete. Production hardening phase.
**Build**: `dotnet build src/SonicDecay.App/SonicDecay.App.csproj`

**Completed Features**:
- Real-time spectral analysis with native C# DSP
- Guitar and string set management with pairing system
- Decay trend visualization with LiveCharts2
- Predictive replacement recommendations via linear regression
- Collapsible context section with summary display
- Platform-specific audio capture (iOS/Android/Windows/macOS)
- UI/UX polish: error handling, onboarding, notifications, delete confirmations, card styles

**Remaining Work**:
- Expand C# unit tests (repositories, ViewModels, service layer)
- Integration tests for audio → analysis → persistence pipeline
- Performance profiling and optimization
- Production deployment preparation

**Architectural Decisions Made**:
- 48kHz sample rate for headroom in harmonic analysis
- RMS threshold over zero-crossing for trigger reliability
- Native C# DSP via FftSharp (MIT, .NET Standard) for cross-platform support
- Python engine retained as desktop fallback and reference implementation
- LiveCharts2 (SkiaSharp) for cross-platform chart rendering
- Guitar-StringSet pairing via junction table with single active constraint

---

## Project Tone & Philosophy

This is a **hyper-rational engineering tool** for musicians who think like engineers. The user base values:
- Objectivity over subjectivity
- Quantifiable metrics over "feel"
- Predictive maintenance over reactive replacement
- Architectural clarity over rapid prototyping

Code should reflect **GTA Industry-ready** standards: production-grade, maintainable, and intellectually rigorous.

---

*Last Updated: 2026-02-06*
*Maintained by: Project Architect*
*For AI Assistant Context: This document defines the complete technical contract for the Sonic Decay Analyzer project.*
