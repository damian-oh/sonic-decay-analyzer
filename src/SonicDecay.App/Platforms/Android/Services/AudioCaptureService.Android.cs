using Android.Media;
using SonicDecay.App.Services.Interfaces;

namespace SonicDecay.App.Services.Implementations
{
    /// <summary>
    /// Android-specific audio capture implementation using AudioRecord API.
    /// </summary>
    public partial class AudioCaptureService
    {
        private AudioRecord? _audioRecord;
        private Thread? _captureThread;
        private volatile bool _isCapturing;

        // Android may not support 24-bit, so we track actual format
        private Encoding _actualEncoding = Encoding.Pcm16bit;
        private int _bytesPerSample = 2;

        /// <inheritdoc />
        public bool IsSupported()
        {
            // AudioRecord is available on all Android versions we target (API 21+)
            return true;
        }

        /// <inheritdoc />
        public async Task<bool> StartCaptureAsync()
        {
            if (State == AudioCaptureState.Capturing)
                return true;

            // Clean up any previous failed state
            if (_isCapturing)
            {
                _isCapturing = false;
            }

            State = AudioCaptureState.Initializing;

            return await Task.Run(() =>
            {
                try
                {
                    // Determine supported encoding (prefer 24-bit, fallback to 16-bit)
                    _actualEncoding = Encoding.Pcm16bit;
                    _bytesPerSample = 2;

                    // Try to use the requested sample rate, with fallbacks
                    int[] sampleRatesToTry = { AudioCaptureConstants.SampleRate, 44100, 22050, 16000 };
                    int bufferSize = 0;
                    int selectedSampleRate = AudioCaptureConstants.SampleRate;

                    foreach (var rate in sampleRatesToTry)
                    {
                        bufferSize = AudioRecord.GetMinBufferSize(
                            rate,
                            ChannelIn.Mono,
                            _actualEncoding);

                        if (bufferSize > 0)
                        {
                            selectedSampleRate = rate;
                            break;
                        }
                    }

                    if (bufferSize <= 0)
                    {
                        State = AudioCaptureState.Stopped;
                        OnError("Failed to determine audio buffer size. Audio hardware may not be available.");
                        return false;
                    }

                    ActualSampleRate = selectedSampleRate;

                    // Ensure buffer is at least our desired size
                    int desiredBufferBytes = AudioCaptureConstants.BufferSizeSamples * _bytesPerSample;
                    bufferSize = Math.Max(bufferSize, desiredBufferBytes);

                    _audioRecord = new AudioRecord(
                        AudioSource.Mic,
                        selectedSampleRate,
                        ChannelIn.Mono,
                        _actualEncoding,
                        bufferSize);

                    if (_audioRecord.State != Android.Media.State.Initialized)
                    {
                        State = AudioCaptureState.Stopped;
                        OnError("Failed to initialize AudioRecord. Check microphone permissions.");
                        _audioRecord?.Release();
                        _audioRecord = null;
                        return false;
                    }

                    _isCapturing = true;

                    try
                    {
                        _audioRecord.StartRecording();
                    }
                    catch (Exception ex)
                    {
                        _isCapturing = false;
                        _audioRecord?.Release();
                        _audioRecord = null;
                        State = AudioCaptureState.Stopped;
                        OnError($"Failed to start recording: {ex.Message}", ex);
                        return false;
                    }

                    _captureThread = new Thread(CaptureLoop)
                    {
                        IsBackground = true,
                        Name = "AudioCaptureThread"
                    };
                    _captureThread.Start();

                    State = AudioCaptureState.Capturing;
                    return true;
                }
                catch (Exception ex)
                {
                    // Ensure cleanup on any failure
                    _isCapturing = false;
                    _audioRecord?.Release();
                    _audioRecord = null;
                    State = AudioCaptureState.Stopped;
                    OnError($"Failed to start audio capture: {ex.Message}", ex);
                    return false;
                }
            });
        }

        /// <inheritdoc />
        public async Task StopCaptureAsync()
        {
            if (State == AudioCaptureState.Stopped)
                return;

            _isCapturing = false;

            await Task.Run(() =>
            {
                try
                {
                    _captureThread?.Join(TimeSpan.FromSeconds(2));
                    _captureThread = null;

                    if (_audioRecord != null)
                    {
                        if (_audioRecord.RecordingState == RecordState.Recording)
                        {
                            _audioRecord.Stop();
                        }
                        _audioRecord.Release();
                        _audioRecord = null;
                    }

                    State = AudioCaptureState.Stopped;
                }
                catch (Exception ex)
                {
                    OnError($"Error stopping audio capture: {ex.Message}", ex);
                }
            });
        }

        private void CaptureLoop()
        {
            int bufferSizeBytes = AudioCaptureConstants.BufferSizeSamples * _bytesPerSample;
            byte[] buffer = new byte[bufferSizeBytes];

            while (_isCapturing && _audioRecord != null)
            {
                try
                {
                    int bytesRead = _audioRecord.Read(buffer, 0, bufferSizeBytes);

                    if (bytesRead > 0)
                    {
                        float[] samples = _bytesPerSample == 3
                            ? ConvertBytesToFloatSamples24Bit(buffer)
                            : ConvertBytesToFloatSamples16Bit(buffer);

                        OnBufferCaptured(samples, ActualSampleRate, AudioCaptureConstants.Channels);
                    }
                    else if (bytesRead < 0)
                    {
                        OnError($"AudioRecord read error: {bytesRead}");
                        break;
                    }
                }
                catch (Exception ex)
                {
                    if (_isCapturing)
                    {
                        OnError($"Error in capture loop: {ex.Message}", ex);
                    }
                    break;
                }
            }
        }

        partial void DisposePlatformResources()
        {
            _isCapturing = false;
            _captureThread?.Join(TimeSpan.FromSeconds(1));
            _audioRecord?.Release();
            _audioRecord = null;
        }
    }
}
