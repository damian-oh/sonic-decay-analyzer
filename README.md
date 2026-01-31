# Sonic Decay Analyzer

A data-driven predictive maintenance tool for guitarists that quantifies instrument string degradation through real-time spectral analysis.

## Project Status

**Version**: 1.0 (Feature Complete)
**Platform**: .NET MAUI (Windows, Android, iOS, macOS)
**License**: Proprietary

## Overview

Guitarists traditionally rely on subjective "ear-feel" to determine when to change strings. This project eliminates that ambiguity by translating acoustic signals into quantifiable physical data, providing a logical threshold for string replacement.

This is **not** a tuner app. This is an **acoustic degradation analysis engine** that quantifies string wear through timbre fingerprinting rather than pitch detection.

## Key Features

### Real-Time Spectral Analysis
- **Spectral Centroid Tracking**: Measures the "center of mass" of the frequency spectrum to detect tonal darkening
- **High-Frequency Energy Ratio**: Monitors amplitude decay of upper harmonics (5kHz-15kHz) relative to the fundamental
- **Native C# DSP**: Cross-platform FFT analysis via FftSharp library (no external dependencies)

### Guitar & String Management
- Register multiple guitars with make, model, and type classification
- Track string sets with individual gauge configurations
- Pair strings to guitars with installation date tracking
- Automatic baseline capture for fresh string spectral fingerprints

### Predictive Maintenance
- Time-series decay tracking with SQLite persistence
- Linear regression-based replacement predictions
- Health status indicators (green/yellow/red) with configurable thresholds
- Estimated replacement date calculation

### Data Visualization
- Interactive decay trend charts via LiveCharts2
- Selectable metrics: Decay %, Spectral Centroid, HF Energy Ratio
- Historical measurement comparison

## Technical Architecture

```
Frontend:  .NET MAUI (C#) - Strict MVVM Pattern
Backend:   Native C# DSP via FftSharp (cross-platform)
Database:  SQLite via sqlite-net-pcl
Fallback:  Python engine for desktop debugging/validation
```

### Audio Specifications
- **Sample Rate**: 48,000 Hz
- **Bit Depth**: 24-bit PCM
- **Buffer Size**: 4096 samples (85.3ms @ 48kHz)
- **FFT Size**: 8192-point (zero-padded)

## Project Structure

```
SonicDecayAnalyzer/
├── src/
│   ├── SonicDecay.App/          # .NET MAUI Application
│   │   ├── Models/              # 5 entity classes
│   │   ├── ViewModels/          # 9 MVVM view models
│   │   ├── Views/               # 8 XAML pages
│   │   ├── Services/            # 13 service interfaces + implementations
│   │   ├── Converters/          # 15 value converters
│   │   └── Platforms/           # Platform-specific audio capture
│   │
│   └── SonicDecay.Engine/       # Python reference implementation
│       ├── analysis.py          # FFT pipeline
│       ├── spectral.py          # Metric calculations
│       └── tests/               # Algorithm validation (pytest)
│
├── docs/                        # Technical documentation
└── tests/                       # C# unit tests
```

## Pages

| Page | Description |
|------|-------------|
| **MainPage** | Real-time analysis interface with collapsible context, spectral metrics, and health indicators |
| **DecayChartPage** | Interactive decay trend visualization with metric selection |
| **LibraryPage** | Navigation hub for guitars, string sets, and pairings |
| **GuitarsListPage** | Guitar inventory with CRUD operations |
| **GuitarInputPage** | Guitar add/edit form with type picker |
| **StringSetsListPage** | String set inventory |
| **StringInputPage** | String set add/edit with gauge presets (light/regular/heavy) |
| **PairingsManagementPage** | Guitar-string pairing management with active status control |

## Dependencies

### NuGet Packages
- `Microsoft.Maui.Controls` - Cross-platform UI framework
- `sqlite-net-pcl` (v1.9.172) - SQLite database access
- `FftSharp` (v2.2.0) - FFT computation (MIT license)
- `LiveChartsCore.SkiaSharpView.Maui` (v2.0.0-rc3.3) - Chart visualization

### Python (Reference Engine)
- NumPy / SciPy - Signal processing
- pytest - Algorithm validation

## Building

### Prerequisites
- .NET 9.0 SDK
- Visual Studio 2022 or Rider with MAUI workload
- Platform SDKs (Android SDK, Xcode for iOS/macOS, Windows SDK)

### Build Commands
```bash
# Restore dependencies
dotnet restore

# Build for Windows
dotnet build -f net9.0-windows10.0.19041.0

# Build for Android
dotnet build -f net9.0-android

# Build for iOS (requires macOS)
dotnet build -f net9.0-ios
```

## Academic Foundation

This project is built upon a fusion of:
- **Acoustic Science**: Understanding of harmonic series and digital audio processing
- **Advanced Mathematics**: Implementation of FFT algorithms and frequency domain analysis
- **Software Engineering**: Rigorous design patterns (MVVM, Repository, SOLID) to manage complex signal data flows

## Documentation

- **CLAUDE.md**: Complete technical specification and architectural contract
- **docs/**: Additional technical documentation (in development)

---

*Last Updated: 2026-01-31*
