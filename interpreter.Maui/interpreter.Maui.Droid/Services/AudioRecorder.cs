using Android.Media;
using System;
using System.Diagnostics;
using System.IO;
using Android.Media.Audiofx;
using Android.Util;

namespace interpreter.Maui.Services;

/// <summary>
/// Handles audio recording operations using Android AudioRecord.
/// </summary>
public class AudioRecorder : IAudioRecorder
{
    private readonly AudioRecordingConfiguration _config;
    private readonly AudioManager? _audioManager;
    private AudioRecord? _audioRecord;
    private NoiseSuppressor? _noiseSuppressor;
    private AcousticEchoCanceler? _echoCanceler;
    private bool _disposed;

    public AudioRecorder(AudioRecordingConfiguration config, AudioManager? audioManager = null)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _audioManager = audioManager;
    }

    /// <summary>
    /// Gets available audio input devices (requires AudioManager to be provided in constructor).
    /// Returns null if AudioManager is not available or API level is below 23.
    /// </summary>
    public AudioDeviceInfo[]? GetAvailableInputDevices()
    {
#pragma warning disable CA1416
        if (_audioManager == null)
            return null;

        if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.M)
        {
            return _audioManager.GetDevices(GetDevicesTargets.Inputs);
        }

        return null;
#pragma warning restore CA1416
    }

    /// <summary>
    /// Sets the preferred audio input device for recording.
    /// Requires Android API 23+ (Marshmallow).
    /// </summary>
    public bool SetPreferredDevice(AudioRecord audioRecord, AudioDeviceInfo device)
    {
#pragma warning disable CA1416
        if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.M)
        {
            return audioRecord.SetPreferredDevice(device);
        }
        return false;
#pragma warning restore CA1416
    }

    /// <summary>
    /// Records audio for a specified duration and returns a stream containing the WAV data.
    /// </summary>
    public System.IO.Stream RecordForDuration(TimeSpan duration)
    {
#pragma warning disable CA1416
        var channelConfig = ChannelIn.Mono;
        var audioFormat = Encoding.Pcm16bit;

        int minBufferSize = AudioRecord.GetMinBufferSize(_config.SampleRate, channelConfig, audioFormat);
        if (minBufferSize <= 0)
            minBufferSize = _config.SampleRate * _config.ChannelCount * (_config.BitsPerSample / 8); // 1 second fallback

        AudioRecord? audioRecord = null;
        NoiseSuppressor? noiseSuppressor = null;
        AcousticEchoCanceler? echoCanceler = null;
        MemoryStream outputStream = new MemoryStream();

        try
        {
            // Configure AudioRecord
            audioRecord = new AudioRecord(
                AudioSource.Mic,
                _config.SampleRate,
                channelConfig,
                audioFormat,
                minBufferSize
            );
            // Apply audio enhancements
            int audioSessionId = audioRecord.AudioSessionId;
            
            if (NoiseSuppressor.IsAvailable)
            {
                noiseSuppressor = NoiseSuppressor.Create(audioSessionId);
                noiseSuppressor?.SetEnabled(true);
            }

            // Write WAV header placeholder
            WavFileHandler.WriteWavHeader(outputStream, _config.SampleRate, _config.ChannelCount, _config.BitsPerSample);

            // Start recording
            audioRecord.StartRecording();

            var buffer = new byte[minBufferSize];
            var startTime = DateTime.Now;

            // Record for specified duration
            while ((DateTime.Now - startTime) < duration)
            {
                int read = audioRecord.Read(buffer, 0, buffer.Length);
                if (read > 0)
                {
                    outputStream.Write(buffer, 0, read);
                }
            }

            // Stop recording
            audioRecord.Stop();
            audioRecord.Release();
            audioRecord.Dispose();
            audioRecord = null;

            // Cleanup audio enhancements
            if (noiseSuppressor != null)
            {
                noiseSuppressor.SetEnabled(false);
                noiseSuppressor.Release();
                noiseSuppressor.Dispose();
                noiseSuppressor = null;
            }

            if (echoCanceler != null)
            {
                echoCanceler.SetEnabled(false);
                echoCanceler.Release();
                echoCanceler.Dispose();
                echoCanceler = null;
            }

            // Finalize WAV header
            WavFileHandler.UpdateWavHeader(outputStream);
            outputStream.Seek(0, SeekOrigin.Begin);

            return outputStream;
        }
        catch (Exception)
        {
            // Clean up on error
            audioRecord?.Stop();
            audioRecord?.Release();
            audioRecord?.Dispose();
            
            noiseSuppressor?.Release();
            noiseSuppressor?.Dispose();
            
            echoCanceler?.Release();
            echoCanceler?.Dispose();
            
            outputStream.Dispose();
            throw;
        }
#pragma warning restore CA1416
    }

    /// <summary>
    /// Creates and configures an AudioRecord instance for continuous recording with audio enhancements.
    /// </summary>
    public AudioRecord CreateAudioRecord(out int bufferSize)
    {
#pragma warning disable CA1416
        var channelConfig = ChannelIn.Mono;
        var audioFormat = Encoding.Pcm16bit;

        int minBufferSize = AudioRecord.GetMinBufferSize(_config.SampleRate, channelConfig, audioFormat);
        if (minBufferSize <= 0)
            minBufferSize = _config.SampleRate * _config.ChannelCount * (_config.BitsPerSample / 8); // 1 second fallback
        bufferSize = minBufferSize;

        var audioRecord = new AudioRecord(
            AudioSource.Mic,
            _config.SampleRate,
            channelConfig,
            audioFormat,
            minBufferSize
        );

        // Apply audio enhancements
        int audioSessionId = audioRecord.AudioSessionId;
        
        if (NoiseSuppressor.IsAvailable)
        {
            _noiseSuppressor = NoiseSuppressor.Create(audioSessionId);
            _noiseSuppressor?.SetEnabled(true);
        }

        return audioRecord;
#pragma warning restore CA1416
    }

    public void Dispose()
    {
        if (_disposed) return;

        _audioRecord?.Stop();
        _audioRecord?.Release();
        _audioRecord?.Dispose();
        _audioRecord = null;

        // Cleanup audio enhancements
        if (_noiseSuppressor != null)
        {
            _noiseSuppressor.SetEnabled(false);
            _noiseSuppressor.Release();
            _noiseSuppressor.Dispose();
            _noiseSuppressor = null;
        }

        if (_echoCanceler != null)
        {
            _echoCanceler.SetEnabled(false);
            _echoCanceler.Release();
            _echoCanceler.Dispose();
            _echoCanceler = null;
        }

        _disposed = true;
    }
}

