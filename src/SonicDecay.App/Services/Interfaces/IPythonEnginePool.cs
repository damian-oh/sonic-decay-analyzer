namespace SonicDecay.App.Services.Interfaces
{
    /// <summary>
    /// Manages a pool of persistent Python processes for low-latency analysis.
    /// Eliminates process startup overhead by reusing warmed-up engines.
    /// </summary>
    public interface IPythonEnginePool : IDisposable
    {
        /// <summary>
        /// Initializes the process pool asynchronously.
        /// Creates and warms up the configured number of Python processes.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for initialization.</param>
        /// <returns>True if pool initialized successfully; otherwise, false.</returns>
        Task<bool> InitializeAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Analyzes an audio buffer using an available pooled process.
        /// </summary>
        /// <param name="samples">Normalized audio samples (-1 to 1).</param>
        /// <param name="sampleRate">Sample rate in Hz.</param>
        /// <param name="options">Optional analysis configuration.</param>
        /// <param name="cancellationToken">Cancellation token for timeout handling.</param>
        /// <returns>Analysis result containing spectral metrics.</returns>
        Task<SpectralAnalysisResult> AnalyzeAsync(
            float[] samples,
            int sampleRate,
            AnalysisOptions? options = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a value indicating whether the pool is initialized and ready.
        /// </summary>
        bool IsReady { get; }

        /// <summary>
        /// Gets the number of processes currently available in the pool.
        /// </summary>
        int AvailableProcessCount { get; }

        /// <summary>
        /// Gets the total number of processes in the pool.
        /// </summary>
        int TotalProcessCount { get; }

        /// <summary>
        /// Gets or sets the maximum number of processes in the pool.
        /// Default is 2 for mobile/desktop balance.
        /// </summary>
        int MaxPoolSize { get; set; }

        /// <summary>
        /// Gets or sets the timeout for acquiring a process from the pool.
        /// Default is 5000ms (5 seconds).
        /// </summary>
        int AcquireTimeoutMs { get; set; }

        /// <summary>
        /// Gets or sets the timeout for individual analysis operations.
        /// Default is 3000ms (3 seconds).
        /// </summary>
        int AnalysisTimeoutMs { get; set; }

        /// <summary>
        /// Gets diagnostic metrics for the pool.
        /// </summary>
        PoolDiagnostics GetDiagnostics();

        /// <summary>
        /// Restarts all processes in the pool.
        /// Use this if processes become unresponsive.
        /// </summary>
        Task RestartPoolAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Contains diagnostic information about the Python engine pool.
    /// </summary>
    public class PoolDiagnostics
    {
        /// <summary>
        /// Gets or sets the total number of analysis requests processed.
        /// </summary>
        public long TotalRequests { get; set; }

        /// <summary>
        /// Gets or sets the number of successful analyses.
        /// </summary>
        public long SuccessfulRequests { get; set; }

        /// <summary>
        /// Gets or sets the number of failed analyses.
        /// </summary>
        public long FailedRequests { get; set; }

        /// <summary>
        /// Gets or sets the number of requests that timed out.
        /// </summary>
        public long TimeoutRequests { get; set; }

        /// <summary>
        /// Gets or sets the average latency in milliseconds.
        /// </summary>
        public double AverageLatencyMs { get; set; }

        /// <summary>
        /// Gets or sets the minimum observed latency in milliseconds.
        /// </summary>
        public double MinLatencyMs { get; set; }

        /// <summary>
        /// Gets or sets the maximum observed latency in milliseconds.
        /// </summary>
        public double MaxLatencyMs { get; set; }

        /// <summary>
        /// Gets or sets the number of times processes were restarted.
        /// </summary>
        public int ProcessRestarts { get; set; }

        /// <summary>
        /// Gets or sets when the pool was last initialized.
        /// </summary>
        public DateTime? LastInitialized { get; set; }

        /// <summary>
        /// Gets the success rate as a percentage.
        /// </summary>
        public double SuccessRate => TotalRequests > 0
            ? (SuccessfulRequests * 100.0) / TotalRequests
            : 0;
    }
}
