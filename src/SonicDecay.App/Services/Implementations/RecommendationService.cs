using SonicDecay.App.Models;
using SonicDecay.App.Services.Interfaces;

namespace SonicDecay.App.Services.Implementations
{
    /// <summary>
    /// Provides predictive maintenance recommendations based on decay trend analysis.
    /// Uses linear regression on historical measurements to project replacement timing.
    /// </summary>
    public class RecommendationService : IRecommendationService
    {
        private readonly IStringBaselineRepository _baselineRepository;
        private readonly IMeasurementLogRepository _measurementLogRepository;

        private double _soonThreshold = 25.0;
        private double _recommendedThreshold = 40.0;
        private double _urgentThreshold = 60.0;

        /// <summary>
        /// Minimum number of measurements required for trend analysis.
        /// </summary>
        private const int MinMeasurementsForTrend = 3;

        /// <summary>
        /// Target decay percentage at which replacement is recommended.
        /// </summary>
        private const double ReplacementTargetDecay = 50.0;

        /// <summary>
        /// Initializes a new instance of the RecommendationService class.
        /// </summary>
        public RecommendationService(
            IStringBaselineRepository baselineRepository,
            IMeasurementLogRepository measurementLogRepository)
        {
            _baselineRepository = baselineRepository ?? throw new ArgumentNullException(nameof(baselineRepository));
            _measurementLogRepository = measurementLogRepository ?? throw new ArgumentNullException(nameof(measurementLogRepository));
        }

        /// <inheritdoc />
        public double SoonThreshold
        {
            get => _soonThreshold;
            set => _soonThreshold = Math.Clamp(value, 0, 100);
        }

        /// <inheritdoc />
        public double RecommendedThreshold
        {
            get => _recommendedThreshold;
            set => _recommendedThreshold = Math.Clamp(value, 0, 100);
        }

        /// <inheritdoc />
        public double UrgentThreshold
        {
            get => _urgentThreshold;
            set => _urgentThreshold = Math.Clamp(value, 0, 100);
        }

        /// <inheritdoc />
        public async Task<ReplacementRecommendation> AnalyzeDecayTrendAsync(
            int baselineId,
            CancellationToken cancellationToken = default)
        {
            var baseline = await _baselineRepository.GetByIdAsync(baselineId);
            if (baseline == null)
            {
                return new ReplacementRecommendation
                {
                    Urgency = ReplacementUrgency.Monitor,
                    Message = "Baseline not found",
                    ConfidencePercentage = 0,
                    Factors = new List<string> { "Invalid baseline ID" }
                };
            }

            var measurements = await _measurementLogRepository.GetByBaselineIdAsync(baselineId);
            return AnalyzeDecayTrend(baseline, measurements);
        }

        /// <inheritdoc />
        public ReplacementRecommendation AnalyzeDecayTrend(
            StringBaseline baseline,
            IReadOnlyList<MeasurementLog> measurements)
        {
            if (measurements.Count == 0)
            {
                return ReplacementRecommendation.InsufficientData(0);
            }

            // Get most recent measurement for current state
            var latestMeasurement = measurements.OrderByDescending(m => m.MeasuredAt).First();
            var currentDecay = latestMeasurement.DecayPercentage;
            var totalPlayHours = measurements.Sum(m => m.PlayTimeHours);

            // Build base recommendation
            var recommendation = new ReplacementRecommendation
            {
                CurrentDecayPercentage = currentDecay,
                HealthScore = CalculateHealthScore(currentDecay),
                Urgency = GetUrgencyFromDecay(currentDecay),
                MeasurementCount = measurements.Count,
                TotalPlayHours = totalPlayHours
            };

            // Check if already severely degraded
            if (currentDecay >= _urgentThreshold)
            {
                recommendation.Message = "Strings are severely degraded - immediate replacement advised";
                recommendation.ConfidencePercentage = 95;
                recommendation.Factors.Add($"Decay at {currentDecay:F1}% exceeds urgent threshold ({_urgentThreshold}%)");
                return recommendation;
            }

            // Need minimum measurements for trend analysis
            if (measurements.Count < MinMeasurementsForTrend)
            {
                recommendation.Message = GetMessageFromUrgency(recommendation.Urgency, currentDecay);
                recommendation.ConfidencePercentage = 50;
                recommendation.Factors.Add($"Only {measurements.Count} measurement(s) - limited trend data");
                recommendation.Factors.Add($"Current decay: {currentDecay:F1}%");
                return recommendation;
            }

            // Calculate decay rate using linear regression
            var (decayRatePerHour, confidence) = CalculateDecayRate(measurements);
            recommendation.DecayRatePerHour = decayRatePerHour;

            // Project time to replacement threshold
            if (decayRatePerHour > 0.001) // Meaningful decay rate
            {
                var remainingDecay = ReplacementTargetDecay - currentDecay;
                if (remainingDecay > 0)
                {
                    var hoursRemaining = remainingDecay / decayRatePerHour;
                    recommendation.EstimatedHoursRemaining = hoursRemaining;

                    // Estimate date based on average play hours per day (assume 1 hour/day if no data)
                    var avgPlayHoursPerDay = totalPlayHours > 0
                        ? CalculateAveragePlayHoursPerDay(measurements)
                        : 1.0;

                    if (avgPlayHoursPerDay > 0)
                    {
                        var daysRemaining = hoursRemaining / avgPlayHoursPerDay;
                        recommendation.ProjectedReplacementDate = DateTime.Now.AddDays(daysRemaining);
                    }

                    recommendation.Factors.Add($"Decay rate: {decayRatePerHour:F3}% per play hour");
                    recommendation.Factors.Add($"Estimated {hoursRemaining:F0} play hours remaining");

                    if (recommendation.ProjectedReplacementDate.HasValue)
                    {
                        var daysUntil = (recommendation.ProjectedReplacementDate.Value - DateTime.Now).Days;
                        if (daysUntil <= 7)
                        {
                            recommendation.Urgency = ReplacementUrgency.Soon;
                            recommendation.Message = $"Consider replacement within {daysUntil} days";
                        }
                        else if (daysUntil <= 30)
                        {
                            recommendation.Message = $"Replacement projected in approximately {daysUntil} days";
                        }
                        else
                        {
                            recommendation.Message = $"Strings should last approximately {daysUntil / 7} more weeks";
                        }
                    }
                }
                else
                {
                    recommendation.Message = "Strings have exceeded optimal decay threshold";
                    recommendation.Urgency = ReplacementUrgency.Recommended;
                }
            }
            else if (decayRatePerHour < -0.001)
            {
                // Negative decay rate (unusual - possible measurement error or new strings)
                recommendation.Factors.Add("Negative decay trend detected - verify measurements");
                recommendation.ConfidencePercentage = Math.Min(confidence, 30);
            }
            else
            {
                // Stable decay (near zero rate)
                recommendation.Message = GetMessageFromUrgency(recommendation.Urgency, currentDecay);
                recommendation.Factors.Add("Decay rate is stable");
            }

            // Adjust confidence based on data quality
            recommendation.ConfidencePercentage = CalculateConfidence(
                measurements.Count,
                confidence,
                totalPlayHours);

            // Add HF ratio analysis as additional factor
            var hfRatioDecay = CalculateHfRatioTrend(baseline, measurements);
            if (hfRatioDecay > 0.3)
            {
                recommendation.Factors.Add($"High-frequency energy declining ({hfRatioDecay:F1}x baseline)");
            }

            // Set message if not already set
            if (string.IsNullOrEmpty(recommendation.Message))
            {
                recommendation.Message = GetMessageFromUrgency(recommendation.Urgency, currentDecay);
            }

            return recommendation;
        }

        /// <inheritdoc />
        public double CalculateHealthScore(double decayPercentage)
        {
            // Health score is inverse of decay, clamped to 0-100
            var health = 100.0 - decayPercentage;
            return Math.Clamp(health, 0, 100);
        }

        /// <inheritdoc />
        public ReplacementUrgency GetUrgencyFromDecay(double decayPercentage)
        {
            if (decayPercentage < 15)
                return ReplacementUrgency.None;
            if (decayPercentage < _soonThreshold)
                return ReplacementUrgency.Monitor;
            if (decayPercentage < _recommendedThreshold)
                return ReplacementUrgency.Soon;
            if (decayPercentage < _urgentThreshold)
                return ReplacementUrgency.Recommended;
            return ReplacementUrgency.Urgent;
        }

        /// <summary>
        /// Calculates the decay rate using linear regression on play hours vs decay.
        /// </summary>
        /// <returns>Tuple of (decay rate per hour, R² confidence).</returns>
        private (double rate, double confidence) CalculateDecayRate(IReadOnlyList<MeasurementLog> measurements)
        {
            if (measurements.Count < 2)
            {
                return (0, 0);
            }

            // Build cumulative play hours and decay data points
            var dataPoints = new List<(double cumulativeHours, double decay)>();
            double cumulativeHours = 0;

            // Process measurements from oldest to newest
            var orderedMeasurements = measurements.OrderBy(m => m.MeasuredAt).ToList();
            foreach (var measurement in orderedMeasurements)
            {
                cumulativeHours += measurement.PlayTimeHours;
                dataPoints.Add((cumulativeHours, measurement.DecayPercentage));
            }

            if (cumulativeHours < 0.1) // Less than 6 minutes of tracked play
            {
                // Use time-based decay if no play hours recorded
                return CalculateTimeBasedDecayRate(orderedMeasurements);
            }

            // Linear regression: decay = slope * hours + intercept
            var (slope, _, rSquared) = LinearRegression(dataPoints);

            return (slope, rSquared * 100);
        }

        /// <summary>
        /// Fallback: Calculate decay rate based on time elapsed (when play hours not tracked).
        /// </summary>
        private (double rate, double confidence) CalculateTimeBasedDecayRate(List<MeasurementLog> orderedMeasurements)
        {
            var first = orderedMeasurements.First();
            var last = orderedMeasurements.Last();

            var timeDiffHours = (last.MeasuredAt - first.MeasuredAt).TotalHours;
            if (timeDiffHours < 1)
            {
                return (0, 20); // Low confidence for short time span
            }

            var decayDiff = last.DecayPercentage - first.DecayPercentage;
            var rate = decayDiff / timeDiffHours;

            // Lower confidence for time-based estimate
            return (rate, 40);
        }

        /// <summary>
        /// Performs simple linear regression on data points.
        /// </summary>
        /// <returns>Tuple of (slope, intercept, R²).</returns>
        private (double slope, double intercept, double rSquared) LinearRegression(
            List<(double x, double y)> points)
        {
            int n = points.Count;
            if (n < 2) return (0, 0, 0);

            double sumX = 0, sumY = 0, sumXY = 0, sumX2 = 0, sumY2 = 0;

            foreach (var (x, y) in points)
            {
                sumX += x;
                sumY += y;
                sumXY += x * y;
                sumX2 += x * x;
                sumY2 += y * y;
            }

            double denominator = n * sumX2 - sumX * sumX;
            if (Math.Abs(denominator) < 1e-10)
            {
                return (0, sumY / n, 0);
            }

            double slope = (n * sumXY - sumX * sumY) / denominator;
            double intercept = (sumY - slope * sumX) / n;

            // Calculate R² (coefficient of determination)
            double meanY = sumY / n;
            double ssTotal = 0, ssResidual = 0;

            foreach (var (x, y) in points)
            {
                double predicted = slope * x + intercept;
                ssResidual += (y - predicted) * (y - predicted);
                ssTotal += (y - meanY) * (y - meanY);
            }

            double rSquared = ssTotal > 0 ? 1 - (ssResidual / ssTotal) : 0;
            rSquared = Math.Max(0, rSquared); // Clamp to non-negative

            return (slope, intercept, rSquared);
        }

        /// <summary>
        /// Calculates average play hours per day from measurement history.
        /// </summary>
        private double CalculateAveragePlayHoursPerDay(IReadOnlyList<MeasurementLog> measurements)
        {
            if (measurements.Count < 2)
            {
                return 1.0; // Default assumption
            }

            var ordered = measurements.OrderBy(m => m.MeasuredAt).ToList();
            var totalDays = (ordered.Last().MeasuredAt - ordered.First().MeasuredAt).TotalDays;
            var totalHours = measurements.Sum(m => m.PlayTimeHours);

            if (totalDays < 1)
            {
                return totalHours; // Less than a day of data
            }

            return totalHours / totalDays;
        }

        /// <summary>
        /// Analyzes HF energy ratio trend compared to baseline.
        /// </summary>
        /// <returns>Ratio of current to baseline HF energy (&lt; 1 means degradation).</returns>
        private double CalculateHfRatioTrend(StringBaseline baseline, IReadOnlyList<MeasurementLog> measurements)
        {
            if (baseline.InitialHighRatio <= 0 || measurements.Count == 0)
            {
                return 1.0; // No degradation detected
            }

            var latestHfRatio = measurements
                .OrderByDescending(m => m.MeasuredAt)
                .First()
                .CurrentHighRatio;

            if (latestHfRatio <= 0)
            {
                return 1.0;
            }

            return baseline.InitialHighRatio / latestHfRatio;
        }

        /// <summary>
        /// Calculates overall confidence based on multiple factors.
        /// </summary>
        private double CalculateConfidence(int measurementCount, double regressionConfidence, double totalPlayHours)
        {
            // Base confidence from regression R²
            double confidence = regressionConfidence;

            // Boost for more measurements (diminishing returns)
            double measurementBonus = Math.Min(20, measurementCount * 3);
            confidence += measurementBonus;

            // Boost for more tracked play time
            if (totalPlayHours >= 10)
                confidence += 10;
            else if (totalPlayHours >= 5)
                confidence += 5;

            // Penalty for very few measurements
            if (measurementCount < MinMeasurementsForTrend)
                confidence *= 0.5;

            return Math.Clamp(confidence, 0, 95);
        }

        /// <summary>
        /// Gets a human-readable message for the given urgency level.
        /// </summary>
        private string GetMessageFromUrgency(ReplacementUrgency urgency, double currentDecay)
        {
            return urgency switch
            {
                ReplacementUrgency.None => "Strings are in excellent condition",
                ReplacementUrgency.Monitor => $"Strings are aging normally ({currentDecay:F0}% decay)",
                ReplacementUrgency.Soon => "Consider replacing strings soon for optimal tone",
                ReplacementUrgency.Recommended => "String replacement is recommended",
                ReplacementUrgency.Urgent => "Immediate string replacement advised",
                _ => $"Current decay: {currentDecay:F1}%"
            };
        }
    }
}
