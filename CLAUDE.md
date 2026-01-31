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
│   │   │   ├── Guitar.cs
│   │   │   ├── GuitarStringSetPairing.cs
│   │   │   ├── StringSet.cs
│   │   │   ├── StringBaseline.cs
│   │   │   └── MeasurementLog.cs
│   │   │
│   │   ├── ViewModels/          # MVVM business logic (9 classes)
│   │   │   ├── BaseViewModel.cs
│   │   │   ├── MainViewModel.cs
│   │   │   ├── DecayChartViewModel.cs
│   │   │   ├── LibraryViewModel.cs
│   │   │   ├── GuitarsListViewModel.cs
│   │   │   ├── GuitarInputViewModel.cs
│   │   │   ├── StringSetsListViewModel.cs
│   │   │   ├── StringInputViewModel.cs
│   │   │   ├── PairingsManagementViewModel.cs
│   │   │   └── RelayCommand.cs
│   │   │
│   │   ├── Views/               # XAML layouts (8 pages)
│   │   │   ├── MainPage.xaml           # Real-time analyzer interface
│   │   │   ├── DecayChartPage.xaml     # LiveCharts2 trend visualization
│   │   │   ├── LibraryPage.xaml        # Navigation hub
│   │   │   ├── GuitarsListPage.xaml    # Guitar list
│   │   │   ├── GuitarInputPage.xaml    # Guitar add/edit form
│   │   │   ├── StringSetsListPage.xaml # String set list
│   │   │   ├── StringInputPage.xaml    # String set add/edit
│   │   │   └── PairingsManagementPage.xaml
│   │   │
│   │   ├── Converters/          # Value converters (15 classes)
│   │   │   └── ValueConverters.cs
│   │   │
│   │   ├── Services/            # Audio capture, DB access
│   │   │   ├── Interfaces/      # Service contracts (13 interfaces)
│   │   │   ├── Implementations/ # Service implementations (13 classes)
│   │   │   └── Spectral/        # Native C# DSP (FftSharp)
│   │   │       ├── WindowFunctions.cs   # Hamming/Hann/Blackman
│   │   │       ├── FftProcessor.cs      # FFT computation
│   │   │       └── SpectralMetrics.cs   # Centroid, HF ratio, decay
│   │   │
│   │   ├── Platforms/           # iOS/Android/Windows/MacCatalyst specifics
│   │   │   ├── Android/Services/AudioCaptureService.Android.cs
│   │   │   ├── iOS/Services/AudioCaptureService.iOS.cs
│   │   │   ├── MacCatalyst/Services/AudioCaptureService.MacCatalyst.cs
│   │   │   └── Windows/Services/AudioCaptureService.Windows.cs
│   │   │
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
│   │       ├── test_analysis.py # FFT pipeline tests
│   │       └── test_spectral.py # Metric calculation tests
│   │
│   └── SonicDecay.Data/         # Database layer (legacy reference)
│
├── docs/                        # Technical documentation
└── tests/                       # C# integration & unit tests
```

---

## Database Schema (3-Tier Relational Model)

### Entities

**StringSet** (Metadata)
```csharp
[Table("StringSets")]
public class StringSet
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    
    [NotNull]
    public string Brand { get; set; }          // e.g., "Elixir", "D'Addario"
    
    [NotNull]
    public string Model { get; set; }          // e.g., "Nanoweb", "EXL110"
    
    // Individual gauge storage for all 6 strings
    public double GaugeE1 { get; set; }        // High E (thinnest)
    public double GaugeB2 { get; set; }        // B
    public double GaugeG3 { get; set; }        // G
    public double GaugeD4 { get; set; }        // D
    public double GaugeA5 { get; set; }        // A
    public double GaugeE6 { get; set; }        // Low E (thickest)
    
    public DateTime CreatedAt { get; set; }
}
```

**StringBaseline** (Reference Indicators per String)
```csharp
[Table("StringBaselines")]
public class StringBaseline
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    
    [Indexed]
    public int SetId { get; set; }             // FK → StringSet.Id
    
    public int StringNumber { get; set; }      // 1-6 (1=High E, 6=Low E)
    
    public double FundamentalFreq { get; set; } // e.g., 82.41 Hz for Low E
    
    // Fresh String Spectral Fingerprint
    public double InitialCentroid { get; set; }    // Hz
    public double InitialHighRatio { get; set; }   // Decimal ratio
    
    public DateTime CreatedAt { get; set; }
}
```

**MeasurementLog** (Time-Series Decay Data)
```csharp
[Table("MeasurementLogs")]
public class MeasurementLog
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public int BaselineId { get; set; }        // FK → StringBaseline.Id

    // Current Spectral Measurements
    public double CurrentCentroid { get; set; }    // Hz
    public double CurrentHighRatio { get; set; }   // Decimal ratio

    // Derived Analytics
    public double DecayPercentage { get; set; }    // Calculated degradation
    public double PlayTimeHours { get; set; }      // User-reported play time

    public string? Note { get; set; }              // Optional user annotation
    public DateTime MeasuredAt { get; set; }
}
```

**Guitar** (Instrument Registry)
```csharp
[Table("Guitars")]
public class Guitar
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [NotNull]
    public string Name { get; set; }           // User-defined name (e.g., "My Strat")
    public string? Make { get; set; }          // e.g., "Fender"
    public string? Model { get; set; }         // e.g., "Stratocaster"
    [NotNull]
    public string Type { get; set; }           // "Electric", "Acoustic", "Classical", "Bass"
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

**GuitarStringSetPairing** (Junction Table)
```csharp
[Table("GuitarStringSetPairings")]
public class GuitarStringSetPairing
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public int GuitarId { get; set; }          // FK → Guitar.Id
    [Indexed]
    public int SetId { get; set; }             // FK → StringSet.Id

    public DateTime InstalledAt { get; set; }  // When strings were installed
    public DateTime? RemovedAt { get; set; }   // Null = currently active
    public bool IsActive { get; set; }         // Only one active per guitar
    public string? Notes { get; set; }
}
```

### Relational Structure

```
Guitar (1) ────< GuitarStringSetPairing (*) >──── StringSet (1)
                            │
                            └──── StringBaseline (6) ────< MeasurementLog (*)
```

**Cardinality Rules**:
- 1 Guitar → N GuitarStringSetPairings (string installation history)
- 1 StringSet → N GuitarStringSetPairings (can be used on multiple guitars)
- 1 GuitarStringSetPairing → 1 active per guitar (constraint enforced in repository)
- 1 StringSet → 6 StringBaselines (one per string)
- 1 StringBaseline → N MeasurementLogs (time-series)

### Constraints & Implementation Notes

- **Foreign Key Management**: Explicit handling in Repository layer (no cascading in SQLite attributes)
- **No ORM Magic**: Avoid high-level abstractions like SQLiteNetExtensions
- **Cascaded Deletes**: Implemented manually in Repository logic:
  - Deleting a `Guitar` must delete all related `GuitarStringSetPairings`
  - Deleting a `StringSet` must delete all related `StringBaselines` and `GuitarStringSetPairings`
  - Deleting a `StringBaseline` must delete all related `MeasurementLogs`
- **Active Pairing Constraint**: Only one `GuitarStringSetPairing` per guitar can have `IsActive = true`
- **Thread Safety**: All DB operations must be async and synchronized via `IDatabaseService`
- **Indexing**: Foreign keys (`SetId`, `BaselineId`, `GuitarId`) are indexed for query performance
- **Gauge Storage**: Individual properties (GaugeE1-E6) chosen over array serialization for schema transparency

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

### Service Layer (13 Interfaces, 13 Implementations)

| Interface | Implementation | Purpose |
|-----------|----------------|---------|
| `IDatabaseService` | `DatabaseService` | SQLite connection singleton |
| `IPermissionService` | `PermissionService` | Microphone access |
| `IAudioCaptureService` | `AudioCaptureService` | 48kHz PCM capture |
| `IAnalysisService` | `NativeAnalysisService` | Primary C# FFT analysis |
| `IAnalysisService` | `AnalysisService` | Python fallback (desktop) |
| `IMeasurementService` | `MeasurementService` | Analysis + DB coordination |
| `IRecommendationService` | `RecommendationService` | Decay prediction |
| `IStringSetRepository` | `StringSetRepository` | StringSet CRUD |
| `IStringBaselineRepository` | `StringBaselineRepository` | Baseline CRUD |
| `IMeasurementLogRepository` | `MeasurementLogRepository` | MeasurementLog CRUD |
| `IGuitarRepository` | `GuitarRepository` | Guitar CRUD |
| `IGuitarStringSetPairingRepository` | `GuitarStringSetPairingRepository` | Pairing CRUD |
| `ISeedDataService` | `SeedDataService` | Preset string data |

### Value Converters (15 Classes in ValueConverters.cs)

| Converter | Purpose |
|-----------|---------|
| `InvertedBoolConverter` | Boolean inversion |
| `StringNotEmptyConverter` | String validation |
| `IntToBoolConverter` | Integer to boolean |
| `DecayToColorConverter` | Health status colors (green/yellow/red) |
| `PresetToColorConverter` | Gauge preset button backgrounds |
| `PresetToTextColorConverter` | Gauge preset button text |
| `BoolToColorConverter` | Boolean-based styling |
| `BoolToTextColorConverter` | Boolean-based text styling |
| `StringNumberIndexConverter` | 1-6 to 0-5 index conversion |
| `CaptureButtonTextConverter` | Start/Stop button labels |
| `SaveButtonTextConverter` | Save/Update button labels |
| `ExpandChevronConverter` | Collapse/expand chevron (▼/▶) |
| `NullToVisibleConverter` | Null-state visibility |
| `NotNullToVisibleConverter` | Non-null visibility |
| `BoolToVisibleConverter` | Boolean visibility |

---

## Development Phases

### Phase 1: Database Infrastructure ✓
**Status**: Foundation layer  
**Deliverables**:
- `IDatabaseService` as thread-safe Singleton
- Repositories for all three entities with explicit FK management
- `Schema.sql` validation script
- Unit tests for CRUD operations

### Phase 2: Audio Capture Service ✓
**Status**: Completed  
**Deliverables**:
- `IPermissionService` for async microphone access
- `IAudioCaptureService` capturing PCM at 48kHz/24-bit
- RMS threshold trigger for automatic analysis start
- Platform-specific implementations (iOS/Android/Windows)

### Phase 3: Spectral Analysis Engine ✓
**Status**: Completed (ported to native C#)
**Deliverables**:
- FFT implementation with windowing (Hamming/Hann/Blackman) via FftSharp
- Spectral Centroid calculation: `Σ(f[i] × magnitude[i]) / Σ(magnitude[i])`
- HF Energy Ratio: `Σ(magnitude[5kHz:15kHz]) / magnitude[f₀]`
- Native `NativeAnalysisService` for cross-platform support (iOS/Android/Windows/macOS)
- Python engine retained as reference implementation and desktop fallback
- Automated metric persistence to `MeasurementLog`

### Phase 4: MVVM Presentation Layer ✓
**Status**: Completed  
**Deliverables**:
- `MainViewModel` with real-time analysis binding
- `StringInputView` with brand/model selection + custom gauges
- Data visualization (decay curve charts)
- Health percentage display with color-coded indicators

### Phase 5: Predictive Maintenance Algorithm ✓
**Status**: Completed
**Deliverables**:
- Decay rate calculation: `(Baseline - Current) / Baseline × 100`
- Replacement recommendation engine
- Latency optimization in audio pipeline
- Final architectural review for production readiness

### Phase 6: Guitar Management & Visualization ✓
**Status**: Complete
**Deliverables**:
- `Guitar` entity with Name, Make, Model, Type, Notes
- `GuitarStringSetPairing` junction table for instrument-string relationships
- `IGuitarRepository` and `IGuitarStringSetPairingRepository` interfaces and implementations
- `GuitarInputPage` for guitar CRUD operations
- `GuitarsListPage` for guitar list view with navigation
- `LibraryPage` as navigation hub (Guitars, String Sets, Pairings)
- `PairingsManagementPage` for dedicated pairing management
- Guitar selection picker in `MainPage` with collapsible context section
- LiveCharts2 integration for decay trend visualization
- `DecayChartPage` with user-selectable metrics (Decay %, Centroid, HF Ratio)
- `SeedDataService` for preset string data loading (Elixir, D'Addario, etc.)
- 15 XAML value converters for UI logic binding

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

## Testing Requirements

### Unit Tests (C#)
- Repository CRUD operations
- ViewModel state transitions
- Service layer isolation tests
- Spectral analysis classes (`WindowFunctions`, `FftProcessor`, `SpectralMetrics`)
- `NativeAnalysisService` end-to-end pipeline

### Integration Tests
- End-to-end audio capture → analysis → persistence
- Cross-platform permission handling
- Database schema migrations

### Algorithm Validation
- FFT accuracy with known sine waves (compare C# vs Python)
- Centroid calculation cross-validation (tolerance: ±1 Hz)
- HF Energy Ratio cross-validation (tolerance: ±0.001)
- Decay Percentage cross-validation (tolerance: ±0.1%)
- Edge cases: silence, clipping, noise

---

## Performance Targets

- **Audio Latency**: <100ms from trigger to analysis start
- **Analysis Speed**: <50ms for FFT + metric extraction
- **Database Writes**: <10ms per measurement log entry
- **UI Responsiveness**: 60fps during real-time updates

---

## Forbidden Patterns

❌ **Do NOT**:
- Use EF Core or high-level ORMs (violates architectural transparency)
- Mix business logic in Views or Code-behind
- Implement pitch detection (this is timbre analysis only)
- Use synchronous file I/O in UI thread
- Hard-code string gauges (must be user-configurable)

✅ **Do**:
- Maintain strict MVVM separation
- Use async/await for all I/O operations
- Implement proper IDisposable for audio resources
- Validate all user inputs before database writes
- Log all analysis engine errors to diagnostic stream

---

## Git Commit Standards

### Commit Message Format

All commits must follow the Conventional Commits specification:

```
<type>(<scope>): <description>

[optional body]

[optional footer]
```

### Commit Types

- **feat**: New feature (e.g., `feat(App): Implement microphone permission service`)
- **fix**: Bug fix (e.g., `fix(Engine): Resolve FFT windowing artifact`)
- **docs**: Documentation only (e.g., `docs(README): Update installation instructions`)
- **style**: Code style/formatting (e.g., `style(Models): Apply consistent spacing`)
- **refactor**: Code restructuring (e.g., `refactor(Data): Apply repository pattern`)
- **test**: Test additions/corrections (e.g., `test(Engine): Add centroid calculation tests`)
- **chore**: Build/tooling changes (e.g., `chore(deps): Update sqlite-net-pcl to 1.9.0`)

### Scope Guidelines

Use these scopes to match the project structure:

- **App**: Changes in `SonicDecay.App/` (ViewModels, Views, Services)
- **Engine**: Changes in `SonicDecay.Engine/` (Python analysis logic)
- **Data**: Changes in `SonicDecay.Data/` (Schema, repositories)
- **Models**: Changes to entity definitions
- **Platforms**: Platform-specific code (iOS/Android/Windows)
- **Docs**: Documentation files

### Examples

```
feat(App): Implement IAudioCaptureService for cross-platform recording
fix(Engine): Correct spectral centroid calculation for edge frequencies
refactor(Data): Extract StringSet repository from DatabaseService
test(Engine): Add FFT accuracy validation with sine wave inputs
docs(CLAUDE): Update database schema section with actual models
chore(App): Add Python.NET dependency for engine integration
```

### Commit Body Guidelines (Optional)

- Explain **why** the change was made, not **what** (the diff shows what)
- Reference architectural decisions from CLAUDE.md when relevant
- Note any breaking changes or migration requirements

---

## Communication Guidelines

When working with this project:

1. **Use precise terminology**: "Spectral Centroid" not "average frequency"
2. **Reference phase numbers**: "This belongs in Phase 3 deliverables"
3. **Cite constraints**: "Per the repository pattern requirement..."
4. **Question assumptions**: If something seems to violate SOLID, ask
5. **Provide rationale**: Explain architectural decisions with reference to project philosophy

---

## Current Development Context

**Active Phase**: All phases complete. Entering production hardening.
**Project Status**: Feature-complete (95%)
**Next Milestone**: C# unit test coverage, production deployment

**Completed Features**:
- Real-time spectral analysis with native C# DSP
- Guitar and string set management with pairing system
- Decay trend visualization with LiveCharts2
- Predictive replacement recommendations via linear regression
- Collapsible context section with summary display
- Platform-specific audio capture (iOS/Android/Windows/macOS)

**Remaining Work**:
- C# unit tests for repositories and ViewModels
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

*Last Updated: 2026-01-31*  
*Maintained by: Project Architect*  
*For AI Assistant Context: This document defines the complete technical contract for the Sonic Decay Analyzer project.*
