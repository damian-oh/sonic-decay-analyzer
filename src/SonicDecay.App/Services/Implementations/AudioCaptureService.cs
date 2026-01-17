using SonicDecay.App.Services.Interfaces;

namespace SonicDecay.App.Services.Implementations
{
    /// <summary>
    /// Cross-platform audio capture service implementation.
    /// Uses partial classes for platform-specific audio hardware access.
    /// </summary>
    public partial class AudioCaptureService : IAudioCaptureService
    {
        private AudioCaptureState _state = AudioCaptureState.Stopped;
        private float _rmsThreshold = AudioCaptureConstants.DefaultRmsThreshold;
        private bool _disposed;

        /// <inheritdoc />
        public event EventHandler<AudioBufferCapturedEventArgs>? BufferCaptured;

        /// <inheritdoc />
        public event EventHandler<AudioCaptureErrorEventArgs>? ErrorOccurred;

        /// <inheritdoc />
        public event EventHandler<AudioCaptureState>? StateChanged;

        /// <inheritdoc />
        public AudioCaptureState State
        {
            get => _state;
            protected set
            {
                if (_state != value)
                {
                    _state = value;
                    StateChanged?.Invoke(this, value);
                }
            }
        }

        /// <inheritdoc />
        public float RmsThreshold
        {
            get => _rmsThreshold;
            set => _rmsThreshold = Math.Clamp(value, 0f, 1f);
        }

        /// <inheritdoc />
        public int ActualSampleRate { get; protected set; } = AudioCaptureConstants.SampleRate;

        /// <summary>
        /// Calculates the Root Mean Square (RMS) amplitude of audio samples.
        /// RMS provides a measure of the average signal power.
        /// </summary>
        /// <param name="samples">Normalized audio samples (-1.0 to 1.0).</param>
        /// <returns>RMS amplitude value between 0.0 and 1.0.</returns>
        protected static float CalculateRms(float[] samples)
        {
            if (samples == null || samples.Length == 0)
                return 0f;

            double sumOfSquares = 0;
            for (int i = 0; i < samples.Length; i++)
            {
                sumOfSquares += samples[i] * samples[i];
            }

            return (float)Math.Sqrt(sumOfSquares / samples.Length);
        }

        /// <summary>
        /// Converts 24-bit PCM bytes to normalized float samples.
        /// 24-bit samples are stored as 3 bytes in little-endian order.
        /// </summary>
        /// <param name="bytes">Raw PCM byte data.</param>
        /// <returns>Normalized float samples (-1.0 to 1.0).</returns>
        protected static float[] ConvertBytesToFloatSamples24Bit(byte[] bytes)
        {
            const int bytesPerSample = 3;
            const float maxValue = 8388607f; // 2^23 - 1

            int sampleCount = bytes.Length / bytesPerSample;
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                int byteIndex = i * bytesPerSample;

                // Reconstruct 24-bit signed integer from 3 bytes (little-endian)
                int sample = bytes[byteIndex]
                           | (bytes[byteIndex + 1] << 8)
                           | (bytes[byteIndex + 2] << 16);

                // Sign extend if negative (bit 23 is set)
                if ((sample & 0x800000) != 0)
                {
                    sample |= unchecked((int)0xFF000000);
                }

                samples[i] = sample / maxValue;
            }

            return samples;
        }

        /// <summary>
        /// Converts 16-bit PCM bytes to normalized float samples.
        /// Used as fallback when 24-bit is not supported.
        /// </summary>
        /// <param name="bytes">Raw PCM byte data.</param>
        /// <returns>Normalized float samples (-1.0 to 1.0).</returns>
        protected static float[] ConvertBytesToFloatSamples16Bit(byte[] bytes)
        {
            const int bytesPerSample = 2;
            const float maxValue = 32767f; // 2^15 - 1

            int sampleCount = bytes.Length / bytesPerSample;
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                int byteIndex = i * bytesPerSample;

                // Reconstruct 16-bit signed integer from 2 bytes (little-endian)
                short sample = (short)(bytes[byteIndex] | (bytes[byteIndex + 1] << 8));
                samples[i] = sample / maxValue;
            }

            return samples;
        }

        /// <summary>
        /// Raises the BufferCaptured event with RMS threshold checking.
        /// </summary>
        /// <param name="samples">The captured audio samples.</param>
        /// <param name="sampleRate">The actual sample rate.</param>
        /// <param name="channels">The number of channels.</param>
        protected void OnBufferCaptured(float[] samples, int sampleRate, int channels)
        {
            float rms = CalculateRms(samples);
            var buffer = new AudioBuffer(samples, sampleRate, channels, rms);
            bool thresholdExceeded = rms >= _rmsThreshold;

            BufferCaptured?.Invoke(this, new AudioBufferCapturedEventArgs(buffer, thresholdExceeded));
        }

        /// <summary>
        /// Raises the ErrorOccurred event.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="exception">The optional exception.</param>
        protected void OnError(string message, Exception? exception = null)
        {
            State = AudioCaptureState.Error;
            ErrorOccurred?.Invoke(this, new AudioCaptureErrorEventArgs(message, exception));
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes managed and unmanaged resources.
        /// </summary>
        /// <param name="disposing">True if called from Dispose(); false if from finalizer.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    DisposePlatformResources();
                }
                _disposed = true;
            }
        }

        // Platform-specific partial methods
        partial void DisposePlatformResources();
    }
}
