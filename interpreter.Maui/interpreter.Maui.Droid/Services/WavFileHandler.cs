using System.IO;

namespace interpreter.Maui.Services;

/// <summary>
/// Handles WAV file format operations.
/// </summary>
public class WavFileHandler
{
    /// <summary>
    /// Writes a WAV header to the stream with placeholder sizes.
    /// </summary>
    public static void WriteWavHeader(Stream stream, int sampleRate, int channels, int bitsPerSample)
    {
        // RIFF header with placeholder sizes
        using var bw = new BinaryWriter(stream, System.Text.Encoding.ASCII, leaveOpen: true);
        int byteRate = sampleRate * channels * bitsPerSample / 8;
        short blockAlign = (short)(channels * bitsPerSample / 8);

        bw.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
        bw.Write(0); // ChunkSize placeholder
        bw.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
        bw.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
        bw.Write(16); // Subchunk1Size for PCM
        bw.Write((short)1); // PCM format
        bw.Write((short)channels);
        bw.Write(sampleRate);
        bw.Write(byteRate);
        bw.Write(blockAlign);
        bw.Write((short)bitsPerSample);
        bw.Write(System.Text.Encoding.ASCII.GetBytes("data"));
        bw.Write(0); // Subchunk2Size placeholder
    }

    /// <summary>
    /// Updates the WAV header with actual file sizes.
    /// </summary>
    public static void UpdateWavHeader(Stream stream)
    {
        long fileSize = stream.Length;
        if (fileSize < 44) return;
        using var bw = new BinaryWriter(stream, System.Text.Encoding.ASCII, leaveOpen: true);
        long dataSize = fileSize - 44;
        // ChunkSize = 36 + Subchunk2Size
        bw.Seek(4, SeekOrigin.Begin);
        bw.Write((int)(36 + dataSize));
        // Subchunk2Size
        bw.Seek(40, SeekOrigin.Begin);
        bw.Write((int)dataSize);
        bw.Flush();
    }
}

