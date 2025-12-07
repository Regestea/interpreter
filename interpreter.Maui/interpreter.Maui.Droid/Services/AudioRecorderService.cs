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
public class AudioRecorderService : IAudioRecorderService
{
    private readonly AudioRecordingConfiguration _config;
    private readonly AudioManager? _audioManager;
    private AudioRecord? _audioRecord;
    private NoiseSuppressor? _noiseSuppressor;
    private AcousticEchoCanceler? _echoCanceler;
    private AutomaticGainControl? _automaticGainControl;
    private bool _disposed;

    public AudioRecorderService(AudioRecordingConfiguration config, AudioManager? audioManager = null)
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
        
        // Use larger buffer for better distant audio capture
        int bufferSize = minBufferSize * 2;

        AudioRecord? audioRecord = null;
        NoiseSuppressor? noiseSuppressor = null;
        AutomaticGainControl? automaticGainControl = null;
        MemoryStream outputStream = new MemoryStream();

        try
        {
            // Use voice recognition source for better distant pickup if configured
            var audioSource = _config.UseVoiceRecognitionSource 
                ? AudioSource.VoiceRecognition 
                : AudioSource.Mic;
            
            // Configure AudioRecord
            audioRecord = new AudioRecord(
                audioSource,
                _config.SampleRate,
                channelConfig,
                audioFormat,
                bufferSize
            );
            
            // Set preferred microphone from MicrophoneSettings
            ApplySelectedMicrophone(audioRecord);
            
            // Apply audio enhancements
            int audioSessionId = audioRecord.AudioSessionId;
            
            if (NoiseSuppressor.IsAvailable)
            {
                noiseSuppressor = NoiseSuppressor.Create(audioSessionId);
                noiseSuppressor?.SetEnabled(true);
            }
            
            // Enable Automatic Gain Control for distant audio
            if (_config.EnableAutomaticGainControl && AutomaticGainControl.IsAvailable)
            {
                automaticGainControl = AutomaticGainControl.Create(audioSessionId);
                automaticGainControl?.SetEnabled(true);
            }

            // Write WAV header placeholder
            WavFileHandler.WriteWavHeader(outputStream, _config.SampleRate, _config.ChannelCount, _config.BitsPerSample);

            // Start recording
            audioRecord.StartRecording();

            var buffer = new byte[bufferSize];
            var startTime = DateTime.Now;

            // Record for specified duration
            while ((DateTime.Now - startTime) < duration)
            {
                int read = audioRecord.Read(buffer, 0, buffer.Length);
                if (read > 0)
                {
                    // Apply software gain amplification for distant audio
                    if (Math.Abs(_config.GainMultiplier - 1.0f) > 0.001f)
                    {
                        ApplyGain(buffer, read, _config.GainMultiplier);
                    }
                    
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


            
            if (automaticGainControl != null)
            {
                automaticGainControl.SetEnabled(false);
                automaticGainControl.Release();
                automaticGainControl.Dispose();
                automaticGainControl = null;
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
            
            
            automaticGainControl?.Release();
            automaticGainControl?.Dispose();
            
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
        
        // Use larger buffer for better distant audio capture
        bufferSize = minBufferSize * 2;

        // Use voice recognition source for better distant pickup if configured
        var audioSource = _config.UseVoiceRecognitionSource 
            ? AudioSource.VoiceRecognition 
            : AudioSource.Mic;

        var audioRecord = new AudioRecord(
            audioSource,
            _config.SampleRate,
            channelConfig,
            audioFormat,
            bufferSize
        );

        // Set preferred microphone from MicrophoneSettings
        ApplySelectedMicrophone(audioRecord);

        // Apply audio enhancements
        int audioSessionId = audioRecord.AudioSessionId;
        
        if (NoiseSuppressor.IsAvailable)
        {
            _noiseSuppressor = NoiseSuppressor.Create(audioSessionId);
            _noiseSuppressor?.SetEnabled(true);
        }
        
        // Enable Automatic Gain Control for distant audio
        if (_config.EnableAutomaticGainControl && AutomaticGainControl.IsAvailable)
        {
            _automaticGainControl = AutomaticGainControl.Create(audioSessionId);
            _automaticGainControl?.SetEnabled(true);
        }

        return audioRecord;
#pragma warning restore CA1416
    }

    /// <summary>
    /// Applies gain amplification to PCM16 audio data.
    /// </summary>
    private void ApplyGain(byte[] buffer, int length, float gain)
    {
        for (int i = 0; i < length; i += 2)
        {
            // Read 16-bit PCM sample (little-endian)
            short sample = (short)(buffer[i] | (buffer[i + 1] << 8));
            
            // Apply gain and clamp to prevent clipping
            int amplified = (int)(sample * gain);
            if (amplified > short.MaxValue)
                amplified = short.MaxValue;
            else if (amplified < short.MinValue)
                amplified = short.MinValue;
            
            // Write back amplified sample
            buffer[i] = (byte)(amplified & 0xFF);
            buffer[i + 1] = (byte)((amplified >> 8) & 0xFF);
        }
    }

    /// <summary>
    /// Applies the selected microphone from MicrophoneSettings to the AudioRecord instance.
    /// </summary>
    private void ApplySelectedMicrophone(AudioRecord audioRecord)
    {
#pragma warning disable CA1416
        var selectedMic = MicrophoneSettings.Instance.SelectedMicrophone;
        if (selectedMic?.AndroidDeviceInfo != null)
        {
            SetPreferredDevice(audioRecord, selectedMic.AndroidDeviceInfo);
        }
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
        
        if (_automaticGainControl != null)
        {
            _automaticGainControl.SetEnabled(false);
            _automaticGainControl.Release();
            _automaticGainControl.Dispose();
            _automaticGainControl = null;
        }

        _disposed = true;
    }
}

