using Opus.Services;
using NAudio.Wave;
using Xunit.Abstractions;

namespace Opus.Tests.Services;

public class OpusCodecServiceTests
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly IOpusCodecService _opusCodecService;
    private readonly string _testAudioPath;

    public OpusCodecServiceTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _opusCodecService = new OpusCodecService();
        _testAudioPath = Path.Combine("AudioSample", "sample-audio.wav");
    }

    [Fact]
    public async Task EncodeAsync_WithValidWavFile_ShouldProduceOpusStream()
    {
        // Arrange
        await using var wavStream = File.OpenRead(_testAudioPath);
        _testOutputHelper.WriteLine($"Before Encode - WAV file size: {wavStream.Length} bytes ({wavStream.Length / 1024.0:F2} KB)");

        // Act
        var opusStream = await _opusCodecService.EncodeAsync(wavStream);
        _testOutputHelper.WriteLine($"After Encode - Opus file size: {opusStream.Length} bytes ({opusStream.Length / 1024.0:F2} KB)");

        // Assert
        Assert.NotNull(opusStream);
        Assert.True(opusStream.Length > 0, "Encoded stream should have data");
        Assert.True(opusStream.CanRead, "Encoded stream should be readable");
        Assert.Equal(0L, opusStream.Position);
        
        await opusStream.DisposeAsync();
    }

    [Fact]
    public async Task DecodeAsync_WithValidOpusStream_ShouldProduceWavStream()
    {
        // Arrange
        await using var wavStream = File.OpenRead(_testAudioPath);
        _testOutputHelper.WriteLine($"Before Encode - WAV file size: {wavStream.Length} bytes ({wavStream.Length / 1024.0:F2} KB)");
        var opusStream = await _opusCodecService.EncodeAsync(wavStream);
        _testOutputHelper.WriteLine($"After Encode - Opus file size: {opusStream.Length} bytes ({opusStream.Length / 1024.0:F2} KB)");

        // Act
        _testOutputHelper.WriteLine($"Before Decode - Opus file size: {opusStream.Length} bytes ({opusStream.Length / 1024.0:F2} KB)");
        var decodedStream = await _opusCodecService.DecodeAsync(opusStream);
        _testOutputHelper.WriteLine($"After Decode - WAV file size: {decodedStream.Length} bytes ({decodedStream.Length / 1024.0:F2} KB)");

        // Assert
        Assert.NotNull(decodedStream);
        Assert.True(decodedStream.Length > 0, "Decoded stream should have data");
        Assert.True(decodedStream.CanRead, "Decoded stream should be readable");
        Assert.Equal(0L, decodedStream.Position);
        
        // Verify it's a valid WAV file
        decodedStream.Position = 0;
        using var waveReader = new WaveFileReader(decodedStream);
        Assert.Equal(16000, waveReader.WaveFormat.SampleRate);
        Assert.Equal(1, waveReader.WaveFormat.Channels);
        Assert.Equal(16, waveReader.WaveFormat.BitsPerSample);
        
        await opusStream.DisposeAsync();
    }

    [Fact]
    public async Task EncodeAndDecode_RoundTrip_ShouldProduceValidAudio()
    {
        // Arrange
        await using var originalWavStream = File.OpenRead(_testAudioPath);
        _testOutputHelper.WriteLine($"Before Encode - Original WAV file size: {originalWavStream.Length} bytes ({originalWavStream.Length / 1024.0:F2} KB)");
        
        // Act - Encode
        var opusStream = await _opusCodecService.EncodeAsync(originalWavStream);
        _testOutputHelper.WriteLine($"After Encode - Opus file size: {opusStream.Length} bytes ({opusStream.Length / 1024.0:F2} KB)");
        
        // Act - Decode
        opusStream.Position = 0;
        _testOutputHelper.WriteLine($"Before Decode - Opus file size: {opusStream.Length} bytes ({opusStream.Length / 1024.0:F2} KB)");
        var decodedStream = await _opusCodecService.DecodeAsync(opusStream);
        _testOutputHelper.WriteLine($"After Decode - Decoded WAV file size: {decodedStream.Length} bytes ({decodedStream.Length / 1024.0:F2} KB)");

        // Assert
        Assert.NotNull(decodedStream);
        Assert.True(decodedStream.Length > 0, "Round-trip should produce audio data");
        
        // Verify the decoded WAV is valid
        decodedStream.Position = 0;
        using var waveReader = new WaveFileReader(decodedStream);
        
        // Check format
        Assert.Equal(16000, waveReader.WaveFormat.SampleRate);
        Assert.Equal(1, waveReader.WaveFormat.Channels);
        Assert.Equal(16, waveReader.WaveFormat.BitsPerSample);
        
        // Verify we can read samples
        var buffer = new byte[1024];
        var bytesRead = waveReader.Read(buffer, 0, buffer.Length);
        Assert.True(bytesRead > 0, "Should be able to read audio samples");
        
        await opusStream.DisposeAsync();
    }

    [Fact]
    public async Task EncodeAsync_WithNullStream_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _opusCodecService.EncodeAsync(null!));
    }

    [Fact]
    public async Task DecodeAsync_WithNullStream_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _opusCodecService.DecodeAsync(null!));
    }

    [Fact]
    public async Task EncodeAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        await using var wavStream = File.OpenRead(_testAudioPath);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        // TaskCanceledException is a subclass of OperationCanceledException
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => 
            _opusCodecService.EncodeAsync(wavStream, cts.Token));
    }

    [Fact]
    public async Task EncodeAsync_ShouldCompressAudio()
    {
        // Arrange
        await using var wavStream = File.OpenRead(_testAudioPath);
        var originalSize = wavStream.Length;
        _testOutputHelper.WriteLine($"Before Encode - Original WAV size: {originalSize} bytes ({originalSize / 1024.0:F2} KB)");

        // Act
        var opusStream = await _opusCodecService.EncodeAsync(wavStream);
        _testOutputHelper.WriteLine($"After Encode - Opus size: {opusStream.Length} bytes ({opusStream.Length / 1024.0:F2} KB)");
        
        var compressionRatio = (double)originalSize / opusStream.Length;
        _testOutputHelper.WriteLine($"Compression ratio: {compressionRatio:F2}x");

        // Assert
        Assert.True(opusStream.Length < originalSize, 
            $"Opus encoding should compress audio. Original: {originalSize}, Encoded: {opusStream.Length}");
        
        await opusStream.DisposeAsync();
    }
}