using System;
using System.IO;

namespace interpreter.Maui.Services;

/// <summary>
/// Manages buffering of audio chunks and creates WAV streams when silence is detected.
/// </summary>
public class AudioChunkBuffer
{
    private readonly MemoryStream _currentChunk;
    private readonly int _sampleRate;
    private readonly int _channelCount;
    private readonly int _bitsPerSample;
    private bool _hasAudioData;

    public AudioChunkBuffer(int sampleRate, int channelCount, int bitsPerSample)
    {
        _sampleRate = sampleRate;
        _channelCount = channelCount;
        _bitsPerSample = bitsPerSample;
        _currentChunk = new MemoryStream();
        _hasAudioData = false;
        
        // Write WAV header placeholder
        WavFileHandler.WriteWavHeader(_currentChunk, _sampleRate, _channelCount, _bitsPerSample);
    }

    /// <summary>
    /// Appends audio data to the current chunk.
    /// </summary>
    public void AppendData(byte[] buffer, int offset, int length)
    {
        if (buffer == null || length <= 0)
            return;

        _currentChunk.Write(buffer, offset, length);
        _hasAudioData = true;
    }

    /// <summary>
    /// Gets whether the buffer has any audio data.
    /// </summary>
    public bool HasData => _hasAudioData;

    /// <summary>
    /// Gets the current chunk size in bytes (excluding WAV header).
    /// </summary>
    public long GetDataSize()
    {
        // Subtract WAV header size (44 bytes)
        return Math.Max(0, _currentChunk.Length - 44);
    }

    /// <summary>
    /// Finalizes the current chunk and returns it as a MemoryStream.
    /// Creates a new buffer for the next chunk.
    /// </summary>
    public MemoryStream? FinalizeChunk()
    {
        if (!_hasAudioData || _currentChunk.Length <= 44)
        {
            return null;
        }

        // Finalize WAV header
        WavFileHandler.UpdateWavHeader(_currentChunk);
        
        // Create a copy of the current chunk
        var finalizedChunk = new MemoryStream();
        _currentChunk.Position = 0;
        _currentChunk.CopyTo(finalizedChunk);
        finalizedChunk.Position = 0;

        // Reset the buffer for the next chunk
        _currentChunk.SetLength(0);
        _currentChunk.Position = 0;
        _hasAudioData = false;
        
        // Write new WAV header for the next chunk
        WavFileHandler.WriteWavHeader(_currentChunk, _sampleRate, _channelCount, _bitsPerSample);

        return finalizedChunk;
    }

    /// <summary>
    /// Resets the buffer without finalizing.
    /// </summary>
    public void Reset()
    {
        _currentChunk.SetLength(0);
        _currentChunk.Position = 0;
        _hasAudioData = false;
        WavFileHandler.WriteWavHeader(_currentChunk, _sampleRate, _channelCount, _bitsPerSample);
    }

    /// <summary>
    /// Disposes the buffer.
    /// </summary>
    public void Dispose()
    {
        _currentChunk?.Dispose();
    }
}

