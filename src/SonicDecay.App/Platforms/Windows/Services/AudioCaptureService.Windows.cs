using System.Runtime.InteropServices;
using Windows.Media;
using Windows.Media.Audio;
using Windows.Media.Capture;
using Windows.Media.Devices;
using Windows.Media.MediaProperties;
using Windows.Media.Render;
using SonicDecay.App.Services.Interfaces;

namespace SonicDecay.App.Services.Implementations
{
    /// <summary>
    /// Windows-specific audio capture implementation using AudioGraph API.
    /// </summary>
    public partial class AudioCaptureService
    {
        private AudioGraph? _audioGraph;
        private AudioDeviceInputNode? _inputNode;
        private AudioFrameOutputNode? _outputNode;
        private volatile bool _isCapturing;

        /// <inheritdoc />
        public bool IsSupported()
        {
            // AudioGraph is available on Windows 10+
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

            try
            {
                // Create audio graph settings
                var settings = new AudioGraphSettings(AudioRenderCategory.Media)
                {
                    QuantumSizeSelectionMode = QuantumSizeSelectionMode.ClosestToDesired,
                    DesiredSamplesPerQuantum = AudioCaptureConstants.BufferSizeSamples,
                    DesiredRenderDeviceAudioProcessing = AudioProcessing.Raw
                };

                // Try to set our desired sample rate
                settings.EncodingProperties = AudioEncodingProperties.CreatePcm(
                    (uint)AudioCaptureConstants.SampleRate,
                    1, // Mono
                    16); // 16-bit (Windows AudioGraph doesn't easily support 24-bit input)

                var result = await AudioGraph.CreateAsync(settings);

                if (result.Status != AudioGraphCreationStatus.Success)
                {
                    State = AudioCaptureState.Stopped;
                    OnError($"Failed to create AudioGraph: {result.Status}");
                    return false;
                }

                _audioGraph = result.Graph;
                ActualSampleRate = (int)_audioGraph.EncodingProperties.SampleRate;

                // Create input node from default microphone
                var inputResult = await _audioGraph.CreateDeviceInputNodeAsync(
                    MediaCategory.Speech);

                if (inputResult.Status != AudioDeviceNodeCreationStatus.Success)
                {
                    _audioGraph.Dispose();
                    _audioGraph = null;
                    State = AudioCaptureState.Stopped;
                    OnError($"Failed to create audio input node: {inputResult.Status}");
                    return false;
                }

                _inputNode = inputResult.DeviceInputNode;

                // Create frame output node for capturing samples
                _outputNode = _audioGraph.CreateFrameOutputNode();
                _inputNode.AddOutgoingConnection(_outputNode);

                // Subscribe to quantum started event for processing
                _audioGraph.QuantumStarted += OnQuantumStarted;

                _isCapturing = true;
                _audioGraph.Start();

                State = AudioCaptureState.Capturing;
                return true;
            }
            catch (Exception ex)
            {
                // Ensure cleanup on any failure
                _isCapturing = false;
                if (_audioGraph != null)
                {
                    _audioGraph.QuantumStarted -= OnQuantumStarted;
                }
                _inputNode?.Dispose();
                _outputNode?.Dispose();
                _audioGraph?.Dispose();
                _inputNode = null;
                _outputNode = null;
                _audioGraph = null;
                State = AudioCaptureState.Stopped;
                OnError($"Failed to start audio capture: {ex.Message}", ex);
                return false;
            }
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

                    if (_audioGraph != null)
                    {
                        _audioGraph.QuantumStarted -= OnQuantumStarted;
                        _audioGraph.Stop();
                    }

                    _inputNode?.Dispose();
                    _outputNode?.Dispose();
                    _audioGraph?.Dispose();

                    _inputNode = null;
                    _outputNode = null;
                    _audioGraph = null;

                    State = AudioCaptureState.Stopped;
                }
                catch (Exception ex)
                {
                    OnError($"Error stopping audio capture: {ex.Message}", ex);
                }
            });
        }

        private void OnQuantumStarted(AudioGraph sender, object args)
        {
            if (!_isCapturing || _outputNode == null)
                return;

            try
            {
                var frame = _outputNode.GetFrame();

                using (var buffer = frame.LockBuffer(Windows.Media.AudioBufferAccessMode.Read))
                using (var reference = buffer.CreateReference())
                {
                    unsafe
                    {
                        byte* dataPtr;
                        uint capacity;

                        // Get the raw buffer pointer via COM interop
                        ((IMemoryBufferByteAccess)reference).GetBuffer(out dataPtr, out capacity);

                        if (capacity > 0)
                        {
                            // Convert bytes to managed array
                            byte[] bytes = new byte[capacity];
                            Marshal.Copy((IntPtr)dataPtr, bytes, 0, (int)capacity);

                            // Convert to float samples (16-bit PCM)
                            float[] samples = ConvertBytesToFloatSamples16Bit(bytes);

                            OnBufferCaptured(samples, ActualSampleRate, AudioCaptureConstants.Channels);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (_isCapturing)
                {
                    OnError($"Error processing audio quantum: {ex.Message}", ex);
                }
            }
        }

        partial void DisposePlatformResources()
        {
            _isCapturing = false;

            if (_audioGraph != null)
            {
                _audioGraph.QuantumStarted -= OnQuantumStarted;
                _audioGraph.Stop();
            }

            _inputNode?.Dispose();
            _outputNode?.Dispose();
            _audioGraph?.Dispose();

            _inputNode = null;
            _outputNode = null;
            _audioGraph = null;
        }
    }

    /// <summary>
    /// COM interface for accessing raw audio buffer memory.
    /// Required for reading audio frame data from AudioGraph.
    /// </summary>
    [ComImport]
    [Guid("5B0D3235-4DBA-4D44-865E-8F1D0E4FD04D")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    unsafe interface IMemoryBufferByteAccess
    {
        void GetBuffer(out byte* buffer, out uint capacity);
    }
}
