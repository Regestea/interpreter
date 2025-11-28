namespace SpeechBrain.Tests;

[Collection("PythonEngine")]
public class SpeechBrainRecognitionTests : IDisposable
{
    private readonly SpeechBrainRecognition _recognition;
    private readonly string _audioSamplesPath;

    public SpeechBrainRecognitionTests()
    {
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
        _recognition.Initialize();
        var audio1Path = Path.Combine(_audioSamplesPath, "P1.wav");
        var audio2Path = Path.Combine(_audioSamplesPath, "P2.wav");
        
        Assert.True(File.Exists(audio1Path), $"Audio file not found: {audio1Path}");
        Assert.True(File.Exists(audio2Path), $"Audio file not found: {audio2Path}");
        
        byte[] audio1 = File.ReadAllBytes(audio1Path);
        byte[] audio2 = File.ReadAllBytes(audio2Path);

        // Act
        var result = _recognition.CompareAudio(audio1, audio2);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Score > 0.5, $"Expected similarity score > 0.5 for same person, but got {result.Score}");
        Assert.True(result.IsMatch, "Expected IsMatch to be true for same person");
        Assert.Equal("success", result.Status);
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
        _recognition.Initialize();
        var audioPath = Path.Combine(_audioSamplesPath, "P1.wav");
        Assert.True(File.Exists(audioPath), $"Audio file not found: {audioPath}");
        
        byte[] audio = File.ReadAllBytes(audioPath);

        // Act
        var result = _recognition.CompareAudio(audio, audio);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Score > 0.9, $"Expected similarity score > 0.9 for identical audio, but got {result.Score}");
        Assert.True(result.IsMatch, "Expected IsMatch to be true for identical audio");
        Assert.Equal("success", result.Status);
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
        _recognition.Initialize();
        var audioPath = Path.Combine(_audioSamplesPath, "P1.wav");
        byte[] audio = File.ReadAllBytes(audioPath);

        // Act
        var result = _recognition.CompareAudio(audio, audio);

        // Assert
        Assert.NotNull(result);
        Assert.InRange(result.Score, -1.0, 1.0);
        Assert.IsType<bool>(result.IsMatch);
        Assert.NotNull(result.Status);
        Assert.NotEmpty(result.Status);
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


