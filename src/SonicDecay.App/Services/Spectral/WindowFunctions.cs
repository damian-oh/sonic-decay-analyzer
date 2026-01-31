namespace SonicDecay.App.Services.Spectral
{
    /// <summary>
    /// Supported window functions for FFT preprocessing.
    /// Windowing reduces spectral leakage by tapering the signal
    /// edges to zero, minimizing discontinuities at buffer boundaries.
    /// </summary>
    public enum WindowType
    {
        /// <summary>
        /// Hamming window - good general-purpose choice with low side lobes.
        /// w(n) = 0.54 - 0.46 * cos(2*pi*n / (N-1))
        /// </summary>
        Hamming,

        /// <summary>
        /// Hann (Hanning) window - smooth taper, zero at endpoints.
        /// w(n) = 0.5 * (1 - cos(2*pi*n / (N-1)))
        /// </summary>
        Hann,

        /// <summary>
        /// Blackman window - lower side lobes than Hamming, wider main lobe.
        /// w(n) = 0.42 - 0.5*cos(2*pi*n/(N-1)) + 0.08*cos(4*pi*n/(N-1))
        /// </summary>
        Blackman,

        /// <summary>
        /// Rectangular (no window) - maximum frequency resolution, high leakage.
        /// w(n) = 1
        /// </summary>
        Rectangular
    }

    /// <summary>
    /// Provides window function implementations for FFT preprocessing.
    /// Ported from SonicDecay.Engine/analysis.py apply_window() function.
    /// </summary>
    public static class WindowFunctions
    {
        /// <summary>
        /// Applies a window function to audio samples.
        /// </summary>
        /// <param name="samples">Input audio samples.</param>
        /// <param name="windowType">Type of window function to apply.</param>
        /// <returns>Windowed samples with same length as input.</returns>
        public static double[] Apply(double[] samples, WindowType windowType = WindowType.Hamming)
        {
            if (samples == null || samples.Length == 0)
            {
                return Array.Empty<double>();
            }

            int n = samples.Length;
            double[] window = GenerateWindow(n, windowType);
            double[] result = new double[n];

            for (int i = 0; i < n; i++)
            {
                result[i] = samples[i] * window[i];
            }

            return result;
        }

        /// <summary>
        /// Generates a window function of specified type and length.
        /// </summary>
        /// <param name="length">Length of the window in samples.</param>
        /// <param name="windowType">Type of window function.</param>
        /// <returns>Window coefficients array.</returns>
        public static double[] GenerateWindow(int length, WindowType windowType)
        {
            if (length <= 0)
            {
                return Array.Empty<double>();
            }

            double[] window = new double[length];

            switch (windowType)
            {
                case WindowType.Hamming:
                    GenerateHamming(window);
                    break;

                case WindowType.Hann:
                    GenerateHann(window);
                    break;

                case WindowType.Blackman:
                    GenerateBlackman(window);
                    break;

                case WindowType.Rectangular:
                default:
                    // Rectangular window: all ones
                    Array.Fill(window, 1.0);
                    break;
            }

            return window;
        }

        /// <summary>
        /// Generates a Hamming window.
        /// Formula: w(n) = 0.54 - 0.46 * cos(2*pi*n / (N-1))
        /// </summary>
        private static void GenerateHamming(double[] window)
        {
            int n = window.Length;

            // Guard against division by zero for single-sample windows
            if (n == 1)
            {
                window[0] = 1.0;
                return;
            }

            double denominator = n - 1;

            for (int i = 0; i < n; i++)
            {
                window[i] = 0.54 - 0.46 * Math.Cos(2.0 * Math.PI * i / denominator);
            }
        }

        /// <summary>
        /// Generates a Hann (Hanning) window.
        /// Formula: w(n) = 0.5 * (1 - cos(2*pi*n / (N-1)))
        /// </summary>
        private static void GenerateHann(double[] window)
        {
            int n = window.Length;

            // Guard against division by zero for single-sample windows
            if (n == 1)
            {
                window[0] = 1.0;
                return;
            }

            double denominator = n - 1;

            for (int i = 0; i < n; i++)
            {
                window[i] = 0.5 * (1.0 - Math.Cos(2.0 * Math.PI * i / denominator));
            }
        }

        /// <summary>
        /// Generates a Blackman window.
        /// Formula: w(n) = 0.42 - 0.5*cos(2*pi*n/(N-1)) + 0.08*cos(4*pi*n/(N-1))
        /// </summary>
        private static void GenerateBlackman(double[] window)
        {
            int n = window.Length;

            // Guard against division by zero for single-sample windows
            if (n == 1)
            {
                window[0] = 1.0;
                return;
            }

            double denominator = n - 1;

            for (int i = 0; i < n; i++)
            {
                double angle = 2.0 * Math.PI * i / denominator;
                window[i] = 0.42 - 0.5 * Math.Cos(angle) + 0.08 * Math.Cos(2.0 * angle);
            }
        }

        /// <summary>
        /// Parses a window type string to the corresponding enum value.
        /// </summary>
        /// <param name="windowName">Window type name (case-insensitive).</param>
        /// <returns>The corresponding WindowType enum value.</returns>
        public static WindowType ParseWindowType(string? windowName)
        {
            if (string.IsNullOrWhiteSpace(windowName))
            {
                return WindowType.Hamming;
            }

            return windowName.ToLowerInvariant() switch
            {
                "hamming" => WindowType.Hamming,
                "hann" or "hanning" => WindowType.Hann,
                "blackman" => WindowType.Blackman,
                "rectangular" or "rect" or "none" => WindowType.Rectangular,
                _ => WindowType.Hamming
            };
        }
    }
}
