using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using SonicDecay.App.Services.Interfaces;

namespace SonicDecay.App.Services.Implementations
{
    /// <summary>
    /// Manages a pool of persistent Python processes for low-latency spectral analysis.
    /// Eliminates ~500-1000ms process startup overhead per analysis.
    /// </summary>
    public class PythonEnginePool : IPythonEnginePool
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        private readonly ConcurrentQueue<PooledProcess> _availableProcesses = new();
        private readonly ConcurrentDictionary<int, PooledProcess> _allProcesses = new();
        private readonly SemaphoreSlim _poolSemaphore;
        private readonly object _initLock = new();

        private string _pythonPath = "python";
        private string _enginePath;
        private int _maxPoolSize = 2;
        private int _acquireTimeoutMs = 5000;
        private int _analysisTimeoutMs = 3000;
        private bool _isInitialized;
        private bool _isDisposed;

        // Diagnostics
        private long _totalRequests;
        private long _successfulRequests;
        private long _failedRequests;
        private long _timeoutRequests;
        private double _totalLatencyMs;
        private double _minLatencyMs = double.MaxValue;
        private double _maxLatencyMs;
        private int _processRestarts;
        private DateTime? _lastInitialized;

        /// <summary>
        /// Initializes a new instance of the PythonEnginePool class.
        /// </summary>
        public PythonEnginePool()
        {
            _poolSemaphore = new SemaphoreSlim(_maxPoolSize, _maxPoolSize);

            // Default engine path relative to app directory
            _enginePath = Path.Combine(
                AppContext.BaseDirectory,
                "..", "..", "..", "..", "..",
                "src", "SonicDecay.Engine"
            );

            if (Directory.Exists(_enginePath))
            {
                _enginePath = Path.GetFullPath(_enginePath);
            }
        }

        /// <inheritdoc />
        public bool IsReady => _isInitialized && !_isDisposed && _availableProcesses.Count > 0;

        /// <inheritdoc />
        public int AvailableProcessCount => _availableProcesses.Count;

        /// <inheritdoc />
        public int TotalProcessCount => _allProcesses.Count;

        /// <inheritdoc />
        public int MaxPoolSize
        {
            get => _maxPoolSize;
            set => _maxPoolSize = Math.Clamp(value, 1, 10);
        }

        /// <inheritdoc />
        public int AcquireTimeoutMs
        {
            get => _acquireTimeoutMs;
            set => _acquireTimeoutMs = Math.Max(100, value);
        }

        /// <inheritdoc />
        public int AnalysisTimeoutMs
        {
            get => _analysisTimeoutMs;
            set => _analysisTimeoutMs = Math.Max(100, value);
        }

        /// <summary>
        /// Gets or sets the path to the Python executable.
        /// </summary>
        public string PythonPath
        {
            get => _pythonPath;
            set => _pythonPath = value ?? "python";
        }

        /// <summary>
        /// Gets or sets the path to the SonicDecay.Engine directory.
        /// </summary>
        public string EnginePath
        {
            get => _enginePath;
            set => _enginePath = value ?? string.Empty;
        }

        /// <inheritdoc />
        public async Task<bool> InitializeAsync(CancellationToken cancellationToken = default)
        {
            if (_isDisposed)
            {
                return false;
            }

            lock (_initLock)
            {
                if (_isInitialized)
                {
                    return true;
                }
            }

            try
            {
                // Create initial pool of processes
                var tasks = new List<Task<PooledProcess?>>();
                for (int i = 0; i < _maxPoolSize; i++)
                {
                    tasks.Add(CreateProcessAsync(cancellationToken));
                }

                var results = await Task.WhenAll(tasks);

                foreach (var process in results)
                {
                    if (process != null)
                    {
                        _allProcesses.TryAdd(process.ProcessId, process);
                        _availableProcesses.Enqueue(process);
                    }
                }

                lock (_initLock)
                {
                    _isInitialized = _availableProcesses.Count > 0;
                    _lastInitialized = DateTime.Now;
                }

                return _isInitialized;
            }
            catch
            {
                return false;
            }
        }

        /// <inheritdoc />
        public async Task<SpectralAnalysisResult> AnalyzeAsync(
            float[] samples,
            int sampleRate,
            AnalysisOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            if (_isDisposed)
            {
                return CreateErrorResult(sampleRate, "Engine pool has been disposed");
            }

            if (!_isInitialized)
            {
                // Try to initialize on first use
                if (!await InitializeAsync(cancellationToken))
                {
                    return CreateErrorResult(sampleRate, "Failed to initialize engine pool");
                }
            }

            Interlocked.Increment(ref _totalRequests);
            var stopwatch = Stopwatch.StartNew();

            PooledProcess? process = null;
            try
            {
                // Acquire a process from the pool
                if (!await _poolSemaphore.WaitAsync(_acquireTimeoutMs, cancellationToken))
                {
                    Interlocked.Increment(ref _timeoutRequests);
                    Interlocked.Increment(ref _failedRequests);
                    return CreateErrorResult(sampleRate, "Timeout waiting for available engine");
                }

                if (!_availableProcesses.TryDequeue(out process))
                {
                    // Pool is empty, create a new process
                    process = await CreateProcessAsync(cancellationToken);
                    if (process == null)
                    {
                        Interlocked.Increment(ref _failedRequests);
                        return CreateErrorResult(sampleRate, "Failed to create engine process");
                    }
                    _allProcesses.TryAdd(process.ProcessId, process);
                }

                // Execute analysis
                options ??= new AnalysisOptions();
                var result = await ExecuteAnalysisAsync(process, samples, sampleRate, options, cancellationToken);

                // Update latency stats
                stopwatch.Stop();
                var latencyMs = stopwatch.Elapsed.TotalMilliseconds;
                UpdateLatencyStats(latencyMs);

                if (result.Success)
                {
                    Interlocked.Increment(ref _successfulRequests);
                }
                else
                {
                    Interlocked.Increment(ref _failedRequests);
                }

                return result;
            }
            catch (OperationCanceledException)
            {
                Interlocked.Increment(ref _timeoutRequests);
                Interlocked.Increment(ref _failedRequests);
                return CreateErrorResult(sampleRate, "Analysis timed out");
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref _failedRequests);
                return CreateErrorResult(sampleRate, $"Analysis failed: {ex.Message}");
            }
            finally
            {
                // Return process to pool if still healthy
                if (process != null)
                {
                    if (process.IsHealthy)
                    {
                        _availableProcesses.Enqueue(process);
                    }
                    else
                    {
                        // Process died, remove and try to create replacement
                        _allProcesses.TryRemove(process.ProcessId, out _);
                        process.Dispose();
                        Interlocked.Increment(ref _processRestarts);

                        _ = Task.Run(async () =>
                        {
                            var replacement = await CreateProcessAsync(CancellationToken.None);
                            if (replacement != null)
                            {
                                _allProcesses.TryAdd(replacement.ProcessId, replacement);
                                _availableProcesses.Enqueue(replacement);
                            }
                        });
                    }
                    _poolSemaphore.Release();
                }
            }
        }

        /// <inheritdoc />
        public PoolDiagnostics GetDiagnostics()
        {
            var total = Interlocked.Read(ref _totalRequests);
            return new PoolDiagnostics
            {
                TotalRequests = total,
                SuccessfulRequests = Interlocked.Read(ref _successfulRequests),
                FailedRequests = Interlocked.Read(ref _failedRequests),
                TimeoutRequests = Interlocked.Read(ref _timeoutRequests),
                AverageLatencyMs = total > 0 ? _totalLatencyMs / total : 0,
                MinLatencyMs = _minLatencyMs == double.MaxValue ? 0 : _minLatencyMs,
                MaxLatencyMs = _maxLatencyMs,
                ProcessRestarts = _processRestarts,
                LastInitialized = _lastInitialized
            };
        }

        /// <inheritdoc />
        public async Task RestartPoolAsync(CancellationToken cancellationToken = default)
        {
            // Stop all existing processes
            foreach (var kvp in _allProcesses)
            {
                kvp.Value.Dispose();
            }
            _allProcesses.Clear();

            // Clear the queue
            while (_availableProcesses.TryDequeue(out _)) { }

            // Reset semaphore
            while (_poolSemaphore.CurrentCount < _maxPoolSize)
            {
                _poolSemaphore.Release();
            }

            _isInitialized = false;
            Interlocked.Increment(ref _processRestarts);

            // Reinitialize
            await InitializeAsync(cancellationToken);
        }

        /// <summary>
        /// Creates a new pooled Python process.
        /// </summary>
        private async Task<PooledProcess?> CreateProcessAsync(CancellationToken cancellationToken)
        {
            var serverPath = Path.Combine(_enginePath, "server.py");

            // If server.py doesn't exist, fall back to regular CLI mode
            // (This means we can't use pooling, but it will still work)
            if (!File.Exists(serverPath))
            {
                return null;
            }

            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = _pythonPath,
                    Arguments = $"\"{serverPath}\"",
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WorkingDirectory = _enginePath,
                };

                var process = new Process { StartInfo = startInfo };
                process.Start();

                // Wait for ready signal
                var readyTask = process.StandardOutput.ReadLineAsync();
                var completed = await Task.WhenAny(
                    readyTask,
                    Task.Delay(5000, cancellationToken)
                );

                if (completed != readyTask)
                {
                    process.Kill();
                    process.Dispose();
                    return null;
                }

                var readyLine = await readyTask;
                if (readyLine?.Contains("READY") != true)
                {
                    process.Kill();
                    process.Dispose();
                    return null;
                }

                return new PooledProcess(process);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Executes analysis on a pooled process.
        /// </summary>
        private async Task<SpectralAnalysisResult> ExecuteAnalysisAsync(
            PooledProcess pooledProcess,
            float[] samples,
            int sampleRate,
            AnalysisOptions options,
            CancellationToken cancellationToken)
        {
            var process = pooledProcess.Process;

            if (process.HasExited)
            {
                pooledProcess.MarkUnhealthy();
                return CreateErrorResult(sampleRate, "Engine process terminated unexpectedly");
            }

            // Build request
            var request = new
            {
                samples = samples.Select(s => (double)s).ToArray(),
                sample_rate = sampleRate,
                expected_fundamental = options.ExpectedFundamental,
                initial_centroid = options.InitialCentroid,
                fft_size = options.FftSize,
                window_type = options.WindowType,
                hf_low = options.HfLow,
                hf_high = options.HfHigh,
            };

            var requestJson = JsonSerializer.Serialize(request, JsonOptions);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(_analysisTimeoutMs);

            try
            {
                // Send request
                await process.StandardInput.WriteLineAsync(requestJson);
                await process.StandardInput.FlushAsync();

                // Read response
                var responseLine = await process.StandardOutput.ReadLineAsync(cts.Token);

                if (string.IsNullOrEmpty(responseLine))
                {
                    pooledProcess.MarkUnhealthy();
                    return CreateErrorResult(sampleRate, "No response from engine");
                }

                var response = JsonSerializer.Deserialize<AnalysisResponse>(responseLine, JsonOptions);
                if (response == null)
                {
                    return CreateErrorResult(sampleRate, "Failed to parse engine response");
                }

                return new SpectralAnalysisResult
                {
                    SpectralCentroid = response.SpectralCentroid,
                    HfEnergyRatio = response.HfEnergyRatio,
                    FundamentalFreq = response.FundamentalFreq,
                    FundamentalMagnitude = response.FundamentalMagnitude,
                    DecayPercentage = response.DecayPercentage,
                    SampleRate = response.SampleRate,
                    FftSize = response.FftSize,
                    Success = response.Success,
                    ErrorMessage = response.ErrorMessage,
                };
            }
            catch (OperationCanceledException)
            {
                pooledProcess.MarkUnhealthy();
                throw;
            }
            catch (Exception ex)
            {
                pooledProcess.MarkUnhealthy();
                return CreateErrorResult(sampleRate, $"Communication error: {ex.Message}");
            }
        }

        private void UpdateLatencyStats(double latencyMs)
        {
            _totalLatencyMs += latencyMs;
            if (latencyMs < _minLatencyMs) _minLatencyMs = latencyMs;
            if (latencyMs > _maxLatencyMs) _maxLatencyMs = latencyMs;
        }

        private static SpectralAnalysisResult CreateErrorResult(int sampleRate, string message)
        {
            return new SpectralAnalysisResult
            {
                SpectralCentroid = 0,
                HfEnergyRatio = 0,
                FundamentalFreq = 0,
                FundamentalMagnitude = 0,
                DecayPercentage = null,
                SampleRate = sampleRate,
                FftSize = 0,
                Success = false,
                ErrorMessage = message,
            };
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;

            foreach (var kvp in _allProcesses)
            {
                kvp.Value.Dispose();
            }
            _allProcesses.Clear();

            while (_availableProcesses.TryDequeue(out var process))
            {
                process.Dispose();
            }

            _poolSemaphore.Dispose();
        }

        /// <summary>
        /// Response model for JSON deserialization.
        /// </summary>
        private class AnalysisResponse
        {
            public double SpectralCentroid { get; set; }
            public double HfEnergyRatio { get; set; }
            public double FundamentalFreq { get; set; }
            public double FundamentalMagnitude { get; set; }
            public double? DecayPercentage { get; set; }
            public int SampleRate { get; set; }
            public int FftSize { get; set; }
            public bool Success { get; set; }
            public string? ErrorMessage { get; set; }
        }

        /// <summary>
        /// Wraps a Process with health tracking.
        /// </summary>
        private class PooledProcess : IDisposable
        {
            public Process Process { get; }
            public int ProcessId { get; }
            public bool IsHealthy { get; private set; } = true;
            public DateTime CreatedAt { get; } = DateTime.Now;

            public PooledProcess(Process process)
            {
                Process = process;
                ProcessId = process.Id;
            }

            public void MarkUnhealthy()
            {
                IsHealthy = false;
            }

            public void Dispose()
            {
                try
                {
                    if (!Process.HasExited)
                    {
                        Process.StandardInput.WriteLine("EXIT");
                        if (!Process.WaitForExit(1000))
                        {
                            Process.Kill();
                        }
                    }
                    Process.Dispose();
                }
                catch { }
            }
        }
    }
}
