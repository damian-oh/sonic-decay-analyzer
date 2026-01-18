"""
Unit tests for spectral analysis functions.

Tests validate:
- Spectral centroid calculation accuracy
- HF energy ratio computation
- Decay percentage calculation
- Fundamental frequency detection
- Edge cases and error handling
"""

import pytest
import numpy as np
import sys
import os

# Add parent directory to path for imports
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from spectral import (
    calculate_spectral_centroid,
    calculate_hf_energy_ratio,
    calculate_decay_percentage,
    find_fundamental_frequency,
)


class TestSpectralCentroid:
    """Tests for spectral centroid calculation."""

    def test_single_frequency_centroid(self):
        """Centroid of a single frequency peak should equal that frequency."""
        frequencies = np.array([0, 100, 200, 300, 400, 500])
        # Single peak at 300 Hz
        magnitudes = np.array([0, 0, 0, 1.0, 0, 0])

        centroid = calculate_spectral_centroid(magnitudes, frequencies)

        assert centroid == pytest.approx(300.0, rel=1e-6)

    def test_uniform_spectrum_centroid(self):
        """Centroid of uniform spectrum should be at the center frequency."""
        frequencies = np.array([100, 200, 300, 400, 500])
        magnitudes = np.array([1.0, 1.0, 1.0, 1.0, 1.0])

        centroid = calculate_spectral_centroid(magnitudes, frequencies)

        # Weighted mean of uniform distribution = arithmetic mean
        expected = np.mean(frequencies)
        assert centroid == pytest.approx(expected, rel=1e-6)

    def test_weighted_centroid(self):
        """Centroid should be weighted toward higher magnitude frequencies."""
        frequencies = np.array([100, 200, 300])
        # Higher weight at 200 Hz
        magnitudes = np.array([1.0, 3.0, 1.0])

        centroid = calculate_spectral_centroid(magnitudes, frequencies)

        # Should be closer to 200 than arithmetic mean (200)
        expected = (100 * 1 + 200 * 3 + 300 * 1) / (1 + 3 + 1)
        assert centroid == pytest.approx(expected, rel=1e-6)

    def test_frequency_band_filtering(self):
        """Centroid should only consider frequencies within specified band."""
        frequencies = np.array([10, 50, 100, 200, 25000])
        magnitudes = np.array([1.0, 1.0, 1.0, 1.0, 1.0])

        # Only consider 20-20000 Hz range
        centroid = calculate_spectral_centroid(
            magnitudes, frequencies, freq_min=20, freq_max=20000
        )

        # Should exclude 10 Hz and 25000 Hz
        expected = (50 + 100 + 200) / 3
        assert centroid == pytest.approx(expected, rel=1e-6)

    def test_empty_input_raises_error(self):
        """Empty arrays should raise ValueError."""
        with pytest.raises(ValueError, match="empty"):
            calculate_spectral_centroid(np.array([]), np.array([]))

    def test_mismatched_lengths_raises_error(self):
        """Mismatched array lengths should raise ValueError."""
        with pytest.raises(ValueError, match="same length"):
            calculate_spectral_centroid(
                np.array([1, 2, 3]),
                np.array([100, 200])
            )

    def test_zero_magnitude_returns_zero(self):
        """All-zero magnitudes should return zero centroid."""
        frequencies = np.array([100, 200, 300])
        magnitudes = np.array([0.0, 0.0, 0.0])

        centroid = calculate_spectral_centroid(magnitudes, frequencies)

        assert centroid == 0.0


class TestHFEnergyRatio:
    """Tests for high-frequency energy ratio calculation."""

    def test_no_hf_content(self):
        """Signal with no HF content should have zero ratio."""
        frequencies = np.array([80, 100, 160, 200])  # All below 5 kHz
        magnitudes = np.array([1.0, 0.5, 0.25, 0.1])

        ratio = calculate_hf_energy_ratio(
            magnitudes, frequencies,
            fundamental_freq=80.0
        )

        assert ratio == 0.0

    def test_equal_fundamental_and_hf_energy(self):
        """Equal fundamental and HF energy should give ratio of 1.0."""
        frequencies = np.array([80, 5000, 10000, 15000])
        magnitudes = np.array([1.0, 0.5, 0.3, 0.2])  # HF sum = 1.0

        ratio = calculate_hf_energy_ratio(
            magnitudes, frequencies,
            fundamental_freq=80.0,
            fundamental_bandwidth=20.0
        )

        assert ratio == pytest.approx(1.0, rel=1e-6)

    def test_zero_fundamental_returns_zero(self):
        """Zero fundamental energy should return zero ratio."""
        frequencies = np.array([80, 5000, 10000])
        magnitudes = np.array([0.0, 1.0, 1.0])  # No fundamental energy

        ratio = calculate_hf_energy_ratio(
            magnitudes, frequencies,
            fundamental_freq=80.0,
            fundamental_bandwidth=20.0
        )

        assert ratio == 0.0

    def test_invalid_fundamental_raises_error(self):
        """Negative or zero fundamental should raise ValueError."""
        frequencies = np.array([100, 200])
        magnitudes = np.array([1.0, 1.0])

        with pytest.raises(ValueError, match="positive"):
            calculate_hf_energy_ratio(magnitudes, frequencies, fundamental_freq=0)

        with pytest.raises(ValueError, match="positive"):
            calculate_hf_energy_ratio(magnitudes, frequencies, fundamental_freq=-100)


class TestDecayPercentage:
    """Tests for decay percentage calculation."""

    def test_no_decay(self):
        """Same centroid values should give 0% decay."""
        decay = calculate_decay_percentage(3000.0, 3000.0)
        assert decay == pytest.approx(0.0, abs=1e-10)

    def test_positive_decay(self):
        """Lower current centroid should give positive decay."""
        # 3000 -> 2400 = 20% decrease
        decay = calculate_decay_percentage(3000.0, 2400.0)
        assert decay == pytest.approx(20.0, rel=1e-6)

    def test_fifty_percent_decay(self):
        """Half the centroid should give 50% decay."""
        decay = calculate_decay_percentage(4000.0, 2000.0)
        assert decay == pytest.approx(50.0, rel=1e-6)

    def test_full_decay(self):
        """Zero current centroid should give 100% decay."""
        decay = calculate_decay_percentage(3000.0, 0.0)
        assert decay == pytest.approx(100.0, rel=1e-6)

    def test_negative_decay_possible(self):
        """Higher current centroid should give negative decay (brighter)."""
        decay = calculate_decay_percentage(2000.0, 2500.0)
        assert decay == pytest.approx(-25.0, rel=1e-6)

    def test_zero_initial_raises_error(self):
        """Zero initial centroid should raise ValueError."""
        with pytest.raises(ValueError, match="positive"):
            calculate_decay_percentage(0.0, 2000.0)


class TestFundamentalDetection:
    """Tests for fundamental frequency detection."""

    def test_detect_single_peak(self):
        """Should detect single prominent peak."""
        frequencies = np.array([50, 82, 100, 150, 200])
        magnitudes = np.array([0.1, 1.0, 0.2, 0.1, 0.1])

        freq, mag = find_fundamental_frequency(magnitudes, frequencies)

        assert freq == pytest.approx(82.0, rel=1e-6)
        assert mag == pytest.approx(1.0, rel=1e-6)

    def test_detect_with_expected_hint(self):
        """Should find peak near expected frequency."""
        frequencies = np.array([50, 80, 85, 100, 150])
        magnitudes = np.array([0.5, 0.9, 1.0, 0.3, 0.2])

        # Expect around 82 Hz, should find 85
        freq, mag = find_fundamental_frequency(
            magnitudes, frequencies,
            expected_freq=82.0,
            search_tolerance=0.1
        )

        assert freq == pytest.approx(85.0, rel=1e-6)

    def test_empty_search_range_returns_zero(self):
        """No frequencies in range should return zeros."""
        frequencies = np.array([5000, 6000, 7000])
        magnitudes = np.array([1.0, 1.0, 1.0])

        freq, mag = find_fundamental_frequency(
            magnitudes, frequencies,
            min_freq=60,
            max_freq=500
        )

        assert freq == 0.0
        assert mag == 0.0
