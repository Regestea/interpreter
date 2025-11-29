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
        byte[] audio = new byte[] { 0x01, 0x02, 0x03 };
        var embedding = new List<float> { 0.1f, 0.2f };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => 
            uninitializedRecognition.CompareAudio(audio, embedding));
    }

    [Fact]
    public void GetAudioEmbedding_WithoutInitialization_ShouldThrowInvalidOperationException()
    {
        // Arrange
        using var uninitializedRecognition = new SpeechBrainRecognition();
        byte[] audio = new byte[] { 0x01, 0x02, 0x03 };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => 
            uninitializedRecognition.GetAudioEmbedding(audio));
    }

    [Fact]
    public void GetAudioEmbedding_ShouldSucceed()
    {
        // Arrange
        _testOutputHelper.WriteLine("=== GetAudioEmbedding_ShouldSucceed ===");
        _recognition.Initialize();
        var audioPath = Path.Combine(_audioSamplesPath, "P1.wav");
        
        _testOutputHelper.WriteLine($"Audio: {audioPath}");
        Assert.True(File.Exists(audioPath), $"Audio file not found: {audioPath}");
        
        byte[] audio = File.ReadAllBytes(audioPath);
        _testOutputHelper.WriteLine($"Audio size: {audio.Length} bytes");

        // Act
        _testOutputHelper.WriteLine("Getting audio embedding...");
        var stopwatch = Stopwatch.StartNew();
        var embedding = _recognition.GetAudioEmbedding(audio);
        stopwatch.Stop();
        _testOutputHelper.WriteLine($"GetAudioEmbedding duration: {stopwatch.ElapsedMilliseconds} ms ({stopwatch.Elapsed.TotalSeconds:F3} seconds)");

        // Assert
        _testOutputHelper.WriteLine($"Result:");
        _testOutputHelper.WriteLine($"  - Embedding size: {embedding.Count}");
        _testOutputHelper.WriteLine($"  - First few values: [{string.Join(", ", embedding.Take(5).Select(v => v.ToString("F4")))}...]");
        
        Assert.NotNull(embedding);
        Assert.NotEmpty(embedding);
        
        _testOutputHelper.WriteLine("✓ Test passed");
    }

    [Fact]
    public void CompareAudio_SamePerson_ShouldReturnHighSimilarityScore()
    {
        // Arrange
        _testOutputHelper.WriteLine("=== CompareAudio_SamePerson_ShouldReturnHighSimilarityScore ===");
        _recognition.Initialize();
        var audio1Path = Path.Combine(_audioSamplesPath, "P1.wav");
        var audio2Path = Path.Combine(_audioSamplesPath, "P2.wav");
        
        _testOutputHelper.WriteLine($"Main Audio (P1): {audio1Path}");
        _testOutputHelper.WriteLine($"Test Audio (P2): {audio2Path}");
        
        Assert.True(File.Exists(audio1Path), $"Audio file not found: {audio1Path}");
        Assert.True(File.Exists(audio2Path), $"Audio file not found: {audio2Path}");
        
        byte[] audio1 = File.ReadAllBytes(audio1Path);
        byte[] audio2 = File.ReadAllBytes(audio2Path);
        
        _testOutputHelper.WriteLine($"Main Audio size: {audio1.Length} bytes");
        _testOutputHelper.WriteLine($"Test Audio size: {audio2.Length} bytes");

        // Get main embedding
        _testOutputHelper.WriteLine("Getting embedding from P1...");
        var mainEmbedding = _recognition.GetAudioEmbedding(audio1);
        _testOutputHelper.WriteLine($"Main embedding size: {mainEmbedding.Count}");

        // Act
        _testOutputHelper.WriteLine("Comparing P2 audio with main embedding...");
        var stopwatch = Stopwatch.StartNew();
        var result = _recognition.CompareAudio(audio2, mainEmbedding);
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
        _testOutputHelper.WriteLine("=== CompareAudio_DifferentPerson_ShouldReturnLowSimilarityScore ===");
        _recognition.Initialize();
        var audio1Path = Path.Combine(_audioSamplesPath, "P1.wav");
        var audio2Path = Path.Combine(_audioSamplesPath, "B1.wav");
        
        _testOutputHelper.WriteLine($"Main Audio (P1): {audio1Path}");
        _testOutputHelper.WriteLine($"Test Audio (B1): {audio2Path}");
        
        Assert.True(File.Exists(audio1Path), $"Audio file not found: {audio1Path}");
        Assert.True(File.Exists(audio2Path), $"Audio file not found: {audio2Path}");
        
        byte[] audio1 = File.ReadAllBytes(audio1Path);
        byte[] audio2 = File.ReadAllBytes(audio2Path);
        
        _testOutputHelper.WriteLine($"Main Audio size: {audio1.Length} bytes");
        _testOutputHelper.WriteLine($"Test Audio size: {audio2.Length} bytes");

        // Get main embedding with P1
        _testOutputHelper.WriteLine("Getting embedding from P1...");
        var mainEmbedding = _recognition.GetAudioEmbedding(audio1);
        _testOutputHelper.WriteLine($"Main embedding size: {mainEmbedding.Count}");

        // Act - Compare with B1 (different person)
        _testOutputHelper.WriteLine("Comparing B1 audio with main embedding...");
        var stopwatch = Stopwatch.StartNew();
        var result = _recognition.CompareAudio(audio2, mainEmbedding);
        stopwatch.Stop();
        _testOutputHelper.WriteLine($"CompareAudio duration: {stopwatch.ElapsedMilliseconds} ms ({stopwatch.Elapsed.TotalSeconds:F3} seconds)");

        // Assert
        _testOutputHelper.WriteLine($"ComparisonResult:");
        _testOutputHelper.WriteLine($"  - Score: {result.Score:F4}");
        _testOutputHelper.WriteLine($"  - IsMatch: {result.IsMatch}");
        _testOutputHelper.WriteLine($"  - Status: {result.Status}");
        
        Assert.NotNull(result);
        Assert.True(result.Score < 0.5, $"Expected similarity score < 0.5 for different person, but got {result.Score}");
        Assert.False(result.IsMatch, "Expected IsMatch to be false for different person");
        Assert.Equal("success", result.Status);
        
        _testOutputHelper.WriteLine("✓ Test passed");
    }

    [Fact]
    public void GetAudioEmbedding_CanBeSwitchedBetweenDifferentAudios()
    {
        // Arrange
        _testOutputHelper.WriteLine("=== GetAudioEmbedding_CanBeSwitchedBetweenDifferentAudios ===");
        _recognition.Initialize();
        
        var audio1Path = Path.Combine(_audioSamplesPath, "P1.wav");
        var audio2Path = Path.Combine(_audioSamplesPath, "P2.wav");
        var audio3Path = Path.Combine(_audioSamplesPath, "B1.wav");
        
        _testOutputHelper.WriteLine($"Audio P1: {audio1Path}");
        _testOutputHelper.WriteLine($"Audio P2: {audio2Path}");
        _testOutputHelper.WriteLine($"Audio B1: {audio3Path}");
        
        Assert.True(File.Exists(audio1Path), $"Audio file not found: {audio1Path}");
        Assert.True(File.Exists(audio2Path), $"Audio file not found: {audio2Path}");
        Assert.True(File.Exists(audio3Path), $"Audio file not found: {audio3Path}");
        
        byte[] audio1 = File.ReadAllBytes(audio1Path);
        byte[] audio2 = File.ReadAllBytes(audio2Path);
        byte[] audio3 = File.ReadAllBytes(audio3Path);
        
        _testOutputHelper.WriteLine("");

        // Act & Assert - Get embedding from P1
        _testOutputHelper.WriteLine("Step 1: Getting embedding from P1...");
        var embedding1 = _recognition.GetAudioEmbedding(audio1);
        Assert.NotNull(embedding1);
        Assert.NotEmpty(embedding1);
        _testOutputHelper.WriteLine($"  Embedding size: {embedding1.Count}");
        
        // Compare P2 with P1 embedding (should match - same person)
        _testOutputHelper.WriteLine("Step 2: Comparing P2 with P1 embedding...");
        var comparison1 = _recognition.CompareAudio(audio2, embedding1);
        _testOutputHelper.WriteLine($"  Score: {comparison1.Score:F4}, IsMatch: {comparison1.IsMatch}");
        Assert.True(comparison1.IsMatch, "Expected P2 to match P1 (same person)");
        Assert.True(comparison1.Score > 0.5, $"Expected high score for same person, got {comparison1.Score}");
        
        _testOutputHelper.WriteLine("");

        // Act & Assert - Get embedding from B1
        _testOutputHelper.WriteLine("Step 3: Getting embedding from B1...");
        var embedding3 = _recognition.GetAudioEmbedding(audio3);
        Assert.NotNull(embedding3);
        Assert.NotEmpty(embedding3);
        _testOutputHelper.WriteLine($"  Embedding size: {embedding3.Count}");
        
        // Compare P2 with B1 embedding (should NOT match - different person)
        _testOutputHelper.WriteLine("Step 4: Comparing P2 with B1 embedding...");
        var comparison2 = _recognition.CompareAudio(audio2, embedding3);
        _testOutputHelper.WriteLine($"  Score: {comparison2.Score:F4}, IsMatch: {comparison2.IsMatch}");
        Assert.False(comparison2.IsMatch, "Expected P2 to NOT match B1 (different person)");
        Assert.True(comparison2.Score < 0.5, $"Expected low score for different person, got {comparison2.Score}");
        
        _testOutputHelper.WriteLine("");

        // Act & Assert - Use P1 embedding again
        _testOutputHelper.WriteLine("Step 5: Comparing P2 with P1 embedding again...");
        var comparison3 = _recognition.CompareAudio(audio2, embedding1);
        _testOutputHelper.WriteLine($"  Score: {comparison3.Score:F4}, IsMatch: {comparison3.IsMatch}");
        Assert.True(comparison3.IsMatch, "Expected P2 to match P1 again (same person)");
        Assert.True(comparison3.Score > 0.5, $"Expected high score for same person, got {comparison3.Score}");
        
        _testOutputHelper.WriteLine("");
        _testOutputHelper.WriteLine("✓ Test passed - Embeddings can be obtained and reused for different comparisons");
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

        // Get main embedding
        _testOutputHelper.WriteLine("Getting audio embedding...");
        var mainEmbedding = _recognition.GetAudioEmbedding(audio);

        // Act
        _testOutputHelper.WriteLine("Comparing same file with itself...");
        var stopwatch = Stopwatch.StartNew();
        var result = _recognition.CompareAudio(audio, mainEmbedding);
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
    public void ComparisonResult_ShouldHaveCorrectProperties()
    {
        // Arrange
        _testOutputHelper.WriteLine("=== ComparisonResult_ShouldHaveCorrectProperties ===");
        _recognition.Initialize();
        var audioPath = Path.Combine(_audioSamplesPath, "P1.wav");
        
        _testOutputHelper.WriteLine($"Audio: {audioPath}");
        byte[] audio = File.ReadAllBytes(audioPath);
        _testOutputHelper.WriteLine($"Audio size: {audio.Length} bytes");

        // Get main embedding
        _testOutputHelper.WriteLine("Getting audio embedding...");
        var mainEmbedding = _recognition.GetAudioEmbedding(audio);

        // Act
        _testOutputHelper.WriteLine("Comparing audio with itself...");
        var stopwatch = Stopwatch.StartNew();
        var result = _recognition.CompareAudio(audio, mainEmbedding);
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

        // Get main embedding with P1
        _testOutputHelper.WriteLine("Getting embedding from P1...");
        var mainEmbedding = _recognition.GetAudioEmbedding(audio1);
        _testOutputHelper.WriteLine("");

        const int iterations = 10;
        var durations = new List<long>();

        // Act - Multiple sequential calls
        _testOutputHelper.WriteLine($"Running {iterations} sequential CompareAudio calls...");
        _testOutputHelper.WriteLine("=".PadRight(60, '='));

        for (int i = 1; i <= iterations; i++)
        {
            // Alternate between same person and different person comparisons
            byte[] audioToCompare = i % 3 == 0 ? audio3 : audio2;
            
            var stopwatch = Stopwatch.StartNew();
            var result = _recognition.CompareAudio(audioToCompare, mainEmbedding);
            stopwatch.Stop();
            
            durations.Add(stopwatch.ElapsedMilliseconds);
            
            string comparison = i % 3 == 0 ? "B1 (different)" : "P2 (same)";
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


