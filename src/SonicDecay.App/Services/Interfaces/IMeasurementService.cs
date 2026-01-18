using SonicDecay.App.Models;

namespace SonicDecay.App.Services.Interfaces
{
    /// <summary>
    /// Result of a measurement operation combining analysis and persistence.
    /// </summary>
    public class MeasurementResult
    {
        /// <summary>
        /// Gets or sets a value indicating whether the measurement succeeded.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the persisted measurement log entity.
        /// Null if Success is false.
        /// </summary>
        public MeasurementLog? Log { get; set; }

        /// <summary>
        /// Gets or sets the spectral analysis result.
        /// </summary>
        public SpectralAnalysisResult? AnalysisResult { get; set; }

        /// <summary>
        /// Gets or sets the error message if measurement failed.
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Creates a successful measurement result.
        /// </summary>
        public static MeasurementResult Ok(MeasurementLog log, SpectralAnalysisResult analysis)
        {
            return new MeasurementResult
            {
                Success = true,
                Log = log,
                AnalysisResult = analysis,
                ErrorMessage = null,
            };
        }

        /// <summary>
        /// Creates a failed measurement result.
        /// </summary>
        public static MeasurementResult Fail(string error, SpectralAnalysisResult? analysis = null)
        {
            return new MeasurementResult
            {
                Success = false,
                Log = null,
                AnalysisResult = analysis,
                ErrorMessage = error,
            };
        }
    }

    /// <summary>
    /// Coordinates audio analysis and metric persistence to MeasurementLog.
    /// This is the primary interface for capturing string degradation data.
    /// </summary>
    public interface IMeasurementService
    {
        /// <summary>
        /// Captures and analyzes an audio buffer, persisting results to the database.
        /// This is the main entry point for measuring string degradation.
        /// </summary>
        /// <param name="buffer">The captured audio buffer.</param>
        /// <param name="baselineId">The StringBaseline ID to measure against.</param>
        /// <param name="playTimeHours">User-reported play time since last measurement.</param>
        /// <param name="note">Optional user annotation for this measurement.</param>
        /// <param name="cancellationToken">Cancellation token for timeout handling.</param>
        /// <returns>Measurement result containing the persisted log and analysis data.</returns>
        Task<MeasurementResult> MeasureAsync(
            AudioBuffer buffer,
            int baselineId,
            double playTimeHours = 0,
            string? note = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Captures and analyzes raw audio samples, persisting results to the database.
        /// </summary>
        /// <param name="samples">Normalized audio samples (-1 to 1).</param>
        /// <param name="sampleRate">Sample rate in Hz.</param>
        /// <param name="baselineId">The StringBaseline ID to measure against.</param>
        /// <param name="playTimeHours">User-reported play time since last measurement.</param>
        /// <param name="note">Optional user annotation for this measurement.</param>
        /// <param name="cancellationToken">Cancellation token for timeout handling.</param>
        /// <returns>Measurement result containing the persisted log and analysis data.</returns>
        Task<MeasurementResult> MeasureAsync(
            float[] samples,
            int sampleRate,
            int baselineId,
            double playTimeHours = 0,
            string? note = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Establishes a baseline measurement for a new or replaced string.
        /// This captures the "fresh string" spectral fingerprint.
        /// </summary>
        /// <param name="buffer">The captured audio buffer.</param>
        /// <param name="baselineId">The StringBaseline ID to update.</param>
        /// <param name="cancellationToken">Cancellation token for timeout handling.</param>
        /// <returns>True if baseline was successfully established; otherwise, false.</returns>
        Task<bool> EstablishBaselineAsync(
            AudioBuffer buffer,
            int baselineId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the decay trend for a specific baseline.
        /// Returns a list of measurements ordered by date.
        /// </summary>
        /// <param name="baselineId">The StringBaseline ID.</param>
        /// <param name="limit">Maximum number of measurements to return (0 = all).</param>
        /// <returns>List of measurement logs ordered by date descending.</returns>
        Task<List<MeasurementLog>> GetDecayTrendAsync(int baselineId, int limit = 0);
    }
}
