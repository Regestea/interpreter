using Android.Media;
using System;
using System.IO;

namespace interpreter.Maui.Services;

/// <summary>
/// Handles audio recording operations using Android AudioRecord.
/// </summary>
public class AudioRecorder : IDisposable
{
    private readonly AudioRecordingConfiguration _config;
    private AudioRecord? _audioRecord;
    private bool _disposed;

    public AudioRecorder(AudioRecordingConfiguration config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
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
            outputStream.Dispose();
            throw;
        }
#pragma warning restore CA1416
    }

    /// <summary>
    /// Creates and configures an AudioRecord instance for continuous recording.
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

        return new AudioRecord(
            AudioSource.Mic,
            _config.SampleRate,
            channelConfig,
            audioFormat,
            minBufferSize
        );
#pragma warning restore CA1416
    }

    public void Dispose()
    {
        if (_disposed) return;

        _audioRecord?.Stop();
        _audioRecord?.Release();
        _audioRecord?.Dispose();
        _audioRecord = null;

        _disposed = true;
    }
}

