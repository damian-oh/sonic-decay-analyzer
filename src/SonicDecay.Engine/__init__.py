"""
SonicDecay.Engine - Python DSP Backend for Spectral Analysis

This module provides acoustic degradation analysis through FFT-based
spectral metrics. Core functionality includes:

- Spectral Centroid calculation (tonal brightness measurement)
- High-Frequency Energy Ratio (harmonic content analysis)
- FFT with configurable windowing (Hamming/Hann)

Designed for integration with .NET MAUI frontend via process interop.
"""

__version__ = "0.1.0"
__author__ = "SonicDecay Project"

from spectral import calculate_spectral_centroid, calculate_hf_energy_ratio
from analysis import analyze_audio_buffer, AnalysisResult

__all__ = [
    "calculate_spectral_centroid",
    "calculate_hf_energy_ratio",
    "analyze_audio_buffer",
    "AnalysisResult",
]
