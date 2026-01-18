"""
Unit tests for FFT analysis pipeline.

Tests validate:
- FFT computation accuracy with known sine waves
- Window function application
- End-to-end analysis pipeline
- Error handling for edge cases
"""

import pytest
import numpy as np
import sys
import os

# Add parent directory to path for imports
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from analysis import (
    apply_window,
    compute_fft,
    analyze_audio_buffer,
    generate_test_signal,
    WindowType,
    AnalysisResult,
)


class TestWindowFunctions:
    """Tests for window function application."""

    def test_hamming_window_reduces_endpoints(self):
        """Hamming window should reduce signal at endpoints."""
        samples = np.ones(100)
        windowed = apply_window(samples, WindowType.HAMMING)

        # Endpoints should be reduced
        assert windowed[0] < samples[0]
        assert windowed[-1] < samples[-1]
        # Middle should be close to original
        assert windowed[50] > 0.9

    def test_hann_window_zeros_endpoints(self):
        """Hann window should approach zero at endpoints."""
        samples = np.ones(100)
        windowed = apply_window(samples, WindowType.HANN)

        # Hann window is zero at endpoints
        assert windowed[0] == pytest.approx(0.0, abs=1e-10)
        assert windowed[-1] == pytest.approx(0.0, abs=1e-10)

    def test_rectangular_window_preserves_signal(self):
        """Rectangular window should not modify signal."""
        samples = np.array([0.5, 0.7, 0.3, 0.9])
        windowed = apply_window(samples, WindowType.RECTANGULAR)

        np.testing.assert_array_almost_equal(windowed, samples)

    def test_window_preserves_length(self):
        """Window should not change sample count."""
        samples = np.random.randn(1024)
        windowed = apply_window(samples, WindowType.HAMMING)

        assert len(windowed) == len(samples)


class TestFFTComputation:
    """Tests for FFT accuracy with known signals."""

    def test_fft_detects_single_frequency(self):
        """FFT should correctly identify a pure sine wave frequency."""
        sample_rate = 48000
        frequency = 440.0  # A4
        duration = 0.1
        t = np.arange(0, duration, 1.0 / sample_rate)
        samples = np.sin(2 * np.pi * frequency * t)

        magnitude, frequencies = compute_fft(samples, sample_rate, fft_size=8192)

        # Find peak frequency
        peak_idx = np.argmax(magnitude)
        detected_freq = frequencies[peak_idx]

        # Should be within frequency resolution
        freq_resolution = sample_rate / 8192
        assert abs(detected_freq - frequency) < freq_resolution * 2

    def test_fft_detects_multiple_frequencies(self):
        """FFT should detect multiple frequency components."""
        sample_rate = 48000
        f1, f2 = 200.0, 500.0
        duration = 0.1
        t = np.arange(0, duration, 1.0 / sample_rate)
        samples = np.sin(2 * np.pi * f1 * t) + 0.5 * np.sin(2 * np.pi * f2 * t)

        magnitude, frequencies = compute_fft(samples, sample_rate, fft_size=8192)

        # Find peaks (significant magnitudes)
        threshold = np.max(magnitude) * 0.3
        peaks = frequencies[magnitude > threshold]

        # Both frequencies should be detected
        assert any(abs(p - f1) < 10 for p in peaks)
        assert any(abs(p - f2) < 10 for p in peaks)

    def test_fft_zero_padding(self):
        """Short signals should be zero-padded to FFT size."""
        samples = np.random.randn(1000)  # Less than 8192
        sample_rate = 48000
        fft_size = 8192

        magnitude, frequencies = compute_fft(samples, sample_rate, fft_size)

        # Output should be based on fft_size
        expected_bins = fft_size // 2 + 1
        assert len(magnitude) == expected_bins
        assert len(frequencies) == expected_bins

    def test_fft_frequency_range(self):
        """Frequencies should range from 0 to Nyquist."""
        sample_rate = 48000
        samples = np.random.randn(4096)

        magnitude, frequencies = compute_fft(samples, sample_rate, fft_size=8192)

        assert frequencies[0] == 0.0
        assert frequencies[-1] == pytest.approx(sample_rate / 2, rel=1e-6)


class TestAnalysisPipeline:
    """Tests for end-to-end analysis pipeline."""

    def test_analyze_synthetic_signal(self):
        """Analysis of synthetic signal should return valid metrics."""
        samples = generate_test_signal(
            frequency=82.41,  # Low E
            sample_rate=48000,
            duration=0.1,
            harmonics=[(2, 0.5), (3, 0.3)]
        )

        result = analyze_audio_buffer(
            samples=samples,
            sample_rate=48000,
            expected_fundamental=82.41
        )

        assert result.success is True
        assert result.error_message is None
        assert result.fundamental_freq > 0
        assert result.spectral_centroid > 0

    def test_analyze_with_decay_calculation(self):
        """Should calculate decay when baseline provided."""
        samples = generate_test_signal(frequency=440.0, sample_rate=48000)

        result = analyze_audio_buffer(
            samples=samples,
            sample_rate=48000,
            initial_centroid=5000.0  # Hypothetical baseline
        )

        assert result.success is True
        assert result.decay_percentage is not None

    def test_analyze_empty_buffer_fails(self):
        """Empty buffer should return failure."""
        result = analyze_audio_buffer(
            samples=np.array([]),
            sample_rate=48000
        )

        assert result.success is False
        assert "empty" in result.error_message.lower()

    def test_analyze_invalid_sample_rate_fails(self):
        """Invalid sample rate should return failure."""
        samples = np.random.randn(1000)

        result = analyze_audio_buffer(samples=samples, sample_rate=0)

        assert result.success is False

    def test_analysis_result_to_dict(self):
        """AnalysisResult should serialize to dictionary."""
        result = AnalysisResult(
            spectral_centroid=2500.0,
            hf_energy_ratio=0.45,
            fundamental_freq=82.41,
            fundamental_magnitude=0.9,
            decay_percentage=15.5,
            sample_rate=48000,
            fft_size=8192,
            success=True,
        )

        d = result.to_dict()

        assert d["spectral_centroid"] == 2500.0
        assert d["success"] is True
        assert "error_message" in d


class TestTestSignalGeneration:
    """Tests for synthetic signal generation utility."""

    def test_generate_pure_tone(self):
        """Should generate clean sine wave at specified frequency."""
        frequency = 440.0
        sample_rate = 48000
        samples = generate_test_signal(frequency, sample_rate, duration=0.1)

        # Verify frequency via FFT
        magnitude, frequencies = compute_fft(samples, sample_rate)
        peak_idx = np.argmax(magnitude)
        detected_freq = frequencies[peak_idx]

        assert abs(detected_freq - frequency) < 10  # Within 10 Hz

    def test_generate_with_harmonics(self):
        """Should include specified harmonics."""
        fundamental = 100.0
        harmonics = [(2, 0.5), (3, 0.25)]  # 2nd at 50%, 3rd at 25%
        samples = generate_test_signal(
            fundamental,
            sample_rate=48000,
            duration=0.1,
            harmonics=harmonics
        )

        magnitude, frequencies = compute_fft(samples, 48000)

        # Check that harmonic frequencies have significant energy
        for harmonic_num, _ in harmonics:
            harmonic_freq = fundamental * harmonic_num
            # Find closest frequency bin
            idx = np.argmin(np.abs(frequencies - harmonic_freq))
            # Should have notable magnitude
            assert magnitude[idx] > np.max(magnitude) * 0.1

    def test_signal_is_normalized(self):
        """Generated signal should not exceed ±1."""
        samples = generate_test_signal(
            440.0,
            harmonics=[(2, 1.0), (3, 1.0), (4, 1.0)]
        )

        assert np.max(np.abs(samples)) <= 1.0
