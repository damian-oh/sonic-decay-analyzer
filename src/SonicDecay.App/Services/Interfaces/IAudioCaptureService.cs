namespace SonicDecay.App.Services.Interfaces
{
    /// <summary>
    /// Defines audio capture specifications matching project requirements.
    /// </summary>
    public static class AudioCaptureConstants
    {
        /// <summary>Sample rate in Hz (48kHz for headroom in harmonic analysis).</summary>
        public const int SampleRate = 48000;

        /// <summary>Bit depth for PCM audio capture.</summary>
        public const int BitDepth = 24;

        /// <summary>Number of audio channels (mono for string analysis).</summary>
        public const int Channels = 1;

        /// <summary>Buffer size in samples (4096 = ~85.3ms at 48kHz).</summary>
        public const int BufferSizeSamples = 4096;

        /// <summary>Buffer duration in milliseconds.</summary>
        public const double BufferDurationMs = (double)BufferSizeSamples / SampleRate * 1000;

        /// <summary>Default RMS threshold for trigger activation (normalized 0-1).</summary>
        public const float DefaultRmsThreshold = 0.02f;
    }

    /// <summary>
    /// Represents the current state of the audio capture service.
    /// </summary>
    public enum AudioCaptureState
    {
        /// <summary>Service is stopped and not capturing audio.</summary>
        Stopped,

        /// <summary>Service is initializing audio hardware.</summary>
        Initializing,

        /// <summary>Service is actively capturing audio data.</summary>
        Capturing,

        /// <summary>Service has encountered an error.</summary>
        Error
    }

    /// <summary>
    /// Contains PCM audio buffer data and associated metadata.
    /// </summary>
    public class AudioBuffer
    {
        /// <summary>
        /// Gets the raw PCM audio samples as normalized float values (-1.0 to 1.0).
        /// </summary>
        public float[] Samples { get; }

        /// <summary>
        /// Gets the sample rate of the captured audio in Hz.
        /// </summary>
        public int SampleRate { get; }

        /// <summary>
        /// Gets the number of channels in the buffer.
        /// </summary>
        public int Channels { get; }

        /// <summary>
        /// Gets the timestamp when this buffer was captured.
        /// </summary>
        public DateTime Timestamp { get; }

        /// <summary>
        /// Gets the calculated RMS (Root Mean Square) amplitude of the buffer.
        /// Value ranges from 0.0 (silence) to 1.0 (maximum amplitude).
        /// </summary>
        public float RmsAmplitude { get; }

        /// <summary>
        /// Initializes a new instance of the AudioBuffer class.
        /// </summary>
        /// <param name="samples">The normalized PCM samples.</param>
        /// <param name="sampleRate">The sample rate in Hz.</param>
        /// <param name="channels">The number of audio channels.</param>
        /// <param name="rmsAmplitude">The pre-calculated RMS amplitude.</param>
        public AudioBuffer(float[] samples, int sampleRate, int channels, float rmsAmplitude)
        {
            Samples = samples ?? throw new ArgumentNullException(nameof(samples));
            SampleRate = sampleRate;
            Channels = channels;
            RmsAmplitude = rmsAmplitude;
            Timestamp = DateTime.Now;
        }
    }

    /// <summary>
    /// Event arguments for audio buffer captured events.
    /// </summary>
    public class AudioBufferCapturedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the captured audio buffer.
        /// </summary>
        public AudioBuffer Buffer { get; }

        /// <summary>
        /// Gets a value indicating whether the buffer exceeded the RMS threshold.
        /// When true, the buffer contains audio suitable for spectral analysis.
        /// </summary>
        public bool ThresholdExceeded { get; }

        /// <summary>
        /// Initializes a new instance of the AudioBufferCapturedEventArgs class.
        /// </summary>
        /// <param name="buffer">The captured audio buffer.</param>
        /// <param name="thresholdExceeded">Whether the RMS threshold was exceeded.</param>
        public AudioBufferCapturedEventArgs(AudioBuffer buffer, bool thresholdExceeded)
        {
            Buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
            ThresholdExceeded = thresholdExceeded;
        }
    }

    /// <summary>
    /// Event arguments for audio capture error events.
    /// </summary>
    public class AudioCaptureErrorEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the error message describing the capture failure.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Gets the exception that caused the error, if any.
        /// </summary>
        public Exception? Exception { get; }

        /// <summary>
        /// Initializes a new instance of the AudioCaptureErrorEventArgs class.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="exception">The optional exception.</param>
        public AudioCaptureErrorEventArgs(string message, Exception? exception = null)
        {
            Message = message ?? throw new ArgumentNullException(nameof(message));
            Exception = exception;
        }
    }

    /// <summary>
    /// Provides cross-platform audio capture functionality for spectral analysis.
    /// Captures PCM audio at 48kHz/24-bit with RMS threshold triggering.
    /// </summary>
    public interface IAudioCaptureService : IDisposable
    {
        /// <summary>
        /// Occurs when an audio buffer has been captured.
        /// </summary>
        event EventHandler<AudioBufferCapturedEventArgs>? BufferCaptured;

        /// <summary>
        /// Occurs when an error is encountered during audio capture.
        /// </summary>
        event EventHandler<AudioCaptureErrorEventArgs>? ErrorOccurred;

        /// <summary>
        /// Occurs when the capture state changes.
        /// </summary>
        event EventHandler<AudioCaptureState>? StateChanged;

        /// <summary>
        /// Gets the current capture state.
        /// </summary>
        AudioCaptureState State { get; }

        /// <summary>
        /// Gets or sets the RMS threshold for trigger activation.
        /// Buffers exceeding this threshold will have ThresholdExceeded set to true.
        /// Value should be between 0.0 and 1.0.
        /// </summary>
        float RmsThreshold { get; set; }

        /// <summary>
        /// Gets the actual sample rate being used by the audio hardware.
        /// May differ from requested rate if hardware doesn't support it.
        /// </summary>
        int ActualSampleRate { get; }

        /// <summary>
        /// Starts audio capture asynchronously.
        /// Requires microphone permission to be granted before calling.
        /// </summary>
        /// <returns>True if capture started successfully; otherwise, false.</returns>
        Task<bool> StartCaptureAsync();

        /// <summary>
        /// Stops audio capture asynchronously.
        /// </summary>
        /// <returns>A task representing the async stop operation.</returns>
        Task StopCaptureAsync();

        /// <summary>
        /// Checks if audio capture is supported on the current platform.
        /// </summary>
        /// <returns>True if audio capture is supported; otherwise, false.</returns>
        bool IsSupported();
    }
}
