using SonicDecay.App.Models;
using SonicDecay.App.Services.Interfaces;

namespace SonicDecay.App.Services.Implementations
{
    /// <summary>
    /// Coordinates spectral analysis with database persistence for measurement tracking.
    /// Implements the automated metric persistence required by Phase 3.
    /// </summary>
    public class MeasurementService : IMeasurementService
    {
        private readonly IAnalysisService _analysisService;
        private readonly IStringBaselineRepository _baselineRepository;
        private readonly IMeasurementLogRepository _measurementLogRepository;

        /// <summary>
        /// Initializes a new instance of the MeasurementService class.
        /// </summary>
        /// <param name="analysisService">The spectral analysis service.</param>
        /// <param name="baselineRepository">The string baseline repository.</param>
        /// <param name="measurementLogRepository">The measurement log repository.</param>
        public MeasurementService(
            IAnalysisService analysisService,
            IStringBaselineRepository baselineRepository,
            IMeasurementLogRepository measurementLogRepository)
        {
            _analysisService = analysisService ?? throw new ArgumentNullException(nameof(analysisService));
            _baselineRepository = baselineRepository ?? throw new ArgumentNullException(nameof(baselineRepository));
            _measurementLogRepository = measurementLogRepository ?? throw new ArgumentNullException(nameof(measurementLogRepository));
        }

        /// <inheritdoc />
        public Task<MeasurementResult> MeasureAsync(
            AudioBuffer buffer,
            int baselineId,
            double playTimeHours = 0,
            string? note = null,
            CancellationToken cancellationToken = default)
        {
            return MeasureAsync(
                buffer.Samples,
                buffer.SampleRate,
                baselineId,
                playTimeHours,
                note,
                cancellationToken);
        }

        /// <inheritdoc />
        public async Task<MeasurementResult> MeasureAsync(
            float[] samples,
            int sampleRate,
            int baselineId,
            double playTimeHours = 0,
            string? note = null,
            CancellationToken cancellationToken = default)
        {
            // Retrieve baseline for reference metrics
            var baseline = await _baselineRepository.GetByIdAsync(baselineId);
            if (baseline == null)
            {
                return MeasurementResult.Fail($"Baseline with ID {baselineId} not found");
            }

            // Check if baseline has been established
            if (baseline.InitialCentroid <= 0)
            {
                return MeasurementResult.Fail(
                    "Baseline has not been established. Call EstablishBaselineAsync first.");
            }

            // Configure analysis with baseline reference
            var options = new AnalysisOptions
            {
                ExpectedFundamental = baseline.FundamentalFreq,
                InitialCentroid = baseline.InitialCentroid,
            };

            // Run spectral analysis
            var analysisResult = await _analysisService.AnalyzeAsync(
                samples,
                sampleRate,
                options,
                cancellationToken);

            if (!analysisResult.Success)
            {
                return MeasurementResult.Fail(
                    $"Analysis failed: {analysisResult.ErrorMessage}",
                    analysisResult);
            }

            // Create measurement log entity
            var log = new MeasurementLog
            {
                BaselineId = baselineId,
                CurrentCentroid = analysisResult.SpectralCentroid,
                CurrentHighRatio = analysisResult.HfEnergyRatio,
                DecayPercentage = analysisResult.DecayPercentage ?? 0,
                PlayTimeHours = playTimeHours,
                Note = note,
                MeasuredAt = DateTime.Now,
            };

            // Persist to database
            try
            {
                await _measurementLogRepository.CreateAsync(log);
            }
            catch (Exception ex)
            {
                return MeasurementResult.Fail(
                    $"Database persistence failed: {ex.Message}",
                    analysisResult);
            }

            return MeasurementResult.Ok(log, analysisResult);
        }

        /// <inheritdoc />
        public async Task<bool> EstablishBaselineAsync(
            AudioBuffer buffer,
            int baselineId,
            CancellationToken cancellationToken = default)
        {
            // Retrieve existing baseline
            var baseline = await _baselineRepository.GetByIdAsync(baselineId);
            if (baseline == null)
            {
                return false;
            }

            // Configure analysis for fresh string capture
            var options = new AnalysisOptions
            {
                ExpectedFundamental = baseline.FundamentalFreq > 0
                    ? baseline.FundamentalFreq
                    : null,
            };

            // Run spectral analysis
            var result = await _analysisService.AnalyzeAsync(
                buffer,
                options,
                cancellationToken);

            if (!result.Success)
            {
                return false;
            }

            // Update baseline with fresh string fingerprint
            baseline.InitialCentroid = result.SpectralCentroid;
            baseline.InitialHighRatio = result.HfEnergyRatio;

            // Update fundamental if auto-detected
            if (baseline.FundamentalFreq <= 0 && result.FundamentalFreq > 0)
            {
                baseline.FundamentalFreq = result.FundamentalFreq;
            }

            try
            {
                await _baselineRepository.UpdateAsync(baseline);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <inheritdoc />
        public async Task<List<MeasurementLog>> GetDecayTrendAsync(int baselineId, int limit = 0)
        {
            var logs = await _measurementLogRepository.GetByBaselineIdAsync(baselineId);

            if (limit > 0 && logs.Count > limit)
            {
                return logs.Take(limit).ToList();
            }

            return logs;
        }
    }
}
