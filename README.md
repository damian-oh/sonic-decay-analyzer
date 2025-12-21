# Sonic Decay Analyzer

A data-driven predictive maintenance tool for guitarists that quantifies instrument string degradation through real-time spectral analysis.

## Project Objective
Guitarists traditionally rely on subjective "ear-feel" to determine when to change strings. This project eliminates that ambiguity by translating acoustic signals into quantifiable physical data, providing a logical threshold for string replacement.

## Core Engineering Logic
Instead of mere pitch detection, this system analyzes the **Timbre Fingerprint** of the instrument using:
- **Spectral Centroid**: Calculating the "center of mass" of the frequency spectrum to track the shift from bright to dull tonal characteristics.
- **High-Frequency Energy Ratio**: Measuring the amplitude decay of upper harmonics (5kHz - 15kHz) relative to the fundamental frequency.
- **Predictive Logging**: Utilizing SQLite to log decay trends over time, enabling personalized replacement cycle predictions based on actual usage.

## Technical Stack
- **Framework**: .NET MAUI (Cross-platform support for Windows, Android, and iOS)
- **Architecture**: MVVM Pattern (Strict separation of UI and business logic)
- **Signal Processing**: Python (NumPy/SciPy) for Fast Fourier Transform (FFT) and statistical modeling
- **Database**: SQLite (Relational logging of baseline and measurement data)

## Academic Foundation
This project is built upon a fusion of:
- **Acoustic Science**: Understanding of harmonic series and digital audio editing.
- **Advanced Mathematics**: Implementation of FFT algorithms and frequency domain analysis.
- **Software Engineering**: Applying rigorous Design Patterns to manage complex signal data flows.