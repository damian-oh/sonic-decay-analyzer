using FluentAssertions;
using SonicDecay.App.Services.Spectral;
using Xunit;

namespace SonicDecay.App.Tests.Services.Spectral;

/// <summary>
/// Unit tests for WindowFunctions class.
/// Tests window generation for FFT preprocessing.
/// </summary>
public class WindowFunctionsTests
{
    #region GenerateWindow Tests

    [Fact]
    public void GenerateWindow_WithZeroLength_ReturnsEmptyArray()
    {
        // Act
        var result = WindowFunctions.GenerateWindow(0, WindowType.Hamming);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void GenerateWindow_WithNegativeLength_ReturnsEmptyArray()
    {
        // Act
        var result = WindowFunctions.GenerateWindow(-5, WindowType.Hamming);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void GenerateWindow_WithSingleSample_ReturnsOneForAllWindowTypes()
    {
        // Act & Assert
        foreach (var windowType in Enum.GetValues<WindowType>())
        {
            var result = WindowFunctions.GenerateWindow(1, windowType);

            result.Should().HaveCount(1);
            result[0].Should().Be(1.0, because: $"{windowType} window should return 1.0 for single sample");
        }
    }

    [Fact]
    public void GenerateWindow_Hamming_ReturnsCorrectLength()
    {
        // Arrange
        const int length = 512;

        // Act
        var result = WindowFunctions.GenerateWindow(length, WindowType.Hamming);

        // Assert
        result.Should().HaveCount(length);
    }

    [Fact]
    public void GenerateWindow_Hamming_ValuesAreInValidRange()
    {
        // Arrange
        const int length = 512;

        // Act
        var result = WindowFunctions.GenerateWindow(length, WindowType.Hamming);

        // Assert - Hamming window values should be between 0.08 and 1.0
        result.Should().OnlyContain(v => v >= 0.08 && v <= 1.0);
    }

    [Fact]
    public void GenerateWindow_Hann_EndpointsAreZero()
    {
        // Arrange
        const int length = 512;

        // Act
        var result = WindowFunctions.GenerateWindow(length, WindowType.Hann);

        // Assert - Hann window has zero endpoints
        result[0].Should().BeApproximately(0.0, 0.0001);
        result[length - 1].Should().BeApproximately(0.0, 0.0001);
    }

    [Fact]
    public void GenerateWindow_Rectangular_AllValuesAreOne()
    {
        // Arrange
        const int length = 128;

        // Act
        var result = WindowFunctions.GenerateWindow(length, WindowType.Rectangular);

        // Assert
        result.Should().OnlyContain(v => v == 1.0);
    }

    [Fact]
    public void GenerateWindow_Blackman_HasLowerSidelobesThanHamming()
    {
        // Arrange
        const int length = 1024;

        // Act
        var blackman = WindowFunctions.GenerateWindow(length, WindowType.Blackman);
        var hamming = WindowFunctions.GenerateWindow(length, WindowType.Hamming);

        // Assert - Blackman endpoints should be closer to zero than Hamming
        // (indicating better sidelobe suppression)
        blackman[0].Should().BeLessThan(hamming[0]);
    }

    #endregion

    #region Apply Tests

    [Fact]
    public void Apply_WithNullSamples_ReturnsEmptyArray()
    {
        // Act
        var result = WindowFunctions.Apply(null!, WindowType.Hamming);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Apply_WithEmptySamples_ReturnsEmptyArray()
    {
        // Act
        var result = WindowFunctions.Apply(Array.Empty<double>(), WindowType.Hamming);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Apply_WithValidSamples_ReturnsCorrectLength()
    {
        // Arrange
        var samples = new double[256];
        Array.Fill(samples, 1.0);

        // Act
        var result = WindowFunctions.Apply(samples, WindowType.Hamming);

        // Assert
        result.Should().HaveCount(256);
    }

    [Fact]
    public void Apply_Rectangular_PreservesOriginalValues()
    {
        // Arrange
        var samples = new double[] { 0.1, 0.5, -0.3, 0.8 };

        // Act
        var result = WindowFunctions.Apply(samples, WindowType.Rectangular);

        // Assert
        result.Should().BeEquivalentTo(samples);
    }

    [Fact]
    public void Apply_DoesNotModifyOriginalArray()
    {
        // Arrange
        var samples = new double[] { 1.0, 1.0, 1.0, 1.0 };
        var original = samples.ToArray();

        // Act
        WindowFunctions.Apply(samples, WindowType.Hamming);

        // Assert
        samples.Should().BeEquivalentTo(original);
    }

    #endregion

    #region ParseWindowType Tests

    [Theory]
    [InlineData("hamming", WindowType.Hamming)]
    [InlineData("HAMMING", WindowType.Hamming)]
    [InlineData("hann", WindowType.Hann)]
    [InlineData("hanning", WindowType.Hann)]
    [InlineData("blackman", WindowType.Blackman)]
    [InlineData("rectangular", WindowType.Rectangular)]
    [InlineData("rect", WindowType.Rectangular)]
    [InlineData("none", WindowType.Rectangular)]
    public void ParseWindowType_WithValidName_ReturnsCorrectType(string name, WindowType expected)
    {
        // Act
        var result = WindowFunctions.ParseWindowType(name);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("invalid")]
    [InlineData("unknown")]
    public void ParseWindowType_WithInvalidName_ReturnsHammingDefault(string? name)
    {
        // Act
        var result = WindowFunctions.ParseWindowType(name);

        // Assert
        result.Should().Be(WindowType.Hamming);
    }

    #endregion
}
