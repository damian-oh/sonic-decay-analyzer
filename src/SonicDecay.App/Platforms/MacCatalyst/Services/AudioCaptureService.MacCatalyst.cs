using AVFoundation;
using AudioToolbox;
using SonicDecay.App.Services.Interfaces;

namespace SonicDecay.App.Services.Implementations
{
    /// <summary>
    /// MacCatalyst-specific audio capture implementation using AVAudioEngine.
    /// Shares implementation approach with iOS.
    /// </summary>
    public partial class AudioCaptureService
    {
        private AVAudioEngine? _audioEngine;
        private AVAudioInputNode? _inputNode;
        private volatile bool _isCapturing;

        /// <inheritdoc />
        public bool IsSupported()
        {
            return true;
        }

        /// <inheritdoc />
        public async Task<bool> StartCaptureAsync()
        {
            if (State == AudioCaptureState.Capturing)
                return true;

            State = AudioCaptureState.Initializing;

            return await Task.Run(() =>
            {
                try
                {
                    // Configure audio session for recording
                    var audioSession = AVAudioSession.SharedInstance();
                    NSError? error;

                    audioSession.SetCategory(AVAudioSessionCategory.PlayAndRecord,
                        AVAudioSessionCategoryOptions.DefaultToSpeaker |
                        AVAudioSessionCategoryOptions.AllowBluetooth,
                        out error);

                    if (error != null)
                    {
                        OnError($"Failed to set audio session category: {error.LocalizedDescription}");
                        return false;
                    }

                    audioSession.SetActive(true, out error);
                    if (error != null)
                    {
                        OnError($"Failed to activate audio session: {error.LocalizedDescription}");
                        return false;
                    }

                    _audioEngine = new AVAudioEngine();
                    _inputNode = _audioEngine.InputNode;

                    var nativeFormat = _inputNode.GetBusOutputFormat(0);
                    ActualSampleRate = (int)nativeFormat.SampleRate;

                    var processingFormat = new AVAudioFormat(nativeFormat.SampleRate, 1);

                    if (processingFormat == null)
                    {
                        OnError("Failed to create audio processing format.");
                        return false;
                    }

                    uint bufferSize = (uint)AudioCaptureConstants.BufferSizeSamples;

                    _inputNode.InstallTapOnBus(0, bufferSize, processingFormat,
                        (buffer, when) => HandleAudioBuffer(buffer));

                    _audioEngine.Prepare();

                    NSError? startError;
                    _audioEngine.StartAndReturnError(out startError);

                    if (startError != null)
                    {
                        OnError($"Failed to start audio engine: {startError.LocalizedDescription}");
                        return false;
                    }

                    _isCapturing = true;
                    State = AudioCaptureState.Capturing;
                    return true;
                }
                catch (Exception ex)
                {
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

            await Task.Run(() =>
            {
                try
                {
                    _isCapturing = false;

                    if (_inputNode != null)
                    {
                        _inputNode.RemoveTapOnBus(0);
                    }

                    _audioEngine?.Stop();

                    var audioSession = AVAudioSession.SharedInstance();
                    audioSession.SetActive(false, out _);

                    State = AudioCaptureState.Stopped;
                }
                catch (Exception ex)
                {
                    OnError($"Error stopping audio capture: {ex.Message}", ex);
                }
            });
        }

        private void HandleAudioBuffer(AVAudioPcmBuffer buffer)
        {
            if (!_isCapturing || buffer.FloatChannelData == null)
                return;

            try
            {
                int frameLength = (int)buffer.FrameLength;
                float[] samples = new float[frameLength];

                unsafe
                {
                    float* channelData = (float*)buffer.FloatChannelData.ToPointer();
                    float* firstChannel = ((float**)channelData)[0];

                    for (int i = 0; i < frameLength; i++)
                    {
                        samples[i] = firstChannel[i];
                    }
                }

                OnBufferCaptured(samples, ActualSampleRate, AudioCaptureConstants.Channels);
            }
            catch (Exception ex)
            {
                if (_isCapturing)
                {
                    OnError($"Error processing audio buffer: {ex.Message}", ex);
                }
            }
        }

        partial void DisposePlatformResources()
        {
            _isCapturing = false;

            if (_inputNode != null)
            {
                try { _inputNode.RemoveTapOnBus(0); } catch { }
            }

            _audioEngine?.Stop();
            _audioEngine?.Dispose();
            _audioEngine = null;
            _inputNode = null;
        }
    }
}
