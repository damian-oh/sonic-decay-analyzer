using SonicDecay.App.Models;

namespace SonicDecay.App.Services.Interfaces
{
    /// <summary>
    /// Represents the urgency level for string replacement.
    /// </summary>
    public enum ReplacementUrgency
    {
        /// <summary>Strings are in excellent condition, no action needed.</summary>
        None,

        /// <summary>Monitor strings, replacement may be needed in the future.</summary>
        Monitor,

        /// <summary>Consider replacing strings soon for optimal tone.</summary>
        Soon,

        /// <summary>Strings are significantly degraded, replacement recommended.</summary>
        Recommended,

        /// <summary>Strings are severely degraded, immediate replacement advised.</summary>
        Urgent
    }

    /// <summary>
    /// Contains the result of a replacement recommendation analysis.
    /// </summary>
    public class ReplacementRecommendation
    {
        /// <summary>
        /// Gets or sets the urgency level for replacement.
        /// </summary>
        public ReplacementUrgency Urgency { get; set; }

        /// <summary>
        /// Gets or sets a human-readable recommendation message.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the current decay percentage.
        /// </summary>
        public double CurrentDecayPercentage { get; set; }

        /// <summary>
        /// Gets or sets the decay rate per hour of play time.
        /// Calculated from historical measurements.
        /// </summary>
        public double DecayRatePerHour { get; set; }

        /// <summary>
        /// Gets or sets the projected date when strings should be replaced.
        /// Null if insufficient data or strings are already degraded.
        /// </summary>
        public DateTime? ProjectedReplacementDate { get; set; }

        /// <summary>
        /// Gets or sets the estimated remaining play hours before replacement.
        /// Null if insufficient data or strings are already degraded.
        /// </summary>
        public double? EstimatedHoursRemaining { get; set; }

        /// <summary>
        /// Gets or sets the confidence percentage of the prediction (0-100).
        /// Higher values indicate more reliable predictions based on data quality.
        /// </summary>
        public double ConfidencePercentage { get; set; }

        /// <summary>
        /// Gets or sets the number of measurements used in the analysis.
        /// </summary>
        public int MeasurementCount { get; set; }

        /// <summary>
        /// Gets or sets the total play hours tracked.
        /// </summary>
        public double TotalPlayHours { get; set; }

        /// <summary>
        /// Gets or sets the string health score (0-100).
        /// 100 = fresh strings, 0 = completely degraded.
        /// </summary>
        public double HealthScore { get; set; }

        /// <summary>
        /// Gets or sets factors that influenced the recommendation.
        /// </summary>
        public List<string> Factors { get; set; } = new();

        /// <summary>
        /// Creates a recommendation for strings with no measurable decay.
        /// </summary>
        public static ReplacementRecommendation Excellent(double currentDecay)
        {
            return new ReplacementRecommendation
            {
                Urgency = ReplacementUrgency.None,
                Message = "Strings are in excellent condition",
                CurrentDecayPercentage = currentDecay,
                HealthScore = 100 - currentDecay,
                ConfidencePercentage = 90,
                Factors = new List<string> { "Minimal spectral degradation" }
            };
        }

        /// <summary>
        /// Creates a recommendation indicating insufficient data for analysis.
        /// </summary>
        public static ReplacementRecommendation InsufficientData(int measurementCount)
        {
            return new ReplacementRecommendation
            {
                Urgency = ReplacementUrgency.Monitor,
                Message = "More measurements needed for accurate prediction",
                ConfidencePercentage = 0,
                MeasurementCount = measurementCount,
                Factors = new List<string> { $"Only {measurementCount} measurement(s) available", "Minimum 3 measurements recommended" }
            };
        }
    }

    /// <summary>
    /// Provides predictive maintenance recommendations based on decay trends.
    /// Analyzes historical measurements to project replacement timing.
    /// </summary>
    public interface IRecommendationService
    {
        /// <summary>
        /// Analyzes decay trend for a specific string baseline and generates
        /// a replacement recommendation.
        /// </summary>
        /// <param name="baselineId">The StringBaseline ID to analyze.</param>
        /// <param name="cancellationToken">Cancellation token for async operation.</param>
        /// <returns>A replacement recommendation based on historical data.</returns>
        Task<ReplacementRecommendation> AnalyzeDecayTrendAsync(
            int baselineId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates a recommendation from a provided list of measurements.
        /// Use this when measurements are already loaded.
        /// </summary>
        /// <param name="baseline">The string baseline with initial metrics.</param>
        /// <param name="measurements">Historical measurements ordered by date descending.</param>
        /// <returns>A replacement recommendation based on the provided data.</returns>
        ReplacementRecommendation AnalyzeDecayTrend(
            StringBaseline baseline,
            IReadOnlyList<MeasurementLog> measurements);

        /// <summary>
        /// Calculates the health score for a string based on current decay.
        /// </summary>
        /// <param name="decayPercentage">The current decay percentage.</param>
        /// <returns>Health score from 0 (dead) to 100 (fresh).</returns>
        double CalculateHealthScore(double decayPercentage);

        /// <summary>
        /// Gets the urgency level based on decay percentage thresholds.
        /// </summary>
        /// <param name="decayPercentage">The current decay percentage.</param>
        /// <returns>The appropriate urgency level.</returns>
        ReplacementUrgency GetUrgencyFromDecay(double decayPercentage);

        /// <summary>
        /// Gets or sets the decay threshold for "Soon" urgency level.
        /// Default is 25%.
        /// </summary>
        double SoonThreshold { get; set; }

        /// <summary>
        /// Gets or sets the decay threshold for "Recommended" urgency level.
        /// Default is 40%.
        /// </summary>
        double RecommendedThreshold { get; set; }

        /// <summary>
        /// Gets or sets the decay threshold for "Urgent" urgency level.
        /// Default is 60%.
        /// </summary>
        double UrgentThreshold { get; set; }
    }
}
