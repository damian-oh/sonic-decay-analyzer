namespace SonicDecay.App.Services.Spectral
{
    /// <summary>
    /// Spectral metric calculations for acoustic degradation measurement.
    /// Ported from SonicDecay.Engine/spectral.py.
    /// </summary>
    public static class SpectralMetrics
    {
        /// <summary>
        /// Minimum magnitude sum threshold to avoid division by near-zero.
        /// </summary>
        private const double MinMagnitudeThreshold = 1e-10;

        /// <summary>
        /// Calculates the spectral centroid (center of mass) of a frequency spectrum.
        /// </summary>
        /// <param name="magnitudes">FFT magnitude values (non-negative).</param>
        /// <param name="frequencies">Corresponding frequency bins in Hz.</param>
        /// <param name="freqMin">Minimum frequency to consider (default 20 Hz).</param>
        /// <param name="freqMax">Maximum frequency to consider (default 20 kHz).</param>
        /// <returns>Spectral centroid frequency in Hz.</returns>
        /// <remarks>
        /// The spectral centroid indicates the "brightness" of a sound:
        /// Centroid = Sigma(f[i] * magnitude[i]) / Sigma(magnitude[i])
        /// As strings age, the centroid typically decreases (darker timbre).
        /// </remarks>
        public static double CalculateCentroid(
            double[] magnitudes,
            double[] frequencies,
            double freqMin = 20.0,
            double freqMax = 20000.0)
        {
            ValidateInputArrays(magnitudes, frequencies);

            // Apply frequency band filtering
            double weightedSum = 0.0;
            double magnitudeSum = 0.0;

            for (int i = 0; i < magnitudes.Length; i++)
            {
                double freq = frequencies[i];
                double mag = magnitudes[i];

                // Skip invalid values
                if (double.IsNaN(freq) || double.IsInfinity(freq) ||
                    double.IsNaN(mag) || double.IsInfinity(mag))
                {
                    continue;
                }

                if (freq >= freqMin && freq <= freqMax)
                {
                    weightedSum += freq * mag;
                    magnitudeSum += mag;
                }
            }

            if (magnitudeSum < MinMagnitudeThreshold)
            {
                return 0.0;
            }

            double result = weightedSum / magnitudeSum;

            // Guard against NaN/Infinity in result
            if (double.IsNaN(result) || double.IsInfinity(result))
            {
                return 0.0;
            }

            return result;
        }

        /// <summary>
        /// Calculates the High-Frequency Energy Ratio relative to fundamental.
        /// </summary>
        /// <param name="magnitudes">FFT magnitude values (non-negative).</param>
        /// <param name="frequencies">Corresponding frequency bins in Hz.</param>
        /// <param name="fundamentalFreq">The fundamental frequency (f0) in Hz.</param>
        /// <param name="hfLow">Lower bound of HF band (default 5 kHz).</param>
        /// <param name="hfHigh">Upper bound of HF band (default 15 kHz).</param>
        /// <param name="fundamentalBandwidth">Hz range around f0 to consider (default 50 Hz).</param>
        /// <returns>Ratio of HF energy to fundamental energy (dimensionless).</returns>
        /// <remarks>
        /// HF Ratio = Sigma(magnitude[5kHz:15kHz]) / magnitude[f0]
        /// Fresh strings exhibit higher HF ratios due to stronger harmonics.
        /// As strings degrade, upper harmonics decay faster than the fundamental.
        /// </remarks>
        public static double CalculateHfRatio(
            double[] magnitudes,
            double[] frequencies,
            double fundamentalFreq,
            double hfLow = 5000.0,
            double hfHigh = 15000.0,
            double fundamentalBandwidth = 50.0)
        {
            ValidateInputArrays(magnitudes, frequencies);

            if (fundamentalFreq <= 0)
            {
                return 0.0;
            }

            // Calculate fundamental energy (within bandwidth around f0)
            double f0Low = fundamentalFreq - fundamentalBandwidth / 2.0;
            double f0High = fundamentalFreq + fundamentalBandwidth / 2.0;
            double fundamentalEnergy = 0.0;

            for (int i = 0; i < magnitudes.Length; i++)
            {
                double freq = frequencies[i];

                if (freq >= f0Low && freq <= f0High)
                {
                    fundamentalEnergy += magnitudes[i];
                }
            }

            if (fundamentalEnergy < MinMagnitudeThreshold)
            {
                return 0.0;
            }

            // Calculate high-frequency band energy
            double hfEnergy = 0.0;

            for (int i = 0; i < magnitudes.Length; i++)
            {
                double freq = frequencies[i];

                if (freq >= hfLow && freq <= hfHigh)
                {
                    hfEnergy += magnitudes[i];
                }
            }

            return hfEnergy / fundamentalEnergy;
        }

        /// <summary>
        /// Calculates the decay percentage based on spectral centroid shift.
        /// </summary>
        /// <param name="initialCentroid">Baseline centroid from fresh strings (Hz).</param>
        /// <param name="currentCentroid">Current measured centroid (Hz).</param>
        /// <returns>
        /// Decay percentage (0-100+). Positive values indicate darkening.
        /// Can exceed 100% in extreme degradation cases.
        /// Returns 0.0 for invalid inputs (NaN, Infinity, or non-positive baseline).
        /// </returns>
        /// <remarks>
        /// Decay% = (Initial - Current) / Initial * 100
        /// </remarks>
        public static double CalculateDecay(double initialCentroid, double currentCentroid)
        {
            // Guard against invalid inputs
            if (initialCentroid <= 0 ||
                double.IsNaN(initialCentroid) ||
                double.IsInfinity(initialCentroid) ||
                double.IsNaN(currentCentroid) ||
                double.IsInfinity(currentCentroid))
            {
                return 0.0;
            }

            double result = ((initialCentroid - currentCentroid) / initialCentroid) * 100.0;

            // Guard against NaN/Infinity in result
            if (double.IsNaN(result) || double.IsInfinity(result))
            {
                return 0.0;
            }

            return result;
        }

        /// <summary>
        /// Detects the fundamental frequency from the magnitude spectrum.
        /// </summary>
        /// <param name="magnitudes">FFT magnitude values.</param>
        /// <param name="frequencies">Corresponding frequency bins in Hz.</param>
        /// <param name="expectedFreq">Expected fundamental frequency (optional).</param>
        /// <param name="searchTolerance">Fractional tolerance around expected (default 10%).</param>
        /// <param name="minFreq">Minimum search frequency if no expected (default 60 Hz).</param>
        /// <param name="maxFreq">Maximum search frequency if no expected (default 1500 Hz).</param>
        /// <returns>Tuple of (detected_frequency_hz, magnitude_at_peak).</returns>
        /// <remarks>
        /// Uses peak detection within an expected range. If expectedFreq is
        /// provided, searches within +/- tolerance of that frequency.
        /// </remarks>
        public static (double Frequency, double Magnitude) FindFundamental(
            double[] magnitudes,
            double[] frequencies,
            double? expectedFreq = null,
            double searchTolerance = 0.1,
            double minFreq = 60.0,
            double maxFreq = 1500.0)
        {
            if (magnitudes == null || frequencies == null ||
                magnitudes.Length == 0 || magnitudes.Length != frequencies.Length)
            {
                return (0.0, 0.0);
            }

            // Determine search range
            double low, high;

            if (expectedFreq.HasValue && expectedFreq.Value > 0)
            {
                low = expectedFreq.Value * (1.0 - searchTolerance);
                high = expectedFreq.Value * (1.0 + searchTolerance);
            }
            else
            {
                low = minFreq;
                high = maxFreq;
            }

            // Find peak within search range
            double peakFreq = 0.0;
            double peakMag = double.MinValue;

            for (int i = 0; i < magnitudes.Length; i++)
            {
                double freq = frequencies[i];

                if (freq >= low && freq <= high && magnitudes[i] > peakMag)
                {
                    peakMag = magnitudes[i];
                    peakFreq = freq;
                }
            }

            // Handle case where no peak was found in range
            if (peakMag == double.MinValue)
            {
                return (0.0, 0.0);
            }

            return (peakFreq, peakMag);
        }

        /// <summary>
        /// Validates that input arrays are non-null, non-empty, and have matching lengths.
        /// </summary>
        private static void ValidateInputArrays(double[] magnitudes, double[] frequencies)
        {
            if (magnitudes == null)
            {
                throw new ArgumentNullException(nameof(magnitudes), "Magnitude spectrum cannot be null.");
            }

            if (frequencies == null)
            {
                throw new ArgumentNullException(nameof(frequencies), "Frequencies cannot be null.");
            }

            if (magnitudes.Length != frequencies.Length)
            {
                throw new ArgumentException("Magnitude spectrum and frequencies must have same length.");
            }

            if (magnitudes.Length == 0)
            {
                throw new ArgumentException("Input arrays cannot be empty.");
            }
        }
    }
}
