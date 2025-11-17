
using Concentus.Enums;
using Concentus.Structs;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Opus.Services;

public class OpusCodecService : IOpusCodecService
{
    // Target format constants
    private const int SampleRate = 16000;
    private const int Channels = 1;
    private const int BitsPerSample = 16;
    private const OpusApplication Application = OpusApplication.OPUS_APPLICATION_VOIP;
    private const int Bitrate = 24000; // 24 kbps
    private const int FrameSizeMs = 20; // 20ms frames
    private const int FrameSize = SampleRate / 1000 * FrameSizeMs; // 320 samples for 16kHz
    
    // Buffer size constants - optimized for minimal allocation
    private const int PcmBufferSize = FrameSize * sizeof(short); // 640 bytes
    private const int OpusBufferSize = 1500; // Safe MTU size, standard in VoIP
    private const int LengthPrefixSize = sizeof(int); // 4 bytes

    public async Task<Stream> EncodeAsync(Stream wavStream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(wavStream);
        
        if (!wavStream.CanRead)
        {
            throw new ArgumentException("Input stream must be readable", nameof(wavStream));
        }

        return await Task.Run(() => EncodeInternal(wavStream, cancellationToken), cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<Stream> DecodeAsync(Stream opusStream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(opusStream);
        
        if (!opusStream.CanRead)
        {
            throw new ArgumentException("Input stream must be readable", nameof(opusStream));
        }

        return await Task.Run(() => DecodeInternal(opusStream, cancellationToken), cancellationToken)
            .ConfigureAwait(false);
    }

    private Stream EncodeInternal(Stream wavStream, CancellationToken cancellationToken)
    {
        WaveStream? sourceStream = null;
        WaveStream? resampledStream = null;
        
        try
        {
            // Read the input WAV file
            sourceStream = new WaveFileReader(wavStream);
            var format = sourceStream.WaveFormat;

            // Validate that we can process this format
            if (format.Encoding != WaveFormatEncoding.Pcm && format.Encoding != WaveFormatEncoding.IeeeFloat)
            {
                throw new NotSupportedException($"Unsupported WAV encoding: {format.Encoding}. Only PCM and IEEE Float are supported.");
            }

            // Determine if we need to resample/convert
            var needsConversion = format.SampleRate != SampleRate || 
                                  format.Channels != Channels || 
                                  format.BitsPerSample != BitsPerSample;

            WaveStream processStream;
            
            if (needsConversion)
            {
                // Convert to target format: 16 kHz, mono, 16-bit PCM
                processStream = ConvertToTargetFormat(sourceStream, out resampledStream);
            }
            else
            {
                processStream = sourceStream;
            }

            // Initialize Opus encoder
            var encoder = new OpusEncoder(SampleRate, Channels, Application)
            {
                Bitrate = Bitrate
            };

            var outputStream = new MemoryStream();
            
            // Pre-allocate buffers outside the loop to reduce allocations
            var pcmBuffer = new byte[PcmBufferSize];
            var pcmShorts = new short[FrameSize];
            var opusBuffer = new byte[OpusBufferSize];
            var lengthBytes = new byte[LengthPrefixSize];
            
            int bytesRead;
            while ((bytesRead = processStream.Read(pcmBuffer, 0, PcmBufferSize)) > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Pad with silence if last frame is incomplete
                if (bytesRead < PcmBufferSize)
                {
                    Array.Clear(pcmBuffer, bytesRead, PcmBufferSize - bytesRead);
                }

                // Convert bytes to shorts (16-bit PCM)
                Buffer.BlockCopy(pcmBuffer, 0, pcmShorts, 0, PcmBufferSize);

                // Encode to Opus
                int encodedLength = encoder.Encode(pcmShorts, 0, FrameSize, opusBuffer, 0, OpusBufferSize);
                
                if (encodedLength <= 0)
                {
                    throw new InvalidOperationException($"Opus encoding failed with result: {encodedLength}");
                }

                // Write length prefix (4 bytes) then encoded data
                BitConverter.TryWriteBytes(lengthBytes, encodedLength);
                outputStream.Write(lengthBytes, 0, LengthPrefixSize);
                outputStream.Write(opusBuffer, 0, encodedLength);
            }

            outputStream.Position = 0;
            return outputStream;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new InvalidOperationException("Failed to encode WAV to Opus", ex);
        }
        finally
        {
            // Dispose resources in reverse order
            resampledStream?.Dispose();
            sourceStream?.Dispose();
        }
    }

    private Stream DecodeInternal(Stream opusStream, CancellationToken cancellationToken)
    {
        try
        {
            // Initialize Opus decoder
            var decoder = new OpusDecoder(SampleRate, Channels);
            
            var targetFormat = new WaveFormat(SampleRate, BitsPerSample, Channels);
            
            // We need to write to a temporary stream first, then copy to a new stream
            // because WaveFileWriter closes the underlying stream when disposed
            byte[] wavData;
            using (var tempStream = new MemoryStream())
            {
                using (var writer = new WaveFileWriter(tempStream, targetFormat))
                {
                    // Pre-allocate buffers outside the loop
                    var lengthBuffer = new byte[LengthPrefixSize];
                    var opusBuffer = new byte[OpusBufferSize];
                    var pcmBuffer = new short[FrameSize * 2]; // Extra space for decoder safety
                    var byteBuffer = new byte[FrameSize * sizeof(short)];
                    
                    while (true)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        // Read length prefix
                        int lengthBytesRead = ReadExactly(opusStream, lengthBuffer, 0, LengthPrefixSize);
                        if (lengthBytesRead == 0)
                        {
                            break; // End of stream
                        }
                        
                        if (lengthBytesRead < LengthPrefixSize)
                        {
                            throw new InvalidDataException("Incomplete length prefix in Opus stream");
                        }

                        int frameLength = BitConverter.ToInt32(lengthBuffer, 0);
                        
                        // Validate frame length
                        if (frameLength <= 0 || frameLength > OpusBufferSize)
                        {
                            throw new InvalidDataException($"Invalid Opus frame length: {frameLength}. Expected 1-{OpusBufferSize} bytes.");
                        }
                        
                        // Read Opus frame data
                        int frameBytesRead = ReadExactly(opusStream, opusBuffer, 0, frameLength);
                        if (frameBytesRead < frameLength)
                        {
                            throw new InvalidDataException($"Incomplete Opus frame. Expected {frameLength} bytes, got {frameBytesRead} bytes.");
                        }
                        
                        // Decode Opus frame to PCM
                        int decodedSamples = decoder.Decode(opusBuffer, 0, frameLength, pcmBuffer, 0, FrameSize, false);
                        
                        if (decodedSamples < 0)
                        {
                            throw new InvalidOperationException($"Opus decoding failed with error code: {decodedSamples}");
                        }

                        // Handle variable sample counts (jitter protection)
                        if (decodedSamples > FrameSize)
                        {
                            decodedSamples = FrameSize; // Clip to expected size
                        }
                        else if (decodedSamples < FrameSize)
                        {
                            // Pad with silence to maintain consistent frame size
                            Array.Clear(pcmBuffer, decodedSamples, FrameSize - decodedSamples);
                            decodedSamples = FrameSize;
                        }

                        // Convert shorts to bytes and write
                        int byteCount = decodedSamples * sizeof(short);
                        Buffer.BlockCopy(pcmBuffer, 0, byteBuffer, 0, byteCount);
                        writer.Write(byteBuffer, 0, byteCount);
                    }
                } // Writer is disposed here, finalizing the WAV header
                
                // Get the buffer before tempStream is disposed
                wavData = tempStream.ToArray();
            }
            
            // Create a new MemoryStream with the WAV data
            var outputStream = new MemoryStream(wavData);
            outputStream.Position = 0;
            
            return outputStream;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new InvalidOperationException("Failed to decode Opus to WAV", ex);
        }
    }

    /// <summary>
    /// Converts input audio to target format: 16 kHz, mono, 16-bit PCM
    /// </summary>
    private WaveStream ConvertToTargetFormat(WaveStream sourceStream, out WaveStream? intermediateStream)
    {
        ISampleProvider sampleProvider = sourceStream.ToSampleProvider();

        // Convert to mono if needed
        if (sourceStream.WaveFormat.Channels != Channels)
        {
            sampleProvider = sampleProvider.ToMono();
        }

        // Resample if needed
        if (sourceStream.WaveFormat.SampleRate != SampleRate)
        {
            sampleProvider = new WdlResamplingSampleProvider(sampleProvider, SampleRate);
        }

        // Convert to 16-bit PCM WaveProvider
        var wave16Provider = new SampleToWaveProvider16(sampleProvider);
        
        // Wrap in a WaveProviderToWaveStream
        intermediateStream = new WaveProviderToWaveStream(wave16Provider);
        
        return intermediateStream;
    }

    /// <summary>
    /// Reads exactly the requested number of bytes from the stream
    /// </summary>
    private static int ReadExactly(Stream stream, byte[] buffer, int offset, int count)
    {
        int totalRead = 0;
        
        while (totalRead < count)
        {
            int bytesRead = stream.Read(buffer, offset + totalRead, count - totalRead);
            
            if (bytesRead == 0)
            {
                break; // End of stream
            }
            
            totalRead += bytesRead;
        }
        
        return totalRead;
    }
}

/// <summary>
/// Helper class to wrap an IWaveProvider as a WaveStream
/// </summary>
internal class WaveProviderToWaveStream : WaveStream
{
    private readonly IWaveProvider _provider;
    private long _position;

    public WaveProviderToWaveStream(IWaveProvider provider)
    {
        _provider = provider;
    }

    public override WaveFormat WaveFormat => _provider.WaveFormat;

    public override long Length => long.MaxValue; // Unknown length for live providers

    public override long Position
    {
        get => _position;
        set => throw new NotSupportedException("Cannot seek in a WaveProvider stream");
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        int bytesRead = _provider.Read(buffer, offset, count);
        _position += bytesRead;
        return bytesRead;
    }
}
