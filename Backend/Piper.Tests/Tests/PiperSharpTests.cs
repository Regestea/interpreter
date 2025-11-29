using System.Diagnostics;
using NAudio.Wave;
using Xunit;
using PiperSharp.Models;
using Xunit.Abstractions;

namespace PiperSharp.Tests.Tests
{
    public class PiperSharpTests : IAsyncLifetime
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private const string TestModelName = "en_US-hfc_female-medium";
        private const string TestPhrase = "A rainbow is a meteorological phenomenon that is caused by reflection, refraction, and dispersion of light in water droplets resulting in a spectrum of light appearing in the sky.";
        private const string TestPhrasePersian = "رنگین کمان یک پدیده هواشناسی است که در اثر بازتاب، شکست و پراکندگی نور در قطرات آب ایجاد می‌شود و طیفی از نور در آسمان ظاهر می‌گردد.";
        private const int MinimumExpectedWavFileSize = 20_000;
        private const int WaveBufferSize = 19200;
        
        // WAV file header magic numbers (RIFF)
        private const byte WavMagicByte1 = 82;  // 'R'
        private const byte WavMagicByte2 = 73;  // 'I'
        private const byte WavMagicByte3 = 70;  // 'F'
        private const byte WavMagicByte4 = 70;  // 'F'

        private string _testDirectory = null!;
        private string _piperPath = null!;
        private string _piperExecutablePath = null!;

        public PiperSharpTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        public Task InitializeAsync()
        {
            _testDirectory = Path.Combine(Path.GetTempPath(), "PiperSharpTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testDirectory);
            
            _piperPath = Path.Combine(_testDirectory, "piper");
            _piperExecutablePath = Path.Combine(_piperPath,
                Environment.OSVersion.Platform == PlatformID.Win32NT ? "piper.exe" : "piper");

            // Extract Piper and models from local zip file
            PiperDataExtractor.EnsurePiperExtracted(_testDirectory);

            // Make executable on Unix systems (if needed)
            if (Environment.OSVersion.Platform != PlatformID.Win32NT && File.Exists(_piperExecutablePath))
            {
                MakePiperExecutable();
            }
            
            return Task.CompletedTask;
        }

        public Task DisposeAsync()
        {
            if (Directory.Exists(_testDirectory))
            {
                try
                {
                    Directory.Delete(_testDirectory, true);
                }
                catch
                {
                    // Best effort cleanup
                }
            }
            
            return Task.CompletedTask;
        }

        [Fact]
        public void ExtractPiper_ShouldCreatePiperDirectory()
        {
            // Assert
            Assert.True(Directory.Exists(_piperPath), "Piper directory should exist after extraction");
            Assert.True(File.Exists(_piperExecutablePath), "Piper executable should exist");
        }

        [Theory]
        [InlineData("en_US-ryan-high")]
        [InlineData("en_US-hfc_female-medium")]
        [InlineData("en_US-amy-medium")]
        [InlineData("fa_IR-gyro-medium")]
        public void GetModel_ShouldReturnModel_WhenValidModelNameProvided(string modelName)
        {
            // Arrange
            var modelsDir = Path.Combine(_testDirectory, "models");
            var modelPath = Path.Combine(modelsDir, modelName);

            // Act
            var models = PiperDataExtractor.GetAvailableModels(modelsDir);
            Assert.NotNull(models);
            
            var model = models.GetValueOrDefault(modelName);

            // Assert
            Assert.True(models.Count > 0, "Model list should contain models");
            Assert.NotNull(model);
            Assert.Equal(modelName, model!.Key);
            Assert.True(Directory.Exists(modelPath), $"Model directory '{modelPath}' should exist");
            Assert.Equal(modelPath, model.ModelLocation ?? string.Empty);
        }

        [Theory]
        [InlineData("en_US-ryan-high")]
        [InlineData("en_US-hfc_female-medium")]
        [InlineData("en_US-amy-medium")]
        [InlineData("fa_IR-gyro-medium")]
        public async Task LoadModel_ShouldLoadExtractedModel_WhenModelExists(string modelName)
        {
            // Arrange
            var modelPath = Path.Combine(_testDirectory, "models", modelName);

            // Act
            var model = await VoiceModel.LoadModel(modelPath);

            // Assert
            Assert.NotNull(model);
            Assert.Equal(modelName, model.Key);
        }

        [Fact]
        public async Task InferAsync_ShouldGenerateValidWavData_WhenProvidedWithText()
        {
            // Arrange
            _testOutputHelper.WriteLine("Testing InferAsync with PiperProvider");
            var totalStopwatch = Stopwatch.StartNew();
            
            var modelPath = Path.Combine(_testDirectory, "models", TestModelName);
            
            _testOutputHelper.WriteLine($"Loading model: {TestModelName}");
            var loadStopwatch = Stopwatch.StartNew();
            var model = await VoiceModel.LoadModel(modelPath);
            loadStopwatch.Stop();
            _testOutputHelper.WriteLine($"Model loaded in {loadStopwatch.ElapsedMilliseconds}ms");
            
            _testOutputHelper.WriteLine("Creating PiperProvider...");
            var providerStopwatch = Stopwatch.StartNew();
            var piperProvider = new PiperProvider(new PiperConfiguration
            {
                ExecutableLocation = _piperExecutablePath,
                Model = model,
            });
            providerStopwatch.Stop();
            _testOutputHelper.WriteLine($"PiperProvider created in {providerStopwatch.ElapsedMilliseconds}ms");
            
            // Act
            _testOutputHelper.WriteLine($"Starting InferAsync with text length: {TestPhrase.Length}");
            var inferStopwatch = Stopwatch.StartNew();
            var result = await piperProvider.InferAsync(TestPhrase);
            inferStopwatch.Stop();
            _testOutputHelper.WriteLine($"InferAsync completed in {inferStopwatch.ElapsedMilliseconds}ms");
            _testOutputHelper.WriteLine($"Generated WAV file size: {result.Length} bytes");

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Length > MinimumExpectedWavFileSize, 
                $"WAV file should be at least {MinimumExpectedWavFileSize} bytes");
            AssertIsValidWavFile(result);
            
            totalStopwatch.Stop();
            _testOutputHelper.WriteLine($"Total test duration: {totalStopwatch.ElapsedMilliseconds}ms");
        }

        [Theory]
        [InlineData("en_US-ryan-high", TestPhrase)]
        [InlineData("en_US-hfc_female-medium", TestPhrase)]
        [InlineData("en_US-amy-medium", TestPhrase)]
        [InlineData("fa_IR-gyro-medium", TestPhrasePersian)]
        public async Task InferAsync_ShouldGenerateValidWavData_ForAllModels(string modelName, string testPhrase)
        {
            // Arrange
            _testOutputHelper.WriteLine($"Testing InferAsync with PiperProvider for {modelName}");
            var totalStopwatch = Stopwatch.StartNew();
            
            var modelPath = Path.Combine(_testDirectory, "models", modelName);
            
            _testOutputHelper.WriteLine($"Loading model: {modelName}");
            var loadStopwatch = Stopwatch.StartNew();
            var model = await VoiceModel.LoadModel(modelPath);
            loadStopwatch.Stop();
            _testOutputHelper.WriteLine($"Model loaded in {loadStopwatch.ElapsedMilliseconds}ms");
            
            _testOutputHelper.WriteLine("Creating PiperProvider...");
            var providerStopwatch = Stopwatch.StartNew();
            var piperProvider = new PiperProvider(new PiperConfiguration
            {
                ExecutableLocation = _piperExecutablePath,
                Model = model,
            });
            providerStopwatch.Stop();
            _testOutputHelper.WriteLine($"PiperProvider created in {providerStopwatch.ElapsedMilliseconds}ms");
            
            // Act
            _testOutputHelper.WriteLine($"Starting InferAsync with text length: {testPhrase.Length}");
            _testOutputHelper.WriteLine($"Text: {testPhrase.Substring(0, Math.Min(50, testPhrase.Length))}...");
            var inferStopwatch = Stopwatch.StartNew();
            var result = await piperProvider.InferAsync(testPhrase);
            inferStopwatch.Stop();
            _testOutputHelper.WriteLine($"InferAsync completed in {inferStopwatch.ElapsedMilliseconds}ms");
            _testOutputHelper.WriteLine($"Generated WAV file size: {result.Length} bytes");

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Length > MinimumExpectedWavFileSize, 
                $"WAV file should be at least {MinimumExpectedWavFileSize} bytes");
            AssertIsValidWavFile(result);
            
            totalStopwatch.Stop();
            _testOutputHelper.WriteLine($"Total test duration for {modelName}: {totalStopwatch.ElapsedMilliseconds}ms");
        }

        [Fact]
        public async Task InferAsync_WithWaveProvider_ShouldGenerateValidAudioStream_WhenProvidedWithText()
        {
            // Arrange
            _testOutputHelper.WriteLine("Testing InferAsync with PiperWaveProvider");
            var totalStopwatch = Stopwatch.StartNew();
            
            var modelPath = Path.Combine(_testDirectory, "models", TestModelName);
            
            _testOutputHelper.WriteLine($"Loading model: {TestModelName}");
            var loadStopwatch = Stopwatch.StartNew();
            var model = await VoiceModel.LoadModel(modelPath);
            loadStopwatch.Stop();
            _testOutputHelper.WriteLine($"Model loaded in {loadStopwatch.ElapsedMilliseconds}ms");
            
            _testOutputHelper.WriteLine("Creating PiperWaveProvider...");
            var providerStopwatch = Stopwatch.StartNew();
            var piperWaveProvider = new PiperWaveProvider(new PiperConfiguration
            {
                ExecutableLocation = _piperExecutablePath,
                Model = model,
            });
            providerStopwatch.Stop();
            _testOutputHelper.WriteLine($"PiperWaveProvider created in {providerStopwatch.ElapsedMilliseconds}ms");

            _testOutputHelper.WriteLine("Starting PiperWaveProvider...");
            var startStopwatch = Stopwatch.StartNew();
            piperWaveProvider.Start();
            startStopwatch.Stop();
            _testOutputHelper.WriteLine($"PiperWaveProvider started in {startStopwatch.ElapsedMilliseconds}ms");

            // Act
            _testOutputHelper.WriteLine($"Starting InferPlayback with text length: {TestPhrase.Length}");
            var inferStopwatch = Stopwatch.StartNew();
            await piperWaveProvider.InferPlayback(TestPhrase);
            inferStopwatch.Stop();
            _testOutputHelper.WriteLine($"InferPlayback completed in {inferStopwatch.ElapsedMilliseconds}ms");
            
            _testOutputHelper.WriteLine($"Reading audio data (buffer size: {WaveBufferSize})...");
            var readStopwatch = Stopwatch.StartNew();
            var buffer = new byte[WaveBufferSize];
            var bytesRead = piperWaveProvider.Read(buffer, 0, buffer.Length);
            readStopwatch.Stop();
            _testOutputHelper.WriteLine($"Read {bytesRead} bytes in {readStopwatch.ElapsedMilliseconds}ms");

            // Assert
            Assert.True(bytesRead > 0, "Should read audio data from wave provider");
            
            // Convert to WAV format for validation
            _testOutputHelper.WriteLine("Converting to WAV format for validation...");
            var conversionStopwatch = Stopwatch.StartNew();
            using var rawStream = new RawSourceWaveStream(buffer, 0, bytesRead, piperWaveProvider.WaveFormat);
            using var pcmStream = WaveFormatConversionStream.CreatePcmStream(rawStream);
            using var testStream = new MemoryStream();
            
            WaveFileWriter.WriteWavFileToStream(testStream, pcmStream);
            testStream.Seek(0, SeekOrigin.Begin);
            
            var wavBytes = testStream.ToArray();
            conversionStopwatch.Stop();
            _testOutputHelper.WriteLine($"WAV conversion completed in {conversionStopwatch.ElapsedMilliseconds}ms");
            
            AssertIsValidWavFile(wavBytes);
            
            totalStopwatch.Stop();
            _testOutputHelper.WriteLine($"Total test duration: {totalStopwatch.ElapsedMilliseconds}ms");
        }

        [Theory]
        [InlineData("en_US-ryan-high", TestPhrase)]
        [InlineData("en_US-hfc_female-medium", TestPhrase)]
        [InlineData("en_US-amy-medium", TestPhrase)]
        [InlineData("fa_IR-gyro-medium", TestPhrasePersian)]
        public async Task InferAsync_WithWaveProvider_ShouldGenerateValidAudioStream_ForAllModels(string modelName, string testPhrase)
        {
            // Arrange
            _testOutputHelper.WriteLine($"Testing InferAsync with PiperWaveProvider for {modelName}");
            var totalStopwatch = Stopwatch.StartNew();
            
            var modelPath = Path.Combine(_testDirectory, "models", modelName);
            
            _testOutputHelper.WriteLine($"Loading model: {modelName}");
            var loadStopwatch = Stopwatch.StartNew();
            var model = await VoiceModel.LoadModel(modelPath);
            loadStopwatch.Stop();
            _testOutputHelper.WriteLine($"Model loaded in {loadStopwatch.ElapsedMilliseconds}ms");
            
            _testOutputHelper.WriteLine("Creating PiperWaveProvider...");
            var providerStopwatch = Stopwatch.StartNew();
            var piperWaveProvider = new PiperWaveProvider(new PiperConfiguration
            {
                ExecutableLocation = _piperExecutablePath,
                Model = model,
            });
            providerStopwatch.Stop();
            _testOutputHelper.WriteLine($"PiperWaveProvider created in {providerStopwatch.ElapsedMilliseconds}ms");

            _testOutputHelper.WriteLine("Starting PiperWaveProvider...");
            var startStopwatch = Stopwatch.StartNew();
            piperWaveProvider.Start();
            startStopwatch.Stop();
            _testOutputHelper.WriteLine($"PiperWaveProvider started in {startStopwatch.ElapsedMilliseconds}ms");

            // Act
            _testOutputHelper.WriteLine($"Starting InferPlayback with text length: {testPhrase.Length}");
            _testOutputHelper.WriteLine($"Text: {testPhrase.Substring(0, Math.Min(50, testPhrase.Length))}...");
            var inferStopwatch = Stopwatch.StartNew();
            await piperWaveProvider.InferPlayback(testPhrase);
            inferStopwatch.Stop();
            _testOutputHelper.WriteLine($"InferPlayback completed in {inferStopwatch.ElapsedMilliseconds}ms");
            
            _testOutputHelper.WriteLine($"Reading audio data (buffer size: {WaveBufferSize})...");
            var readStopwatch = Stopwatch.StartNew();
            var buffer = new byte[WaveBufferSize];
            var bytesRead = piperWaveProvider.Read(buffer, 0, buffer.Length);
            readStopwatch.Stop();
            _testOutputHelper.WriteLine($"Read {bytesRead} bytes in {readStopwatch.ElapsedMilliseconds}ms");

            // Assert
            Assert.True(bytesRead > 0, "Should read audio data from wave provider");
            
            // Convert to WAV format for validation
            _testOutputHelper.WriteLine("Converting to WAV format for validation...");
            var conversionStopwatch = Stopwatch.StartNew();
            using var rawStream = new RawSourceWaveStream(buffer, 0, bytesRead, piperWaveProvider.WaveFormat);
            using var pcmStream = WaveFormatConversionStream.CreatePcmStream(rawStream);
            using var testStream = new MemoryStream();
            
            WaveFileWriter.WriteWavFileToStream(testStream, pcmStream);
            testStream.Seek(0, SeekOrigin.Begin);
            
            var wavBytes = testStream.ToArray();
            conversionStopwatch.Stop();
            _testOutputHelper.WriteLine($"WAV conversion completed in {conversionStopwatch.ElapsedMilliseconds}ms");
            
            AssertIsValidWavFile(wavBytes);
            
            totalStopwatch.Stop();
            _testOutputHelper.WriteLine($"Total test duration for {modelName}: {totalStopwatch.ElapsedMilliseconds}ms");
        }

        [Fact]
        public void GetAvailableModels_ShouldReturnNonEmptyList()
        {
            // Arrange
            _testOutputHelper.WriteLine("Testing GetAvailableModels");
            var stopwatch = Stopwatch.StartNew();
            
            var modelsDir = Path.Combine(_testDirectory, "models");
            
            // Act
            _testOutputHelper.WriteLine($"Getting available models from: {modelsDir}");
            var getModelsStopwatch = Stopwatch.StartNew();
            var models = PiperDataExtractor.GetAvailableModels(modelsDir);
            getModelsStopwatch.Stop();
            _testOutputHelper.WriteLine($"GetAvailableModels completed in {getModelsStopwatch.ElapsedMilliseconds}ms");
            _testOutputHelper.WriteLine($"Found {models?.Count ?? 0} models");

            // Assert
            Assert.NotNull(models);
            Assert.True(models.Count > 0, "Should retrieve at least one model from extracted zip");
            Assert.True(models.ContainsKey(TestModelName), 
                $"Model list should contain the test model '{TestModelName}'");
            
            stopwatch.Stop();
            _testOutputHelper.WriteLine($"Total test duration: {stopwatch.ElapsedMilliseconds}ms");
        }

        private static void AssertIsValidWavFile(byte[] data)
        {
            Assert.True(data.Length >= 4, 
                "Data should be at least 4 bytes to contain WAV header");
            Assert.Equal(WavMagicByte1, data[0]);
            Assert.Equal(WavMagicByte2, data[1]);
            Assert.Equal(WavMagicByte3, data[2]);
            Assert.Equal(WavMagicByte4, data[3]);
        }


        private void MakePiperExecutable()
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "/bin/chmod",
                Arguments = $"+x {_piperExecutablePath}",
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            });

            var processNotNull = process != null;
            if (processNotNull)
            {
                process!.WaitForExit();
                
                if (process.ExitCode != 0)
                {
                    var error = process.StandardError.ReadToEnd();
                    throw new InvalidOperationException($"Failed to make Piper executable: {error}");
                }
            }
        }
    }
}