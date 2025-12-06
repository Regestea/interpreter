using System;
using System.Collections.Generic;
using System.IO;

namespace interpreter.Maui.Services;

/// <summary>
/// Extracts speech segments from a complete audio recording based on time ranges.
/// </summary>
public static class SpeechSegmentExtractor
{
    /// <summary>
    /// Extracts speech segments from raw PCM audio data based on time ranges.
    /// </summary>
    /// <param name="audioData">Complete PCM16 audio data (little-endian)</param>
    /// <param name="sampleRate">Sample rate in Hz</param>
    /// <param name="channelCount">Number of audio channels</param>
    /// <param name="segments">List of speech segments with start/end times</param>
    /// <returns>Concatenated audio data containing only speech segments</returns>
    public static byte[] ExtractSegments(
        byte[] audioData, 
        int sampleRate, 
        int channelCount,
        IReadOnlyList<SpeechTimeSegment> segments)
    {
        if (audioData == null || audioData.Length == 0)
            return Array.Empty<byte>();
        
        if (segments == null || segments.Count == 0)
            return audioData; // Return original if no segments detected
        
        int bytesPerSample = 2 * channelCount; // 16-bit PCM
        int bytesPerSecond = sampleRate * bytesPerSample;
        
        using var outputStream = new MemoryStream();
        
        foreach (var segment in segments)
        {
            long startByte = (long)(segment.Start.TotalSeconds * bytesPerSecond);
            long endByte = (long)(segment.End.TotalSeconds * bytesPerSecond);
            
            // Align to sample boundaries
            startByte = (startByte / bytesPerSample) * bytesPerSample;
            endByte = (endByte / bytesPerSample) * bytesPerSample;
            
            // Clamp to valid range
            startByte = Math.Max(0, startByte);
            endByte = Math.Min(audioData.Length, endByte);
            
            if (endByte > startByte)
            {
                int length = (int)(endByte - startByte);
                outputStream.Write(audioData, (int)startByte, length);
            }
        }
        
        return outputStream.ToArray();
    }

    /// <summary>
    /// Extracts speech segments and creates a WAV file.
    /// </summary>
    public static MemoryStream ExtractSegmentsAsWav(
        byte[] audioData,
        int sampleRate,
        int channelCount,
        int bitsPerSample,
        IReadOnlyList<SpeechTimeSegment> segments)
    {
        byte[] extractedData = ExtractSegments(audioData, sampleRate, channelCount, segments);
        
        var outputStream = new MemoryStream();
        WavFileHandler.WriteWavHeader(outputStream, sampleRate, channelCount, bitsPerSample);
        outputStream.Write(extractedData, 0, extractedData.Length);
        WavFileHandler.UpdateWavHeader(outputStream);
        outputStream.Position = 0;
        
        return outputStream;
    }

    /// <summary>
    /// Extracts each speech segment as a separate WAV stream.
    /// </summary>
    public static List<(SpeechTimeSegment Segment, MemoryStream Audio)> ExtractIndividualSegments(
        byte[] audioData,
        int sampleRate,
        int channelCount,
        int bitsPerSample,
        IReadOnlyList<SpeechTimeSegment> segments)
    {
        var results = new List<(SpeechTimeSegment, MemoryStream)>();
        
        if (audioData == null || audioData.Length == 0 || segments == null)
            return results;
        
        int bytesPerSample = 2 * channelCount;
        int bytesPerSecond = sampleRate * bytesPerSample;
        
        foreach (var segment in segments)
        {
            long startByte = (long)(segment.Start.TotalSeconds * bytesPerSecond);
            long endByte = (long)(segment.End.TotalSeconds * bytesPerSecond);
            
            // Align to sample boundaries
            startByte = (startByte / bytesPerSample) * bytesPerSample;
            endByte = (endByte / bytesPerSample) * bytesPerSample;
            
            // Clamp to valid range
            startByte = Math.Max(0, startByte);
            endByte = Math.Min(audioData.Length, endByte);
            
            if (endByte > startByte)
            {
                int length = (int)(endByte - startByte);
                
                var wavStream = new MemoryStream();
                WavFileHandler.WriteWavHeader(wavStream, sampleRate, channelCount, bitsPerSample);
                wavStream.Write(audioData, (int)startByte, length);
                WavFileHandler.UpdateWavHeader(wavStream);
                wavStream.Position = 0;
                
                results.Add((segment, wavStream));
            }
        }
        
        return results;
    }

    /// <summary>
    /// Merges nearby segments if the gap between them is smaller than the threshold.
    /// This helps avoid choppy audio from closely-spaced speech segments.
    /// </summary>
    public static List<SpeechTimeSegment> MergeNearbySegments(
        IReadOnlyList<SpeechTimeSegment> segments,
        TimeSpan maxGap)
    {
        if (segments == null || segments.Count == 0)
            return new List<SpeechTimeSegment>();
        
        var merged = new List<SpeechTimeSegment>();
        var current = segments[0];
        
        for (int i = 1; i < segments.Count; i++)
        {
            var next = segments[i];
            var gap = next.Start - current.End;
            
            if (gap <= maxGap)
            {
                // Merge: extend current segment to include next
                current = new SpeechTimeSegment
                {
                    Start = current.Start,
                    End = next.End
                };
            }
            else
            {
                // Gap too large, save current and start new
                merged.Add(current);
                current = next;
            }
        }
        
        // Add the last segment
        merged.Add(current);
        
        return merged;
    }

    /// <summary>
    /// Calculates the total duration of all speech segments.
    /// </summary>
    public static TimeSpan GetTotalSpeechDuration(IReadOnlyList<SpeechTimeSegment> segments)
    {
        if (segments == null || segments.Count == 0)
            return TimeSpan.Zero;
        
        TimeSpan total = TimeSpan.Zero;
        foreach (var segment in segments)
        {
            total += segment.Duration;
        }
        
        return total;
    }
}

