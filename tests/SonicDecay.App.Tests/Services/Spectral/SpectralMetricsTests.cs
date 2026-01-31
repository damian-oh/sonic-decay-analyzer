using FluentAssertions;
using SonicDecay.App.Services.Spectral;
using Xunit;

namespace SonicDecay.App.Tests.Services.Spectral;

/// <summary>
/// Unit tests for SpectralMetrics class.
/// Tests spectral analysis calculations for acoustic degradation measurement.
/// </summary>
public class SpectralMetricsTests
{
    #region CalculateCentroid Tests

    [Fact]
    public void CalculateCentroid_WithNullMagnitudes_ThrowsArgumentNullException()
    {
        // Arrange
        var frequencies = new double[] { 100, 200, 300 };

        // Act & Assert
        var act = () => SpectralMetrics.CalculateCentroid(null!, frequencies);
        act.Should().Throw<ArgumentNullException>().WithParameterName("magnitudes");
    }

    [Fact]
    public void CalculateCentroid_WithNullFrequencies_ThrowsArgumentNullException()
    {
        // Arrange
        var magnitudes = new double[] { 1.0, 0.5, 0.25 };

        // Act & Assert
        var act = () => SpectralMetrics.CalculateCentroid(magnitudes, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("frequencies");
    }

    [Fact]
    public void CalculateCentroid_WithMismatchedArrayLengths_ThrowsArgumentException()
    {
        // Arrange
        var magnitudes = new double[] { 1.0, 0.5 };
        var frequencies = new double[] { 100, 200, 300 };

        // Act & Assert
        var act = () => SpectralMetrics.CalculateCentroid(magnitudes, frequencies);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CalculateCentroid_WithEmptyArrays_ThrowsArgumentException()
    {
        // Act & Assert
        var act = () => SpectralMetrics.CalculateCentroid(
            Array.Empty<double>(),
            Array.Empty<double>());
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CalculateCentroid_WithZeroMagnitudes_ReturnsZero()
    {
        // Arrange
        var magnitudes = new double[] { 0, 0, 0, 0 };
        var frequencies = new double[] { 100, 200, 300, 400 };

        // Act
        var result = SpectralMetrics.CalculateCentroid(magnitudes, frequencies);

        // Assert
        result.Should().Be(0.0);
    }

    [Fact]
    public void CalculateCentroid_WithSinglePeak_ReturnsFrequencyOfPeak()
    {
        // Arrange - Single peak at 440 Hz
        var magnitudes = new double[] { 0, 0, 1.0, 0, 0 };
        var frequencies = new double[] { 200, 300, 440, 500, 600 };

        // Act
        var result = SpectralMetrics.CalculateCentroid(magnitudes, frequencies);

        // Assert
        result.Should().BeApproximately(440, 0.001);
    }

    [Fact]
    public void CalculateCentroid_WithUniformMagnitudes_ReturnsCenterFrequency()
    {
        // Arrange - Uniform magnitudes, center should be average
        var magnitudes = new double[] { 1.0, 1.0, 1.0, 1.0, 1.0 };
        var frequencies = new double[] { 100, 200, 300, 400, 500 };
        var expectedCentroid = 300.0; // (100+200+300+400+500)/5

        // Act
        var result = SpectralMetrics.CalculateCentroid(magnitudes, frequencies);

        // Assert
        result.Should().BeApproximately(expectedCentroid, 0.001);
    }

    [Fact]
    public void CalculateCentroid_FiltersOutOfRangeFrequencies()
    {
        // Arrange - Include frequencies outside audible range
        var magnitudes = new double[] { 1.0, 1.0, 1.0, 1.0 };
        var frequencies = new double[] { 10, 100, 1000, 25000 }; // 10 Hz and 25 kHz outside default range

        // Act
        var result = SpectralMetrics.CalculateCentroid(magnitudes, frequencies, 20, 20000);

        // Assert - Should only consider 100 and 1000 Hz
        var expected = (100 * 1.0 + 1000 * 1.0) / (1.0 + 1.0);
        result.Should().BeApproximately(expected, 0.001);
    }

    [Fact]
    public void CalculateCentroid_WithNaNValues_SkipsInvalidEntries()
    {
        // Arrange - Include NaN in magnitudes
        var magnitudes = new double[] { 1.0, double.NaN, 1.0, double.PositiveInfinity };
        var frequencies = new double[] { 100, 200, 300, 400 };

        // Act
        var result = SpectralMetrics.CalculateCentroid(magnitudes, frequencies);

        // Assert - Should only consider valid entries (100 Hz and 300 Hz)
        var expected = (100 * 1.0 + 300 * 1.0) / (1.0 + 1.0);
        result.Should().BeApproximately(expected, 0.001);
    }

    #endregion

    #region CalculateHfRatio Tests

    [Fact]
    public void CalculateHfRatio_WithZeroFundamental_ReturnsZero()
    {
        // Arrange
        var magnitudes = new double[] { 1.0, 0.5, 0.25 };
        var frequencies = new double[] { 100, 5000, 10000 };

        // Act
        var result = SpectralMetrics.CalculateHfRatio(magnitudes, frequencies, fundamentalFreq: 0);

        // Assert
        result.Should().Be(0.0);
    }

    [Fact]
    public void CalculateHfRatio_WithNegativeFundamental_ReturnsZero()
    {
        // Arrange
        var magnitudes = new double[] { 1.0, 0.5, 0.25 };
        var frequencies = new double[] { 100, 5000, 10000 };

        // Act
        var result = SpectralMetrics.CalculateHfRatio(magnitudes, frequencies, fundamentalFreq: -100);

        // Assert
        result.Should().Be(0.0);
    }

    [Fact]
    public void CalculateHfRatio_WithZeroFundamentalEnergy_ReturnsZero()
    {
        // Arrange - No energy at fundamental
        var magnitudes = new double[] { 0, 0.5, 0.25 };
        var frequencies = new double[] { 440, 5000, 10000 };

        // Act
        var result = SpectralMetrics.CalculateHfRatio(magnitudes, frequencies, fundamentalFreq: 440);

        // Assert
        result.Should().Be(0.0);
    }

    [Fact]
    public void CalculateHfRatio_WithOnlyFundamental_ReturnsZero()
    {
        // Arrange - Energy only at fundamental, none in HF band
        var magnitudes = new double[] { 1.0, 0, 0 };
        var frequencies = new double[] { 440, 5000, 10000 };

        // Act
        var result = SpectralMetrics.CalculateHfRatio(magnitudes, frequencies, fundamentalFreq: 440);

        // Assert
        result.Should().Be(0.0);
    }

    [Fact]
    public void CalculateHfRatio_ReturnsCorrectRatio()
    {
        // Arrange
        var magnitudes = new double[] { 2.0, 1.0, 1.0 };
        var frequencies = new double[] { 440, 6000, 10000 };
        var expectedRatio = 2.0 / 2.0; // HF energy = 2.0, Fundamental = 2.0

        // Act
        var result = SpectralMetrics.CalculateHfRatio(
            magnitudes, frequencies,
            fundamentalFreq: 440,
            hfLow: 5000,
            hfHigh: 15000,
            fundamentalBandwidth: 50);

        // Assert
        result.Should().BeApproximately(expectedRatio, 0.001);
    }

    #endregion

    #region CalculateDecay Tests

    [Fact]
    public void CalculateDecay_WithZeroInitialCentroid_ReturnsZero()
    {
        // Act
        var result = SpectralMetrics.CalculateDecay(initialCentroid: 0, currentCentroid: 500);

        // Assert
        result.Should().Be(0.0);
    }

    [Fact]
    public void CalculateDecay_WithNegativeInitialCentroid_ReturnsZero()
    {
        // Act
        var result = SpectralMetrics.CalculateDecay(initialCentroid: -100, currentCentroid: 500);

        // Assert
        result.Should().Be(0.0);
    }

    [Fact]
    public void CalculateDecay_WithNaNInitialCentroid_ReturnsZero()
    {
        // Act
        var result = SpectralMetrics.CalculateDecay(initialCentroid: double.NaN, currentCentroid: 500);

        // Assert
        result.Should().Be(0.0);
    }

    [Fact]
    public void CalculateDecay_WithInfinityInitialCentroid_ReturnsZero()
    {
        // Act
        var result = SpectralMetrics.CalculateDecay(
            initialCentroid: double.PositiveInfinity, currentCentroid: 500);

        // Assert
        result.Should().Be(0.0);
    }

    [Fact]
    public void CalculateDecay_WithNaNCurrentCentroid_ReturnsZero()
    {
        // Act
        var result = SpectralMetrics.CalculateDecay(initialCentroid: 1000, currentCentroid: double.NaN);

        // Assert
        result.Should().Be(0.0);
    }

    [Fact]
    public void CalculateDecay_WithEqualCentroids_ReturnsZeroDecay()
    {
        // Act
        var result = SpectralMetrics.CalculateDecay(initialCentroid: 1000, currentCentroid: 1000);

        // Assert
        result.Should().Be(0.0);
    }

    [Fact]
    public void CalculateDecay_WithDarkerTimbre_ReturnsPositiveDecay()
    {
        // Arrange - Current centroid is lower (darker sound)
        double initial = 1000;
        double current = 800;
        double expectedDecay = ((1000 - 800) / 1000.0) * 100.0; // 20%

        // Act
        var result = SpectralMetrics.CalculateDecay(initial, current);

        // Assert
        result.Should().BeApproximately(expectedDecay, 0.001);
    }

    [Fact]
    public void CalculateDecay_WithBrighterTimbre_ReturnsNegativeDecay()
    {
        // Arrange - Current centroid is higher (brighter sound)
        double initial = 1000;
        double current = 1200;
        double expectedDecay = ((1000 - 1200) / 1000.0) * 100.0; // -20%

        // Act
        var result = SpectralMetrics.CalculateDecay(initial, current);

        // Assert
        result.Should().BeApproximately(expectedDecay, 0.001);
    }

    [Fact]
    public void CalculateDecay_CanExceed100Percent()
    {
        // Arrange - Extreme degradation where current is much lower
        double initial = 1000;
        double current = -500; // Extreme case

        // Act
        var result = SpectralMetrics.CalculateDecay(initial, current);

        // Assert - Should be 150% decay
        result.Should().BeGreaterThan(100);
    }

    #endregion

    #region FindFundamental Tests

    [Fact]
    public void FindFundamental_WithNullArrays_ReturnsZero()
    {
        // Act
        var (freq, mag) = SpectralMetrics.FindFundamental(null!, null!);

        // Assert
        freq.Should().Be(0.0);
        mag.Should().Be(0.0);
    }

    [Fact]
    public void FindFundamental_WithEmptyArrays_ReturnsZero()
    {
        // Act
        var (freq, mag) = SpectralMetrics.FindFundamental(
            Array.Empty<double>(),
            Array.Empty<double>());

        // Assert
        freq.Should().Be(0.0);
        mag.Should().Be(0.0);
    }

    [Fact]
    public void FindFundamental_FindsLargestPeakInRange()
    {
        // Arrange - Multiple peaks, largest at 440 Hz
        var magnitudes = new double[] { 0.3, 1.0, 0.5, 0.2 };
        var frequencies = new double[] { 200, 440, 600, 800 };

        // Act
        var (freq, mag) = SpectralMetrics.FindFundamental(magnitudes, frequencies);

        // Assert
        freq.Should().BeApproximately(440, 0.001);
        mag.Should().BeApproximately(1.0, 0.001);
    }

    [Fact]
    public void FindFundamental_WithExpectedFrequency_SearchesNearExpected()
    {
        // Arrange - Peak at 440 Hz, search around 440 Hz
        var magnitudes = new double[] { 0.8, 1.0, 0.3, 0.1 };
        var frequencies = new double[] { 100, 440, 800, 1000 };

        // Act
        var (freq, mag) = SpectralMetrics.FindFundamental(
            magnitudes, frequencies,
            expectedFreq: 440,
            searchTolerance: 0.1);

        // Assert
        freq.Should().BeApproximately(440, 0.001);
    }

    [Fact]
    public void FindFundamental_WithExpectedFrequency_IgnoresPeaksOutsideTolerance()
    {
        // Arrange - Larger peak at 100 Hz but outside tolerance of expected 440 Hz
        var magnitudes = new double[] { 2.0, 1.0, 0.3 };
        var frequencies = new double[] { 100, 440, 800 };

        // Act
        var (freq, mag) = SpectralMetrics.FindFundamental(
            magnitudes, frequencies,
            expectedFreq: 440,
            searchTolerance: 0.1); // 10% tolerance = 396-484 Hz

        // Assert - Should find 440 Hz, not the larger 100 Hz peak
        freq.Should().BeApproximately(440, 0.001);
    }

    [Fact]
    public void FindFundamental_WithNoPeakInRange_ReturnsZero()
    {
        // Arrange - All frequencies outside search range
        var magnitudes = new double[] { 1.0, 0.5 };
        var frequencies = new double[] { 10, 25000 }; // Below min and above max

        // Act
        var (freq, mag) = SpectralMetrics.FindFundamental(magnitudes, frequencies);

        // Assert
        freq.Should().Be(0.0);
        mag.Should().Be(0.0);
    }

    #endregion
}
