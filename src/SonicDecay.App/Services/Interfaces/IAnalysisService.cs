namespace SonicDecay.App.Services.Interfaces
{
    /// <summary>
    /// Represents the result of a spectral analysis operation.
    /// Contains metrics for acoustic degradation measurement.
    /// </summary>
    public class SpectralAnalysisResult
    {
        /// <summary>
        /// Gets or sets the spectral centroid (center of mass) in Hz.
        /// Indicates the "brightness" of the sound. Decreases as strings age.
        /// </summary>
        public double SpectralCentroid { get; set; }

        /// <summary>
        /// Gets or sets the high-frequency energy ratio.
        /// Ratio of energy in 5-15 kHz band relative to fundamental.
        /// Higher values indicate fresher strings with stronger harmonics.
        /// </summary>
        public double HfEnergyRatio { get; set; }

        /// <summary>
        /// Gets or sets the detected fundamental frequency in Hz.
        /// </summary>
        public double FundamentalFreq { get; set; }

        /// <summary>
        /// Gets or sets the magnitude at the fundamental frequency.
        /// </summary>
        public double FundamentalMagnitude { get; set; }

        /// <summary>
        /// Gets or sets the calculated decay percentage relative to baseline.
        /// Null if no baseline was provided. Positive values indicate darkening.
        /// Formula: (InitialCentroid - CurrentCentroid) / InitialCentroid × 100
        /// </summary>
        public double? DecayPercentage { get; set; }

        /// <summary>
        /// Gets or sets the sample rate of the analyzed audio in Hz.
        /// </summary>
        public int SampleRate { get; set; }

        /// <summary>
        /// Gets or sets the FFT size used in analysis.
        /// </summary>
        public int FftSize { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the analysis succeeded.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the error message if analysis failed.
        /// Null when Success is true.
        /// </summary>
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Configuration options for spectral analysis.
    /// </summary>
    public class AnalysisOptions
    {
        /// <summary>
        /// Gets or sets the expected fundamental frequency in Hz.
        /// Used to improve HF ratio calculation accuracy.
        /// If null, fundamental will be auto-detected.
        /// </summary>
        public double? ExpectedFundamental { get; set; }

        /// <summary>
        /// Gets or sets the initial (baseline) centroid for decay calculation.
        /// If provided, DecayPercentage will be calculated.
        /// </summary>
        public double? InitialCentroid { get; set; }

        /// <summary>
        /// Gets or sets the FFT size. Default is 8192 for good frequency resolution.
        /// </summary>
        public int FftSize { get; set; } = 8192;

        /// <summary>
        /// Gets or sets the window function type.
        /// Default is "hamming" for spectral leakage reduction.
        /// </summary>
        public string WindowType { get; set; } = "hamming";

        /// <summary>
        /// Gets or sets the lower bound of the HF band in Hz. Default is 5000.
        /// </summary>
        public double HfLow { get; set; } = 5000.0;

        /// <summary>
        /// Gets or sets the upper bound of the HF band in Hz. Default is 15000.
        /// </summary>
        public double HfHigh { get; set; } = 15000.0;
    }

    /// <summary>
    /// Provides spectral analysis services via the Python DSP engine.
    /// Communicates with the Python backend for FFT-based metric extraction.
    /// </summary>
    public interface IAnalysisService
    {
        /// <summary>
        /// Analyzes an audio buffer and extracts spectral metrics.
        /// </summary>
        /// <param name="samples">Normalized audio samples (-1 to 1).</param>
        /// <param name="sampleRate">Sample rate in Hz.</param>
        /// <param name="options">Optional analysis configuration.</param>
        /// <param name="cancellationToken">Cancellation token for timeout handling.</param>
        /// <returns>Analysis result containing spectral metrics.</returns>
        Task<SpectralAnalysisResult> AnalyzeAsync(
            float[] samples,
            int sampleRate,
            AnalysisOptions? options = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Analyzes an audio buffer from the capture service.
        /// Convenience method that extracts samples from AudioBuffer.
        /// </summary>
        /// <param name="buffer">The captured audio buffer.</param>
        /// <param name="options">Optional analysis configuration.</param>
        /// <param name="cancellationToken">Cancellation token for timeout handling.</param>
        /// <returns>Analysis result containing spectral metrics.</returns>
        Task<SpectralAnalysisResult> AnalyzeAsync(
            AudioBuffer buffer,
            AnalysisOptions? options = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if the Python engine is available and properly configured.
        /// </summary>
        /// <returns>True if the engine can be invoked; otherwise, false.</returns>
        Task<bool> IsEngineAvailableAsync();

        /// <summary>
        /// Gets the version of the Python analysis engine.
        /// </summary>
        /// <returns>Version string, or null if engine is unavailable.</returns>
        Task<string?> GetEngineVersionAsync();

        /// <summary>
        /// Gets or sets the path to the Python executable.
        /// Default searches PATH for "python" or "python3".
        /// </summary>
        string PythonPath { get; set; }

        /// <summary>
        /// Gets or sets the path to the SonicDecay.Engine directory.
        /// </summary>
        string EnginePath { get; set; }

        /// <summary>
        /// Gets or sets the timeout for analysis operations in milliseconds.
        /// Default is 5000ms (5 seconds).
        /// </summary>
        int TimeoutMs { get; set; }
    }
}
