using System.Diagnostics;
using Xunit.Abstractions;

namespace SpeechBrain.Tests;

[Collection("PythonEngine")]
public class SpeechBrainRecognitionTests : IDisposable
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly SpeechBrainRecognition _recognition;
    private readonly string _audioSamplesPath;

    public SpeechBrainRecognitionTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _recognition = new SpeechBrainRecognition();
        _audioSamplesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AudioSamples");
    }

    [Fact]
    public void Constructor_ShouldCreateInstance()
    {
        // Arrange & Act
        using var recognition = new SpeechBrainRecognition();

        // Assert
        Assert.NotNull(recognition);
    }

    [Fact]
    public void Initialize_ShouldSucceed()
    {
        // Act & Assert
        var exception = Record.Exception(() => _recognition.Initialize());
        Assert.Null(exception);
    }

    [Fact]
    public void Initialize_CalledMultipleTimes_ShouldNotThrow()
    {
        // Act & Assert
        _recognition.Initialize();
        var exception = Record.Exception(() => _recognition.Initialize());
        Assert.Null(exception);
    }

    [Fact]
    public void CompareAudio_WithoutInitialization_ShouldThrowInvalidOperationException()
    {
        // Arrange
        using var uninitializedRecognition = new SpeechBrainRecognition();
        byte[] audio1 = new byte[] { 0x01, 0x02, 0x03 };
        byte[] audio2 = new byte[] { 0x04, 0x05, 0x06 };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => 
            uninitializedRecognition.CompareAudio(audio1, audio2));
    }

    [Fact]
    public void GetEmbedding_WithoutInitialization_ShouldThrowInvalidOperationException()
    {
        // Arrange
        using var uninitializedRecognition = new SpeechBrainRecognition();
        byte[] audio = new byte[] { 0x01, 0x02, 0x03 };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            uninitializedRecognition.GetEmbedding(audio));
    }

    [Fact]
    public void CompareAudio_SamePerson_ShouldReturnHighSimilarityScore()
    {
        // Arrange
        _testOutputHelper.WriteLine("=== CompareAudio_SamePerson_ShouldReturnHighSimilarityScore ===");
        _recognition.Initialize();
        var audio1Path = Path.Combine(_audioSamplesPath, "P1.wav");
        var audio2Path = Path.Combine(_audioSamplesPath, "P2.wav");
        
        _testOutputHelper.WriteLine($"Audio 1: {audio1Path}");
        _testOutputHelper.WriteLine($"Audio 2: {audio2Path}");
        
        Assert.True(File.Exists(audio1Path), $"Audio file not found: {audio1Path}");
        Assert.True(File.Exists(audio2Path), $"Audio file not found: {audio2Path}");
        
        byte[] audio1 = File.ReadAllBytes(audio1Path);
        byte[] audio2 = File.ReadAllBytes(audio2Path);
        
        _testOutputHelper.WriteLine($"Audio 1 size: {audio1.Length} bytes");
        _testOutputHelper.WriteLine($"Audio 2 size: {audio2.Length} bytes");

        // Act
        _testOutputHelper.WriteLine("Comparing audio from same person (P1 vs P2)...");
        var stopwatch = Stopwatch.StartNew();
        var result = _recognition.CompareAudio(audio1, audio2);
        stopwatch.Stop();
        _testOutputHelper.WriteLine($"CompareAudio duration: {stopwatch.ElapsedMilliseconds} ms ({stopwatch.Elapsed.TotalSeconds:F3} seconds)");

        // Assert
        _testOutputHelper.WriteLine($"ComparisonResult:");
        _testOutputHelper.WriteLine($"  - Score: {result.Score:F4}");
        _testOutputHelper.WriteLine($"  - IsMatch: {result.IsMatch}");
        _testOutputHelper.WriteLine($"  - Status: {result.Status}");
        
        Assert.NotNull(result);
        Assert.True(result.Score > 0.5, $"Expected similarity score > 0.5 for same person, but got {result.Score}");
        Assert.True(result.IsMatch, "Expected IsMatch to be true for same person");
        Assert.Equal("success", result.Status);
        
        _testOutputHelper.WriteLine("✓ Test passed");
    }

    [Fact]
    public void CompareAudio_DifferentPerson_ShouldReturnLowSimilarityScore()
    {
        // Arrange
        _recognition.Initialize();
        var audio1Path = Path.Combine(_audioSamplesPath, "P1.wav");
        var audio2Path = Path.Combine(_audioSamplesPath, "B1.wav");
        
        Assert.True(File.Exists(audio1Path), $"Audio file not found: {audio1Path}");
        Assert.True(File.Exists(audio2Path), $"Audio file not found: {audio2Path}");
        
        byte[] audio1 = File.ReadAllBytes(audio1Path);
        byte[] audio2 = File.ReadAllBytes(audio2Path);

        // Act
        var result = _recognition.CompareAudio(audio1, audio2);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Score < 0.5, $"Expected similarity score < 0.5 for different person, but got {result.Score}");
        Assert.False(result.IsMatch, "Expected IsMatch to be false for different person");
        Assert.Equal("success", result.Status);
    }

    [Fact]
    public void CompareAudio_SameFile_ShouldReturnPerfectMatch()
    {
        // Arrange
        _testOutputHelper.WriteLine("=== CompareAudio_SameFile_ShouldReturnPerfectMatch ===");
        _recognition.Initialize();
        var audioPath = Path.Combine(_audioSamplesPath, "P1.wav");
        
        _testOutputHelper.WriteLine($"Audio: {audioPath}");
        Assert.True(File.Exists(audioPath), $"Audio file not found: {audioPath}");
        
        byte[] audio = File.ReadAllBytes(audioPath);
        _testOutputHelper.WriteLine($"Audio size: {audio.Length} bytes");

        // Act
        _testOutputHelper.WriteLine("Comparing same file with itself...");
        var stopwatch = Stopwatch.StartNew();
        var result = _recognition.CompareAudio(audio, audio);
        stopwatch.Stop();
        _testOutputHelper.WriteLine($"CompareAudio duration: {stopwatch.ElapsedMilliseconds} ms ({stopwatch.Elapsed.TotalSeconds:F3} seconds)");

        // Assert
        _testOutputHelper.WriteLine($"ComparisonResult:");
        _testOutputHelper.WriteLine($"  - Score: {result.Score:F4}");
        _testOutputHelper.WriteLine($"  - IsMatch: {result.IsMatch}");
        _testOutputHelper.WriteLine($"  - Status: {result.Status}");
        
        Assert.NotNull(result);
        Assert.True(result.Score > 0.9, $"Expected similarity score > 0.9 for identical audio, but got {result.Score}");
        Assert.True(result.IsMatch, "Expected IsMatch to be true for identical audio");
        Assert.Equal("success", result.Status);
        
        _testOutputHelper.WriteLine("✓ Test passed");
    }

    [Fact]
    public void GetEmbedding_ShouldReturnNonEmptyArray()
    {
        // Arrange
        _recognition.Initialize();
        var audioPath = Path.Combine(_audioSamplesPath, "P1.wav");
        Assert.True(File.Exists(audioPath), $"Audio file not found: {audioPath}");
        
        byte[] audio = File.ReadAllBytes(audioPath);

        // Act
        var embedding = _recognition.GetEmbedding(audio);

        // Assert
        Assert.NotNull(embedding);
        Assert.NotEmpty(embedding);
        Assert.All(embedding, value => Assert.True(!float.IsNaN(value) && !float.IsInfinity(value)));
    }

    [Fact]
    public void GetEmbedding_SameAudio_ShouldReturnConsistentEmbeddings()
    {
        // Arrange
        _recognition.Initialize();
        var audioPath = Path.Combine(_audioSamplesPath, "P1.wav");
        Assert.True(File.Exists(audioPath), $"Audio file not found: {audioPath}");
        
        byte[] audio = File.ReadAllBytes(audioPath);

        // Act
        var embedding1 = _recognition.GetEmbedding(audio);
        var embedding2 = _recognition.GetEmbedding(audio);

        // Assert
        Assert.Equal(embedding1.Length, embedding2.Length);
        for (int i = 0; i < embedding1.Length; i++)
        {
            Assert.Equal(embedding1[i], embedding2[i], precision: 5);
        }
    }

    [Fact]
    public void GetEmbedding_DifferentAudio_ShouldReturnDifferentEmbeddings()
    {
        // Arrange
        _recognition.Initialize();
        var audio1Path = Path.Combine(_audioSamplesPath, "P1.wav");
        var audio2Path = Path.Combine(_audioSamplesPath, "B1.wav");
        
        Assert.True(File.Exists(audio1Path), $"Audio file not found: {audio1Path}");
        Assert.True(File.Exists(audio2Path), $"Audio file not found: {audio2Path}");
        
        byte[] audio1 = File.ReadAllBytes(audio1Path);
        byte[] audio2 = File.ReadAllBytes(audio2Path);

        // Act
        var embedding1 = _recognition.GetEmbedding(audio1);
        var embedding2 = _recognition.GetEmbedding(audio2);

        // Assert
        Assert.Equal(embedding1.Length, embedding2.Length);
        
        // Embeddings should be different for different speakers
        bool hasDifferences = false;
        for (int i = 0; i < embedding1.Length; i++)
        {
            if (Math.Abs(embedding1[i] - embedding2[i]) > 0.001f)
            {
                hasDifferences = true;
                break;
            }
        }
        Assert.True(hasDifferences, "Embeddings for different speakers should be different");
    }

    [Fact]
    public void ComparisonResult_ShouldHaveCorrectProperties()
    {
        // Arrange
        _testOutputHelper.WriteLine("=== ComparisonResult_ShouldHaveCorrectProperties ===");
        _recognition.Initialize();
        var audioPath = Path.Combine(_audioSamplesPath, "P1.wav");
        
        _testOutputHelper.WriteLine($"Audio: {audioPath}");
        byte[] audio = File.ReadAllBytes(audioPath);
        _testOutputHelper.WriteLine($"Audio size: {audio.Length} bytes");

        // Act
        _testOutputHelper.WriteLine("Comparing audio with itself...");
        var stopwatch = Stopwatch.StartNew();
        var result = _recognition.CompareAudio(audio, audio);
        stopwatch.Stop();
        _testOutputHelper.WriteLine($"CompareAudio duration: {stopwatch.ElapsedMilliseconds} ms ({stopwatch.Elapsed.TotalSeconds:F3} seconds)");

        // Assert
        _testOutputHelper.WriteLine($"ComparisonResult:");
        _testOutputHelper.WriteLine($"  - Score: {result.Score:F4} (Type: {result.Score.GetType().Name})");
        _testOutputHelper.WriteLine($"  - IsMatch: {result.IsMatch} (Type: {result.IsMatch.GetType().Name})");
        _testOutputHelper.WriteLine($"  - Status: '{result.Status}' (Type: {result.Status.GetType().Name})");
        _testOutputHelper.WriteLine($"  - Score in range [-1, 1]: {result.Score >= -1.0 && result.Score <= 1.0}");
        
        Assert.NotNull(result);
        Assert.InRange(result.Score, -1.0, 1.0);
        Assert.IsType<bool>(result.IsMatch);
        Assert.NotNull(result.Status);
        Assert.NotEmpty(result.Status);
        
        _testOutputHelper.WriteLine("✓ Test passed");
    }

    [Fact]
    public void CompareAudio_MultipleSequentialCalls_ShouldShowPerformanceMetrics()
    {
        // Arrange
        _testOutputHelper.WriteLine("=== CompareAudio_MultipleSequentialCalls_ShouldShowPerformanceMetrics ===");
        _recognition.Initialize();
        
        var audio1Path = Path.Combine(_audioSamplesPath, "P1.wav");
        var audio2Path = Path.Combine(_audioSamplesPath, "P2.wav");
        var audio3Path = Path.Combine(_audioSamplesPath, "B1.wav");
        
        _testOutputHelper.WriteLine($"Audio 1 (P1): {audio1Path}");
        _testOutputHelper.WriteLine($"Audio 2 (P2): {audio2Path}");
        _testOutputHelper.WriteLine($"Audio 3 (B1): {audio3Path}");
        
        Assert.True(File.Exists(audio1Path), $"Audio file not found: {audio1Path}");
        Assert.True(File.Exists(audio2Path), $"Audio file not found: {audio2Path}");
        Assert.True(File.Exists(audio3Path), $"Audio file not found: {audio3Path}");
        
        byte[] audio1 = File.ReadAllBytes(audio1Path);
        byte[] audio2 = File.ReadAllBytes(audio2Path);
        byte[] audio3 = File.ReadAllBytes(audio3Path);
        
        _testOutputHelper.WriteLine($"Audio 1 size: {audio1.Length} bytes");
        _testOutputHelper.WriteLine($"Audio 2 size: {audio2.Length} bytes");
        _testOutputHelper.WriteLine($"Audio 3 size: {audio3.Length} bytes");
        _testOutputHelper.WriteLine("");

        const int iterations = 10;
        var durations = new List<long>();

        // Act - Multiple sequential calls
        _testOutputHelper.WriteLine($"Running {iterations} sequential CompareAudio calls...");
        _testOutputHelper.WriteLine("=".PadRight(60, '='));

        for (int i = 1; i <= iterations; i++)
        {
            // Alternate between same person and different person comparisons
            byte[] audioA = i % 3 == 0 ? audio1 : (i % 2 == 0 ? audio2 : audio1);
            byte[] audioB = i % 3 == 0 ? audio3 : (i % 2 == 0 ? audio1 : audio2);
            
            var stopwatch = Stopwatch.StartNew();
            var result = _recognition.CompareAudio(audioA, audioB);
            stopwatch.Stop();
            
            durations.Add(stopwatch.ElapsedMilliseconds);
            
            string comparison = i % 3 == 0 ? "P1 vs B1 (different)" : (i % 2 == 0 ? "P2 vs P1 (same)" : "P1 vs P2 (same)");
            _testOutputHelper.WriteLine($"Call #{i:D2} ({comparison}):");
            _testOutputHelper.WriteLine($"  Duration: {stopwatch.ElapsedMilliseconds,6} ms ({stopwatch.Elapsed.TotalSeconds:F3} sec)");
            _testOutputHelper.WriteLine($"  Score: {result.Score:F4}, IsMatch: {result.IsMatch}, Status: {result.Status}");
            _testOutputHelper.WriteLine("");
        }

        // Assert and Log Statistics
        _testOutputHelper.WriteLine("=".PadRight(60, '='));
        _testOutputHelper.WriteLine("PERFORMANCE STATISTICS:");
        _testOutputHelper.WriteLine("=".PadRight(60, '='));
        
        var firstCall = durations[0];
        var lastCall = durations[iterations - 1];
        var minDuration = durations.Min();
        var maxDuration = durations.Max();
        var avgDuration = durations.Average();
        var medianDuration = durations.OrderBy(d => d).ElementAt(iterations / 2);
        
        _testOutputHelper.WriteLine($"First call:      {firstCall,6} ms");
        _testOutputHelper.WriteLine($"Last call:       {lastCall,6} ms");
        _testOutputHelper.WriteLine($"Minimum:         {minDuration,6} ms");
        _testOutputHelper.WriteLine($"Maximum:         {maxDuration,6} ms");
        _testOutputHelper.WriteLine($"Average:         {avgDuration,6:F2} ms");
        _testOutputHelper.WriteLine($"Median:          {medianDuration,6} ms");
        _testOutputHelper.WriteLine("");
        
        if (firstCall > lastCall)
        {
            var improvement = ((firstCall - lastCall) / (double)firstCall) * 100;
            _testOutputHelper.WriteLine($"Performance improved by {improvement:F2}% from first to last call");
        }
        else
        {
            var degradation = ((lastCall - firstCall) / (double)firstCall) * 100;
            _testOutputHelper.WriteLine($"Performance degraded by {degradation:F2}% from first to last call");
        }
        
        _testOutputHelper.WriteLine("");
        _testOutputHelper.WriteLine("Duration trend:");
        for (int i = 0; i < iterations; i++)
        {
            var bar = new string('█', (int)(durations[i] / 10)); // Scale for visualization
            _testOutputHelper.WriteLine($"  Call #{i + 1:D2}: {bar} {durations[i]} ms");
        }
        
        _testOutputHelper.WriteLine("");
        _testOutputHelper.WriteLine("✓ Test completed - check performance metrics above");
        
        // Verify all calls succeeded (no exceptions thrown)
        Assert.Equal(iterations, durations.Count);
    }

    [Fact]
    public void Dispose_ShouldNotThrow()
    {
        // Arrange
        var recognition = new SpeechBrainRecognition();
        recognition.Initialize();

        // Act & Assert
        var exception = Record.Exception(() => recognition.Dispose());
        Assert.Null(exception);
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_ShouldNotThrow()
    {
        // Arrange
        var recognition = new SpeechBrainRecognition();
        recognition.Initialize();

        // Act & Assert
        recognition.Dispose();
        var exception = Record.Exception(() => recognition.Dispose());
        Assert.Null(exception);
    }

    public void Dispose()
    {
        _recognition?.Dispose();
    }
}


