using SonicDecay.App.Services.Interfaces;
using SonicDecay.App.Services.Spectral;

namespace SonicDecay.App.Services.Implementations
{
    /// <summary>
    /// Native C# implementation of spectral analysis using FftSharp.
    /// Cross-platform compatible (iOS, Android, Windows, macOS).
    /// No Python dependencies required.
    /// </summary>
    public class NativeAnalysisService : IAnalysisService
    {
        /// <summary>
        /// Native engine version identifier.
        /// </summary>
        private const string EngineVersion = "Native C# 1.0.0";

        /// <summary>
        /// Default analysis timeout in milliseconds.
        /// </summary>
        private int _timeoutMs = 5000;

        /// <inheritdoc />
        /// <remarks>
        /// For native C# implementation, PythonPath is not used.
        /// Property exists for interface compatibility.
        /// </remarks>
        public string PythonPath { get; set; } = string.Empty;

        /// <inheritdoc />
        /// <remarks>
        /// For native C# implementation, EnginePath is not used.
        /// Property exists for interface compatibility.
        /// </remarks>
        public string EnginePath { get; set; } = string.Empty;

        /// <inheritdoc />
        public int TimeoutMs
        {
            get => _timeoutMs;
            set => _timeoutMs = Math.Max(100, value);
        }

        /// <inheritdoc />
        public Task<SpectralAnalysisResult> AnalyzeAsync(
            float[] samples,
            int sampleRate,
            AnalysisOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            options ??= new AnalysisOptions();

            try
            {
                // Validate input
                if (samples == null || samples.Length == 0)
                {
                    return Task.FromResult(CreateErrorResult(
                        sampleRate,
                        options.FftSize,
                        "Empty or null sample buffer"));
                }

                if (sampleRate <= 0)
                {
                    return Task.FromResult(CreateErrorResult(
                        sampleRate,
                        options.FftSize,
                        "Invalid sample rate"));
                }

                cancellationToken.ThrowIfCancellationRequested();

                // Convert float[] to double[] for processing
                double[] doubleSamples = ConvertToDouble(samples);

                // Parse window type from options
                WindowType windowType = WindowFunctions.ParseWindowType(options.WindowType);

                // Step 1: Compute FFT
                var (magnitudes, frequencies) = FftProcessor.Compute(
                    doubleSamples,
                    sampleRate,
                    options.FftSize,
                    windowType);

                if (magnitudes.Length == 0)
                {
                    return Task.FromResult(CreateErrorResult(
                        sampleRate,
                        options.FftSize,
                        "FFT computation returned empty result"));
                }

                cancellationToken.ThrowIfCancellationRequested();

                // Step 2: Calculate spectral centroid
                double centroid = SpectralMetrics.CalculateCentroid(magnitudes, frequencies);

                // Step 3: Find or use fundamental frequency
                double fundamentalFreq;
                double fundamentalMagnitude;

                if (options.ExpectedFundamental.HasValue && options.ExpectedFundamental.Value > 0)
                {
                    fundamentalFreq = options.ExpectedFundamental.Value;
                    // Get magnitude at expected fundamental
                    var (_, mag) = SpectralMetrics.FindFundamental(
                        magnitudes,
                        frequencies,
                        options.ExpectedFundamental);
                    fundamentalMagnitude = mag;
                }
                else
                {
                    // Auto-detect fundamental
                    var (freq, mag) = SpectralMetrics.FindFundamental(magnitudes, frequencies);
                    fundamentalFreq = freq;
                    fundamentalMagnitude = mag;
                }

                // Step 4: Calculate HF energy ratio
                double hfRatio = 0.0;
                if (fundamentalFreq > 0)
                {
                    hfRatio = SpectralMetrics.CalculateHfRatio(
                        magnitudes,
                        frequencies,
                        fundamentalFreq,
                        options.HfLow,
                        options.HfHigh);
                }

                // Step 5: Calculate decay percentage if baseline provided
                double? decayPercentage = null;
                if (options.InitialCentroid.HasValue && options.InitialCentroid.Value > 0)
                {
                    decayPercentage = SpectralMetrics.CalculateDecay(
                        options.InitialCentroid.Value,
                        centroid);
                }

                return Task.FromResult(new SpectralAnalysisResult
                {
                    SpectralCentroid = centroid,
                    HfEnergyRatio = hfRatio,
                    FundamentalFreq = fundamentalFreq,
                    FundamentalMagnitude = fundamentalMagnitude,
                    DecayPercentage = decayPercentage,
                    SampleRate = sampleRate,
                    FftSize = options.FftSize,
                    Success = true,
                    ErrorMessage = null
                });
            }
            catch (OperationCanceledException)
            {
                return Task.FromResult(CreateErrorResult(
                    sampleRate,
                    options?.FftSize ?? FftProcessor.DefaultFftSize,
                    "Analysis cancelled"));
            }
            catch (Exception ex)
            {
                return Task.FromResult(CreateErrorResult(
                    sampleRate,
                    options?.FftSize ?? FftProcessor.DefaultFftSize,
                    $"Analysis failed: {ex.Message}"));
            }
        }

        /// <inheritdoc />
        public Task<SpectralAnalysisResult> AnalyzeAsync(
            AudioBuffer buffer,
            AnalysisOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            return AnalyzeAsync(buffer.Samples, buffer.SampleRate, options, cancellationToken);
        }

        /// <inheritdoc />
        /// <remarks>
        /// Native C# implementation is always available on all platforms.
        /// </remarks>
        public Task<bool> IsEngineAvailableAsync()
        {
            return Task.FromResult(true);
        }

        /// <inheritdoc />
        public Task<string?> GetEngineVersionAsync()
        {
            return Task.FromResult<string?>(EngineVersion);
        }

        /// <summary>
        /// Converts float array to double array for FFT processing.
        /// Invalid values (NaN, Infinity) are replaced with 0.0 (silence).
        /// </summary>
        /// <param name="samples">Input float samples.</param>
        /// <returns>Double array with sanitized values.</returns>
        private static double[] ConvertToDouble(float[] samples)
        {
            double[] result = new double[samples.Length];

            for (int i = 0; i < samples.Length; i++)
            {
                float sample = samples[i];
                // Replace invalid audio samples with silence
                if (float.IsNaN(sample) || float.IsInfinity(sample))
                {
                    result[i] = 0.0;
                }
                else
                {
                    result[i] = sample;
                }
            }

            return result;
        }

        /// <summary>
        /// Creates an error result with the specified message.
        /// </summary>
        /// <param name="sampleRate">Sample rate for the result.</param>
        /// <param name="fftSize">FFT size for the result.</param>
        /// <param name="message">Error message.</param>
        /// <returns>SpectralAnalysisResult with Success = false.</returns>
        private static SpectralAnalysisResult CreateErrorResult(
            int sampleRate,
            int fftSize,
            string message)
        {
            return new SpectralAnalysisResult
            {
                SpectralCentroid = 0,
                HfEnergyRatio = 0,
                FundamentalFreq = 0,
                FundamentalMagnitude = 0,
                DecayPercentage = null,
                SampleRate = sampleRate,
                FftSize = fftSize,
                Success = false,
                ErrorMessage = message
            };
        }
    }
}
