"""
FFT-based audio analysis pipeline for spectral metric extraction.

This module provides the main analysis interface for processing
audio buffers and extracting acoustic degradation metrics.

Processing Pipeline:
1. Apply window function (Hamming/Hann) to reduce spectral leakage
2. Zero-pad to 8192 points for frequency resolution
3. Compute FFT and extract magnitude spectrum
4. Calculate spectral centroid and HF energy ratio

Reference: CLAUDE.md audio processing specifications.
"""

import numpy as np
from dataclasses import dataclass
from typing import Optional, Literal
from enum import Enum

from spectral import (
    calculate_spectral_centroid,
    calculate_hf_energy_ratio,
    find_fundamental_frequency,
    calculate_decay_percentage,
)


class WindowType(Enum):
    """Supported window functions for FFT preprocessing."""
    HAMMING = "hamming"
    HANN = "hann"
    BLACKMAN = "blackman"
    RECTANGULAR = "rectangular"


@dataclass
class AnalysisResult:
    """
    Container for spectral analysis results.

    Attributes:
        spectral_centroid: Center of mass of frequency spectrum (Hz).
        hf_energy_ratio: Ratio of high-frequency to fundamental energy.
        fundamental_freq: Detected fundamental frequency (Hz).
        fundamental_magnitude: Magnitude at fundamental frequency.
        decay_percentage: Calculated decay if baseline provided (%).
        sample_rate: Sample rate of analyzed audio (Hz).
        fft_size: Size of FFT used in analysis.
        success: Whether analysis completed successfully.
        error_message: Error description if success is False.
    """
    spectral_centroid: float
    hf_energy_ratio: float
    fundamental_freq: float
    fundamental_magnitude: float
    decay_percentage: Optional[float]
    sample_rate: int
    fft_size: int
    success: bool
    error_message: Optional[str] = None

    def to_dict(self) -> dict:
        """Convert result to dictionary for JSON serialization."""
        return {
            "spectral_centroid": self.spectral_centroid,
            "hf_energy_ratio": self.hf_energy_ratio,
            "fundamental_freq": self.fundamental_freq,
            "fundamental_magnitude": self.fundamental_magnitude,
            "decay_percentage": self.decay_percentage,
            "sample_rate": self.sample_rate,
            "fft_size": self.fft_size,
            "success": self.success,
            "error_message": self.error_message,
        }


def apply_window(
    samples: np.ndarray,
    window_type: WindowType = WindowType.HAMMING
) -> np.ndarray:
    """
    Apply a window function to audio samples.

    Windowing reduces spectral leakage by tapering the signal
    edges to zero, minimizing discontinuities at buffer boundaries.

    Args:
        samples: Input audio samples.
        window_type: Type of window function to apply.

    Returns:
        Windowed samples with same length as input.
    """
    n = len(samples)

    if window_type == WindowType.HAMMING:
        window = np.hamming(n)
    elif window_type == WindowType.HANN:
        window = np.hanning(n)
    elif window_type == WindowType.BLACKMAN:
        window = np.blackman(n)
    elif window_type == WindowType.RECTANGULAR:
        window = np.ones(n)
    else:
        window = np.hamming(n)  # Default to Hamming

    return samples * window


def compute_fft(
    samples: np.ndarray,
    sample_rate: int,
    fft_size: int = 8192,
    window_type: WindowType = WindowType.HAMMING
) -> tuple[np.ndarray, np.ndarray]:
    """
    Compute FFT magnitude spectrum with windowing and zero-padding.

    Per CLAUDE.md specifications:
    - 8192-point FFT (zero-padded from 4096 samples)
    - Hamming window default to reduce spectral leakage

    Args:
        samples: Input audio samples (normalized -1 to 1).
        sample_rate: Sample rate in Hz.
        fft_size: FFT size (default 8192 for frequency resolution).
        window_type: Window function to apply before FFT.

    Returns:
        Tuple of (magnitude_spectrum, frequencies) for positive frequencies.
    """
    # Apply window function
    windowed = apply_window(samples, window_type)

    # Zero-pad to FFT size if necessary
    if len(windowed) < fft_size:
        padded = np.zeros(fft_size)
        padded[:len(windowed)] = windowed
    else:
        padded = windowed[:fft_size]

    # Compute FFT
    fft_result = np.fft.fft(padded)

    # Extract positive frequencies only (up to Nyquist)
    n_positive = fft_size // 2 + 1
    magnitude = np.abs(fft_result[:n_positive])

    # Generate frequency bins
    frequencies = np.fft.fftfreq(fft_size, 1.0 / sample_rate)[:n_positive]
    frequencies = np.abs(frequencies)  # Ensure positive

    return magnitude, frequencies


def analyze_audio_buffer(
    samples: np.ndarray,
    sample_rate: int,
    expected_fundamental: Optional[float] = None,
    initial_centroid: Optional[float] = None,
    fft_size: int = 8192,
    window_type: WindowType = WindowType.HAMMING,
    hf_low: float = 5000.0,
    hf_high: float = 15000.0
) -> AnalysisResult:
    """
    Perform complete spectral analysis on an audio buffer.

    This is the main entry point for the analysis pipeline. It:
    1. Validates input data
    2. Computes windowed FFT
    3. Extracts spectral centroid
    4. Calculates HF energy ratio
    5. Optionally computes decay percentage

    Args:
        samples: Audio samples as numpy array (normalized -1 to 1).
        sample_rate: Sample rate in Hz (typically 48000).
        expected_fundamental: Expected f₀ for HF ratio calculation.
            If None, will attempt auto-detection.
        initial_centroid: Baseline centroid for decay calculation.
            If None, decay_percentage will be None.
        fft_size: FFT size (default 8192).
        window_type: Window function (default Hamming).
        hf_low: Lower bound of HF band (default 5000 Hz).
        hf_high: Upper bound of HF band (default 15000 Hz).

    Returns:
        AnalysisResult containing all computed metrics.
    """
    try:
        # Validate input
        if samples is None or len(samples) == 0:
            return AnalysisResult(
                spectral_centroid=0.0,
                hf_energy_ratio=0.0,
                fundamental_freq=0.0,
                fundamental_magnitude=0.0,
                decay_percentage=None,
                sample_rate=sample_rate,
                fft_size=fft_size,
                success=False,
                error_message="Empty or null sample buffer",
            )

        if sample_rate <= 0:
            return AnalysisResult(
                spectral_centroid=0.0,
                hf_energy_ratio=0.0,
                fundamental_freq=0.0,
                fundamental_magnitude=0.0,
                decay_percentage=None,
                sample_rate=sample_rate,
                fft_size=fft_size,
                success=False,
                error_message="Invalid sample rate",
            )

        # Convert to numpy array if needed
        samples = np.asarray(samples, dtype=np.float64)

        # Compute FFT
        magnitude, frequencies = compute_fft(
            samples, sample_rate, fft_size, window_type
        )

        # Calculate spectral centroid
        centroid = calculate_spectral_centroid(magnitude, frequencies)

        # Find or use fundamental frequency
        if expected_fundamental is not None and expected_fundamental > 0:
            fundamental = expected_fundamental
            # Get magnitude at expected fundamental
            _, fund_mag = find_fundamental_frequency(
                magnitude, frequencies, expected_fundamental
            )
        else:
            # Auto-detect fundamental
            fundamental, fund_mag = find_fundamental_frequency(
                magnitude, frequencies
            )

        # Calculate HF energy ratio
        if fundamental > 0:
            hf_ratio = calculate_hf_energy_ratio(
                magnitude, frequencies, fundamental, hf_low, hf_high
            )
        else:
            hf_ratio = 0.0

        # Calculate decay if baseline provided
        decay = None
        if initial_centroid is not None and initial_centroid > 0:
            decay = calculate_decay_percentage(initial_centroid, centroid)

        return AnalysisResult(
            spectral_centroid=centroid,
            hf_energy_ratio=hf_ratio,
            fundamental_freq=fundamental,
            fundamental_magnitude=fund_mag,
            decay_percentage=decay,
            sample_rate=sample_rate,
            fft_size=fft_size,
            success=True,
            error_message=None,
        )

    except Exception as e:
        return AnalysisResult(
            spectral_centroid=0.0,
            hf_energy_ratio=0.0,
            fundamental_freq=0.0,
            fundamental_magnitude=0.0,
            decay_percentage=None,
            sample_rate=sample_rate,
            fft_size=fft_size,
            success=False,
            error_message=str(e),
        )


def generate_test_signal(
    frequency: float,
    sample_rate: int = 48000,
    duration: float = 0.1,
    harmonics: Optional[list[tuple[int, float]]] = None
) -> np.ndarray:
    """
    Generate a test signal with fundamental and optional harmonics.

    Useful for algorithm validation and testing.

    Args:
        frequency: Fundamental frequency in Hz.
        sample_rate: Sample rate in Hz.
        duration: Duration in seconds.
        harmonics: List of (harmonic_number, relative_amplitude) tuples.
            E.g., [(2, 0.5), (3, 0.25)] for 2nd harmonic at 50% and 3rd at 25%.

    Returns:
        Numpy array of audio samples.
    """
    t = np.arange(0, duration, 1.0 / sample_rate)

    # Fundamental
    signal = np.sin(2 * np.pi * frequency * t)

    # Add harmonics
    if harmonics:
        for harmonic_num, amplitude in harmonics:
            signal += amplitude * np.sin(2 * np.pi * frequency * harmonic_num * t)

    # Normalize to prevent clipping
    max_amp = np.max(np.abs(signal))
    if max_amp > 0:
        signal = signal / max_amp * 0.9

    return signal
