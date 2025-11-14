using System.Diagnostics;
using NAudio.Wave;
using NUnit.Framework;
using PiperSharp.Models;

namespace PiperSharp.Tests.Tests
{
    [TestFixture]
    public class PiperSharpTests
    {
        private const string TestModelName = "en_US-ryan-high";
        private const string TestPhrase = "Hello there!";
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

        [OneTimeSetUp]
        public async Task OneTimeSetUp()
        {
            _testDirectory = Path.Combine(Path.GetTempPath(), "PiperSharpTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testDirectory);
            
            _piperPath = Path.Combine(_testDirectory, "piper");
            _piperExecutablePath = Path.Combine(_piperPath,
                Environment.OSVersion.Platform == PlatformID.Win32NT ? "piper.exe" : "piper");

            // Download Piper once for all tests
            await PiperDownloader.DownloadPiper().ExtractPiper(_testDirectory);

            // Make executable on Unix systems
            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
            {
                await MakePiperExecutableAsync();
            }
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
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
        }

        [Test]
        [Order(1)]
        public void DownloadPiper_ShouldCreatePiperDirectory()
        {
            // Assert
            Assert.That(Directory.Exists(_piperPath), Is.True, "Piper directory should exist after download");
            Assert.That(File.Exists(_piperExecutablePath), Is.True, "Piper executable should exist");
        }

        [Test]
        [Order(2)]
        [TestCase("en_US-ryan-high")]
        [TestCase("en_US-hfc_female-medium")]
        [TestCase("en_US-amy-medium")]
        [TestCase("fa_IR-gyro-medium")]
        public async Task DownloadModel_ShouldDownloadAndExtractModel_WhenValidModelNameProvided(string modelName)
        {
            // Arrange
            var modelPath = Path.Combine(_testDirectory, modelName);

            // Act
            var models = await PiperDownloader.GetHuggingFaceModelList();
            Assert.That(models, Is.Not.Null, "Model list should not be null");
            
            var model = models[modelName];
            model = await model.DownloadModel(_testDirectory);

            // Assert
            Assert.That(models.Count, Is.GreaterThan(0), "Model list should contain models");
            Assert.That(model.Key, Is.EqualTo(modelName), $"Model key should be '{modelName}'");
            Assert.That(Directory.Exists(modelPath), Is.True, $"Model directory '{modelPath}' should exist");
            Assert.That(model.ModelLocation, Is.EqualTo(modelPath), "Model location should match expected path");
        }

        [Test]
        [Order(3)]
        [TestCase("en_US-ryan-high")]
        [TestCase("en_US-hfc_female-medium")]
        [TestCase("en_US-amy-medium")]
        [TestCase("fa_IR-gyro-medium")]
        public async Task LoadModel_ShouldLoadPreviouslyDownloadedModel_WhenModelExists(string modelName)
        {
            // Arrange
            var modelPath = Path.Combine(_testDirectory, modelName);

            // Act
            var model = await VoiceModel.LoadModel(modelPath);

            // Assert
            Assert.That(model, Is.Not.Null, "Loaded model should not be null");
            Assert.That(model.Key, Is.EqualTo(modelName), $"Loaded model key should be '{modelName}'");
        }

        [Test]
        [Order(4)]
        public async Task InferAsync_ShouldGenerateValidWavData_WhenProvidedWithText()
        {
            // Arrange
            var modelPath = Path.Combine(_testDirectory, TestModelName);
            var model = await VoiceModel.LoadModel(modelPath);
            
            var piperProvider = new PiperProvider(new PiperConfiguration
            {
                ExecutableLocation = _piperExecutablePath,
                Model = model,
            });

            // Act
            var result = await piperProvider.InferAsync(TestPhrase);

            // Assert
            Assert.That(result, Is.Not.Null, "Result should not be null");
            Assert.That(result.Length, Is.GreaterThan(MinimumExpectedWavFileSize), 
                $"WAV file should be at least {MinimumExpectedWavFileSize} bytes");
            AssertIsValidWavFile(result);
        }

        [Test]
        [Order(5)]
        public async Task InferAsync_WithWaveProvider_ShouldGenerateValidAudioStream_WhenProvidedWithText()
        {
            // Arrange
            var modelPath = Path.Combine(_testDirectory, TestModelName);
            var model = await VoiceModel.LoadModel(modelPath);
            
            var piperWaveProvider = new PiperWaveProvider(new PiperConfiguration
            {
                ExecutableLocation = _piperExecutablePath,
                Model = model,
            });

            piperWaveProvider.Start();

            // Act
            await piperWaveProvider.InferPlayback(TestPhrase);
            
            var buffer = new byte[WaveBufferSize];
            var bytesRead = piperWaveProvider.Read(buffer, 0, buffer.Length);

            // Assert
            Assert.That(bytesRead, Is.GreaterThan(0), "Should read audio data from wave provider");
            
            // Convert to WAV format for validation
            using var rawStream = new RawSourceWaveStream(buffer, 0, bytesRead, piperWaveProvider.WaveFormat);
            using var pcmStream = WaveFormatConversionStream.CreatePcmStream(rawStream);
            using var testStream = new MemoryStream();
            
            WaveFileWriter.WriteWavFileToStream(testStream, pcmStream);
            testStream.Seek(0, SeekOrigin.Begin);
            
            var wavBytes = testStream.ToArray();
            AssertIsValidWavFile(wavBytes);
        }

        [Test]
        public async Task GetHuggingFaceModelList_ShouldReturnNonEmptyList()
        {
            // Act
            var models = await PiperDownloader.GetHuggingFaceModelList();

            // Assert
            Assert.That(models, Is.Not.Null, "Model list should not be null");
            Assert.That(models!.Count, Is.GreaterThan(0), "Should retrieve at least one model from HuggingFace");
            Assert.That(models.ContainsKey(TestModelName), Is.True, 
                $"Model list should contain the test model '{TestModelName}'");
        }

        private static void AssertIsValidWavFile(byte[] data)
        {
            Assert.That(data.Length, Is.GreaterThanOrEqualTo(4), 
                "Data should be at least 4 bytes to contain WAV header");
            Assert.That(data[0], Is.EqualTo(WavMagicByte1), "First byte should be 'R' (82)");
            Assert.That(data[1], Is.EqualTo(WavMagicByte2), "Second byte should be 'I' (73)");
            Assert.That(data[2], Is.EqualTo(WavMagicByte3), "Third byte should be 'F' (70)");
            Assert.That(data[3], Is.EqualTo(WavMagicByte4), "Fourth byte should be 'F' (70)");
        }

        private async Task MakePiperExecutableAsync()
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "/bin/chmod",
                Arguments = $"+x {_piperExecutablePath}",
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            });

            if (process != null)
            {
                await process.WaitForExitAsync();
                
                if (process.ExitCode != 0)
                {
                    var error = await process.StandardError.ReadToEndAsync();
                    throw new InvalidOperationException($"Failed to make Piper executable: {error}");
                }
            }
        }
    }
}