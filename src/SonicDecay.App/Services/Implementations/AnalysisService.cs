using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using SonicDecay.App.Services.Interfaces;

// Process.Start/Kill are unsupported on iOS/macCatalyst. Python subprocess interop
// is intentionally Windows/macOS/Android-only. iOS users will use alternative analysis.
#pragma warning disable CA1416

namespace SonicDecay.App.Services.Implementations
{
    /// <summary>
    /// Spectral analysis service implementation using Python process interop.
    /// Communicates with SonicDecay.Engine via JSON-based CLI protocol.
    /// </summary>
    public class AnalysisService : IAnalysisService
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        private string _pythonPath = "python";
        private string _enginePath;
        private int _timeoutMs = 5000;

        /// <summary>
        /// Initializes a new instance of the AnalysisService class.
        /// </summary>
        public AnalysisService()
        {
            // Default engine path relative to app directory
            _enginePath = Path.Combine(
                AppContext.BaseDirectory,
                "..", "..", "..", "..", "..",
                "src", "SonicDecay.Engine"
            );

            // Normalize path
            if (Directory.Exists(_enginePath))
            {
                _enginePath = Path.GetFullPath(_enginePath);
            }
        }

        /// <inheritdoc />
        public string PythonPath
        {
            get => _pythonPath;
            set
            {
                var path = value ?? "python";

                // Security: Validate python path against injection patterns
                if (ContainsInjectionPatterns(path))
                {
                    Debug.WriteLine($"[AnalysisService] Rejected potentially unsafe Python path: {path}");
                    _pythonPath = "python"; // Fall back to default
                }
                else
                {
                    _pythonPath = path;
                }
            }
        }

        /// <inheritdoc />
        public string EnginePath
        {
            get => _enginePath;
            set => _enginePath = value ?? string.Empty;
        }

        /// <inheritdoc />
        public int TimeoutMs
        {
            get => _timeoutMs;
            set => _timeoutMs = Math.Max(100, value);
        }

        /// <inheritdoc />
        public async Task<SpectralAnalysisResult> AnalyzeAsync(
            float[] samples,
            int sampleRate,
            AnalysisOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            options ??= new AnalysisOptions();

            try
            {
                // Build request object
                var request = new AnalysisRequest
                {
                    Samples = samples.Select(s => (double)s).ToArray(),
                    SampleRate = sampleRate,
                    ExpectedFundamental = options.ExpectedFundamental,
                    InitialCentroid = options.InitialCentroid,
                    FftSize = options.FftSize,
                    WindowType = options.WindowType,
                    HfLow = options.HfLow,
                    HfHigh = options.HfHigh,
                };

                // Serialize to JSON
                var requestJson = JsonSerializer.Serialize(request, JsonOptions);

                // Execute Python process
                var responseJson = await ExecutePythonAsync(
                    "analyze",
                    requestJson,
                    cancellationToken
                );

                // Deserialize response
                var response = JsonSerializer.Deserialize<AnalysisResponse>(responseJson, JsonOptions);

                if (response == null)
                {
                    return CreateErrorResult(sampleRate, "Failed to parse Python response");
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
                return CreateErrorResult(sampleRate, "Analysis timed out");
            }
            catch (Exception ex)
            {
                return CreateErrorResult(sampleRate, $"Analysis failed: {ex.Message}");
            }
        }

        /// <inheritdoc />
        public Task<SpectralAnalysisResult> AnalyzeAsync(
            AudioBuffer buffer,
            AnalysisOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            return AnalyzeAsync(buffer.Samples, buffer.SampleRate, options, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<bool> IsEngineAvailableAsync()
        {
            try
            {
                var version = await GetEngineVersionAsync();
                return !string.IsNullOrEmpty(version);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AnalysisService] Engine availability check failed: {ex.Message}");
                return false;
            }
        }

        /// <inheritdoc />
        public async Task<string?> GetEngineVersionAsync()
        {
            try
            {
                var response = await ExecutePythonAsync("version", null, CancellationToken.None);
                var result = JsonSerializer.Deserialize<VersionResponse>(response, JsonOptions);
                return result?.Version;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AnalysisService] Failed to get engine version: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Executes the Python CLI with the specified command and input.
        /// </summary>
        private async Task<string> ExecutePythonAsync(
            string command,
            string? inputJson,
            CancellationToken cancellationToken)
        {
            var cliPath = Path.Combine(_enginePath, "cli.py");

            if (!File.Exists(cliPath))
            {
                throw new FileNotFoundException($"Python engine not found at: {cliPath}");
            }

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(_timeoutMs);

            var startInfo = new ProcessStartInfo
            {
                FileName = _pythonPath,
                Arguments = $"\"{cliPath}\" {command}",
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = _enginePath,
            };

            using var process = new Process { StartInfo = startInfo };

            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();

            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data != null) outputBuilder.AppendLine(e.Data);
            };

            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data != null) errorBuilder.AppendLine(e.Data);
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            // Write input if provided
            if (!string.IsNullOrEmpty(inputJson))
            {
                await process.StandardInput.WriteAsync(inputJson);
                await process.StandardInput.FlushAsync();
            }
            process.StandardInput.Close();

            // Wait for completion with timeout
            try
            {
                await process.WaitForExitAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                try
                {
                    process.Kill(entireProcessTree: true);
                }
                catch (Exception killEx)
                {
                    Debug.WriteLine($"[AnalysisService] Failed to kill process: {killEx.Message}");
                }
                throw;
            }

            var output = outputBuilder.ToString().Trim();
            var error = errorBuilder.ToString().Trim();

            if (process.ExitCode != 0 && string.IsNullOrEmpty(output))
            {
                throw new InvalidOperationException(
                    $"Python process failed (exit code {process.ExitCode}): {error}"
                );
            }

            return output;
        }

        /// <summary>
        /// Creates an error result with the specified message.
        /// </summary>
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

        /// <summary>
        /// Internal request model for JSON serialization.
        /// </summary>
        private class AnalysisRequest
        {
            public double[] Samples { get; set; } = Array.Empty<double>();
            public int SampleRate { get; set; }
            public double? ExpectedFundamental { get; set; }
            public double? InitialCentroid { get; set; }
            public int FftSize { get; set; }
            public string? WindowType { get; set; }
            public double HfLow { get; set; }
            public double HfHigh { get; set; }
        }

        /// <summary>
        /// Internal response model for JSON deserialization.
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
        /// Internal version response model.
        /// </summary>
        private class VersionResponse
        {
            public string? Version { get; set; }
            public bool Success { get; set; }
        }

        /// <summary>
        /// Checks if a path contains potential command injection patterns.
        /// </summary>
        /// <param name="path">The path to validate.</param>
        /// <returns>True if the path contains suspicious patterns.</returns>
        private static bool ContainsInjectionPatterns(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            // Check for common command injection characters and patterns
            var dangerousPatterns = new[]
            {
                ";",      // Command separator
                "|",      // Pipe
                "&",      // Background/and operator
                "`",      // Backtick execution
                "$(",     // Command substitution
                "${",     // Variable expansion
                "$((",    // Arithmetic expansion
                "\n",     // Newline
                "\r",     // Carriage return
                ">>",     // Append redirect
                "<<",     // Here document
            };

            foreach (var pattern in dangerousPatterns)
            {
                if (path.Contains(pattern, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
