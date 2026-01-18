"""
Spectral analysis functions for acoustic degradation measurement.

This module implements the core spectral metrics used to quantify
guitar string wear through timbre fingerprinting.

Mathematical Foundations:
- Spectral Centroid: Center of mass of the frequency spectrum
- HF Energy Ratio: Proportion of energy in upper harmonics

Reference: CLAUDE.md specification for metric definitions.
"""

import numpy as np
from typing import Tuple, Optional


def calculate_spectral_centroid(
    magnitude_spectrum: np.ndarray,
    frequencies: np.ndarray,
    freq_min: float = 20.0,
    freq_max: float = 20000.0
) -> float:
    """
    Calculate the spectral centroid (center of mass) of a frequency spectrum.

    The spectral centroid indicates the "brightness" of a sound and is
    calculated as the weighted mean of frequencies present in the signal:

        Centroid = Σ(f[i] × magnitude[i]) / Σ(magnitude[i])

    As strings age, the centroid typically decreases (darker timbre).

    Args:
        magnitude_spectrum: FFT magnitude values (non-negative).
        frequencies: Corresponding frequency bins in Hz.
        freq_min: Minimum frequency to consider (default 20 Hz).
        freq_max: Maximum frequency to consider (default 20 kHz).

    Returns:
        Spectral centroid frequency in Hz.

    Raises:
        ValueError: If input arrays have mismatched lengths or are empty.
    """
    if len(magnitude_spectrum) != len(frequencies):
        raise ValueError("Magnitude spectrum and frequencies must have same length")

    if len(magnitude_spectrum) == 0:
        raise ValueError("Input arrays cannot be empty")

    # Apply frequency band filtering
    mask = (frequencies >= freq_min) & (frequencies <= freq_max)
    filtered_magnitudes = magnitude_spectrum[mask]
    filtered_frequencies = frequencies[mask]

    if len(filtered_magnitudes) == 0:
        return 0.0

    # Calculate weighted mean (spectral centroid)
    magnitude_sum = np.sum(filtered_magnitudes)

    if magnitude_sum == 0:
        return 0.0

    centroid = np.sum(filtered_frequencies * filtered_magnitudes) / magnitude_sum

    return float(centroid)


def calculate_hf_energy_ratio(
    magnitude_spectrum: np.ndarray,
    frequencies: np.ndarray,
    fundamental_freq: float,
    hf_low: float = 5000.0,
    hf_high: float = 15000.0,
    fundamental_bandwidth: float = 50.0
) -> float:
    """
    Calculate the High-Frequency Energy Ratio relative to fundamental.

    Measures the ratio of energy in the upper harmonic band (5-15 kHz)
    against the fundamental frequency energy:

        HF Ratio = Σ(magnitude[5kHz:15kHz]) / magnitude[f₀]

    Fresh strings exhibit higher HF ratios due to stronger harmonics.
    As strings degrade, upper harmonics decay faster than the fundamental.

    Args:
        magnitude_spectrum: FFT magnitude values (non-negative).
        frequencies: Corresponding frequency bins in Hz.
        fundamental_freq: The fundamental frequency (f₀) in Hz.
        hf_low: Lower bound of HF band (default 5 kHz).
        hf_high: Upper bound of HF band (default 15 kHz).
        fundamental_bandwidth: Hz range around f₀ to consider (default 50 Hz).

    Returns:
        Ratio of HF energy to fundamental energy (dimensionless).
        Returns 0.0 if fundamental energy is negligible.

    Raises:
        ValueError: If input arrays have mismatched lengths or are empty.
    """
    if len(magnitude_spectrum) != len(frequencies):
        raise ValueError("Magnitude spectrum and frequencies must have same length")

    if len(magnitude_spectrum) == 0:
        raise ValueError("Input arrays cannot be empty")

    if fundamental_freq <= 0:
        raise ValueError("Fundamental frequency must be positive")

    # Calculate fundamental energy (within bandwidth around f₀)
    f0_low = fundamental_freq - fundamental_bandwidth / 2
    f0_high = fundamental_freq + fundamental_bandwidth / 2
    f0_mask = (frequencies >= f0_low) & (frequencies <= f0_high)
    fundamental_energy = np.sum(magnitude_spectrum[f0_mask])

    if fundamental_energy < 1e-10:  # Avoid division by near-zero
        return 0.0

    # Calculate high-frequency band energy
    hf_mask = (frequencies >= hf_low) & (frequencies <= hf_high)
    hf_energy = np.sum(magnitude_spectrum[hf_mask])

    ratio = hf_energy / fundamental_energy

    return float(ratio)


def calculate_decay_percentage(
    initial_centroid: float,
    current_centroid: float
) -> float:
    """
    Calculate the decay percentage based on spectral centroid shift.

    Decay is measured as the relative decrease from the baseline:

        Decay% = (Initial - Current) / Initial × 100

    Args:
        initial_centroid: Baseline centroid from fresh strings (Hz).
        current_centroid: Current measured centroid (Hz).

    Returns:
        Decay percentage (0-100+). Positive values indicate darkening.
        Can exceed 100% in extreme degradation cases.

    Raises:
        ValueError: If initial centroid is zero or negative.
    """
    if initial_centroid <= 0:
        raise ValueError("Initial centroid must be positive")

    decay = ((initial_centroid - current_centroid) / initial_centroid) * 100.0

    return float(decay)


def find_fundamental_frequency(
    magnitude_spectrum: np.ndarray,
    frequencies: np.ndarray,
    expected_freq: Optional[float] = None,
    search_tolerance: float = 0.1,
    min_freq: float = 60.0,
    max_freq: float = 1500.0
) -> Tuple[float, float]:
    """
    Detect the fundamental frequency from the magnitude spectrum.

    Uses peak detection within an expected range. If expected_freq is
    provided, searches within ±tolerance of that frequency.

    Args:
        magnitude_spectrum: FFT magnitude values.
        frequencies: Corresponding frequency bins in Hz.
        expected_freq: Expected fundamental frequency (optional).
        search_tolerance: Fractional tolerance around expected (default 10%).
        min_freq: Minimum search frequency if no expected (default 60 Hz).
        max_freq: Maximum search frequency if no expected (default 1500 Hz).

    Returns:
        Tuple of (detected_frequency_hz, magnitude_at_peak).
    """
    if expected_freq is not None and expected_freq > 0:
        # Search within tolerance of expected frequency
        low = expected_freq * (1 - search_tolerance)
        high = expected_freq * (1 + search_tolerance)
    else:
        low = min_freq
        high = max_freq

    # Create mask for search range
    mask = (frequencies >= low) & (frequencies <= high)
    search_magnitudes = magnitude_spectrum[mask]
    search_frequencies = frequencies[mask]

    if len(search_magnitudes) == 0:
        return (0.0, 0.0)

    # Find peak
    peak_idx = np.argmax(search_magnitudes)
    peak_freq = search_frequencies[peak_idx]
    peak_mag = search_magnitudes[peak_idx]

    return (float(peak_freq), float(peak_mag))
