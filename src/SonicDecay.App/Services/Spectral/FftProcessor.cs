using FftSharp;

namespace SonicDecay.App.Services.Spectral
{
    /// <summary>
    /// FFT computation wrapper using FftSharp library.
    /// Ported from SonicDecay.Engine/analysis.py compute_fft() function.
    /// </summary>
    public static class FftProcessor
    {
        /// <summary>
        /// Default FFT size per CLAUDE.md specification (8192-point).
        /// </summary>
        public const int DefaultFftSize = 8192;

        /// <summary>
        /// Computes the FFT magnitude spectrum with windowing and zero-padding.
        /// </summary>
        /// <param name="samples">Input audio samples (normalized -1 to 1).</param>
        /// <param name="sampleRate">Sample rate in Hz.</param>
        /// <param name="fftSize">FFT size (default 8192 for frequency resolution).</param>
        /// <param name="windowType">Window function to apply before FFT.</param>
        /// <returns>Tuple of (magnitude_spectrum, frequencies) for positive frequencies.</returns>
        /// <remarks>
        /// Per CLAUDE.md specifications:
        /// - 8192-point FFT (zero-padded from 4096 samples)
        /// - Hamming window default to reduce spectral leakage
        /// </remarks>
        public static (double[] Magnitudes, double[] Frequencies) Compute(
            double[] samples,
            int sampleRate,
            int fftSize = DefaultFftSize,
            WindowType windowType = WindowType.Hamming)
        {
            if (samples == null || samples.Length == 0)
            {
                return (Array.Empty<double>(), Array.Empty<double>());
            }

            if (sampleRate <= 0)
            {
                throw new ArgumentException("Sample rate must be positive.", nameof(sampleRate));
            }

            // Ensure FFT size is a power of 2
            fftSize = EnsurePowerOfTwo(fftSize);

            // Apply window function
            double[] windowed = WindowFunctions.Apply(samples, windowType);

            // Zero-pad to FFT size if necessary
            double[] padded = ZeroPad(windowed, fftSize);

            // Compute FFT using FftSharp
            System.Numerics.Complex[] fftResult = FftSharp.FFT.Forward(padded);

            // Extract positive frequencies only (up to Nyquist)
            int nPositive = fftSize / 2 + 1;
            double[] magnitudes = new double[nPositive];

            for (int i = 0; i < nPositive; i++)
            {
                magnitudes[i] = fftResult[i].Magnitude;
            }

            // Generate frequency bins
            double[] frequencies = GenerateFrequencyBins(fftSize, sampleRate, nPositive);

            return (magnitudes, frequencies);
        }

        /// <summary>
        /// Zero-pads or truncates an array to the specified FFT size.
        /// </summary>
        /// <param name="samples">Input samples.</param>
        /// <param name="fftSize">Target FFT size.</param>
        /// <returns>Array of exactly fftSize length.</returns>
        private static double[] ZeroPad(double[] samples, int fftSize)
        {
            if (samples.Length == fftSize)
            {
                return samples;
            }

            double[] padded = new double[fftSize];

            if (samples.Length < fftSize)
            {
                // Zero-pad: copy samples and leave rest as zeros
                Array.Copy(samples, padded, samples.Length);
            }
            else
            {
                // Truncate: copy only fftSize samples
                Array.Copy(samples, padded, fftSize);
            }

            return padded;
        }

        /// <summary>
        /// Generates frequency bin values for positive frequencies.
        /// </summary>
        /// <param name="fftSize">FFT size.</param>
        /// <param name="sampleRate">Sample rate in Hz.</param>
        /// <param name="nPositive">Number of positive frequency bins.</param>
        /// <returns>Array of frequency values in Hz.</returns>
        private static double[] GenerateFrequencyBins(int fftSize, int sampleRate, int nPositive)
        {
            double[] frequencies = new double[nPositive];
            double binWidth = (double)sampleRate / fftSize;

            for (int i = 0; i < nPositive; i++)
            {
                frequencies[i] = i * binWidth;
            }

            return frequencies;
        }

        /// <summary>
        /// Ensures the FFT size is a power of two by rounding up if necessary.
        /// </summary>
        /// <param name="size">Requested size.</param>
        /// <returns>Nearest power of two >= size.</returns>
        private static int EnsurePowerOfTwo(int size)
        {
            if (size <= 0)
            {
                return 2;
            }

            // Check if already a power of 2
            if ((size & (size - 1)) == 0)
            {
                return size;
            }

            // Round up to next power of 2
            int power = 1;
            while (power < size)
            {
                power *= 2;
            }

            return power;
        }
    }
}
