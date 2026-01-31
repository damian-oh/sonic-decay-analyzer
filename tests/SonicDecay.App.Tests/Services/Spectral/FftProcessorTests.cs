using FluentAssertions;
using SonicDecay.App.Services.Spectral;
using Xunit;

namespace SonicDecay.App.Tests.Services.Spectral;

/// <summary>
/// Unit tests for FftProcessor class.
/// Tests FFT computation and frequency bin generation.
/// </summary>
public class FftProcessorTests
{
    private const int SampleRate = 48000;
    private const int DefaultFftSize = 8192;

    #region Compute Tests

    [Fact]
    public void Compute_WithNullSamples_ReturnsEmptyArrays()
    {
        // Act
        var (magnitudes, frequencies) = FftProcessor.Compute(null!, SampleRate);

        // Assert
        magnitudes.Should().BeEmpty();
        frequencies.Should().BeEmpty();
    }

    [Fact]
    public void Compute_WithEmptySamples_ReturnsEmptyArrays()
    {
        // Act
        var (magnitudes, frequencies) = FftProcessor.Compute(Array.Empty<double>(), SampleRate);

        // Assert
        magnitudes.Should().BeEmpty();
        frequencies.Should().BeEmpty();
    }

    [Fact]
    public void Compute_WithZeroSampleRate_ThrowsArgumentException()
    {
        // Arrange
        var samples = GenerateSineWave(440, 1.0, SampleRate, 4096);

        // Act & Assert
        var act = () => FftProcessor.Compute(samples, 0);
        act.Should().Throw<ArgumentException>().WithParameterName("sampleRate");
    }

    [Fact]
    public void Compute_ReturnsCorrectArrayLengths()
    {
        // Arrange
        var samples = GenerateSineWave(440, 1.0, SampleRate, 4096);

        // Act
        var (magnitudes, frequencies) = FftProcessor.Compute(samples, SampleRate, DefaultFftSize);

        // Assert - FFT returns fftSize/2 + 1 bins (DC through Nyquist inclusive)
        var expectedLength = DefaultFftSize / 2 + 1;
        magnitudes.Should().HaveCount(expectedLength);
        frequencies.Should().HaveCount(expectedLength);
    }

    [Fact]
    public void Compute_FrequenciesStartAtZero()
    {
        // Arrange
        var samples = GenerateSineWave(440, 1.0, SampleRate, 4096);

        // Act
        var (_, frequencies) = FftProcessor.Compute(samples, SampleRate, DefaultFftSize);

        // Assert
        frequencies[0].Should().BeApproximately(0.0, 0.001);
    }

    [Fact]
    public void Compute_FrequenciesEndAtNyquist()
    {
        // Arrange
        var samples = GenerateSineWave(440, 1.0, SampleRate, 4096);
        var nyquistFreq = SampleRate / 2.0;

        // Act
        var (_, frequencies) = FftProcessor.Compute(samples, SampleRate, DefaultFftSize);

        // Assert - Last frequency should be exactly at Nyquist (DC + fftSize/2 bins)
        var lastFreq = frequencies[^1];
        lastFreq.Should().BeApproximately(nyquistFreq, 0.001);
    }

    [Fact]
    public void Compute_DetectsSineWaveAtCorrectFrequency()
    {
        // Arrange
        const double targetFreq = 440.0;
        var samples = GenerateSineWave(targetFreq, 1.0, SampleRate, 4096);

        // Act
        var (magnitudes, frequencies) = FftProcessor.Compute(samples, SampleRate, DefaultFftSize);

        // Assert - Find the peak
        var peakIndex = Array.IndexOf(magnitudes, magnitudes.Max());
        var detectedFreq = frequencies[peakIndex];

        // Tolerance based on FFT bin resolution
        var binResolution = (double)SampleRate / DefaultFftSize;
        detectedFreq.Should().BeApproximately(targetFreq, binResolution * 2);
    }

    [Fact]
    public void Compute_MagnitudesAreNonNegative()
    {
        // Arrange
        var samples = GenerateSineWave(440, 1.0, SampleRate, 4096);

        // Act
        var (magnitudes, _) = FftProcessor.Compute(samples, SampleRate, DefaultFftSize);

        // Assert
        magnitudes.Should().OnlyContain(m => m >= 0 && !double.IsNaN(m) && !double.IsInfinity(m));
    }

    [Fact]
    public void Compute_WithDifferentWindowTypes_ProducesDifferentResults()
    {
        // Arrange
        var samples = GenerateSineWave(440, 1.0, SampleRate, 4096);

        // Act
        var (hammingMags, _) = FftProcessor.Compute(samples, SampleRate, DefaultFftSize, WindowType.Hamming);
        var (hannMags, _) = FftProcessor.Compute(samples, SampleRate, DefaultFftSize, WindowType.Hann);
        var (rectMags, _) = FftProcessor.Compute(samples, SampleRate, DefaultFftSize, WindowType.Rectangular);

        // Assert - Results should differ due to windowing effects
        hammingMags.Should().NotBeEquivalentTo(hannMags);
        hammingMags.Should().NotBeEquivalentTo(rectMags);
    }

    [Fact]
    public void Compute_ZeroPadsToFftSize()
    {
        // Arrange - Samples shorter than FFT size
        var samples = GenerateSineWave(440, 1.0, SampleRate, 1024);
        var expectedLength = DefaultFftSize / 2 + 1;

        // Act
        var (magnitudes, frequencies) = FftProcessor.Compute(samples, SampleRate, DefaultFftSize);

        // Assert - Should still produce correct output length
        magnitudes.Should().HaveCount(expectedLength);
        frequencies.Should().HaveCount(expectedLength);
    }

    [Fact]
    public void Compute_TruncatesToFftSize()
    {
        // Arrange - Samples longer than FFT size
        var samples = GenerateSineWave(440, 1.0, SampleRate, DefaultFftSize * 2);
        var expectedLength = DefaultFftSize / 2 + 1;

        // Act
        var (magnitudes, frequencies) = FftProcessor.Compute(samples, SampleRate, DefaultFftSize);

        // Assert - Should produce correct output length
        magnitudes.Should().HaveCount(expectedLength);
        frequencies.Should().HaveCount(expectedLength);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Generates a sine wave at the specified frequency.
    /// </summary>
    private static double[] GenerateSineWave(double frequency, double amplitude, int sampleRate, int length)
    {
        var samples = new double[length];
        var angularFreq = 2.0 * Math.PI * frequency / sampleRate;

        for (int i = 0; i < length; i++)
        {
            samples[i] = amplitude * Math.Sin(angularFreq * i);
        }

        return samples;
    }

    #endregion
}
