using System.Diagnostics;
using NAudio.Wave;
using Xunit;
using PiperSharp.Models;

namespace PiperSharp.Tests.Tests
{
    public class PiperSharpTests : IAsyncLifetime
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

        public async Task InitializeAsync()
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
        public void DownloadPiper_ShouldCreatePiperDirectory()
        {
            // Assert
            Assert.True(Directory.Exists(_piperPath), "Piper directory should exist after download");
            Assert.True(File.Exists(_piperExecutablePath), "Piper executable should exist");
        }

        [Theory]
        [InlineData("en_US-ryan-high")]
        [InlineData("en_US-hfc_female-medium")]
        [InlineData("en_US-amy-medium")]
        [InlineData("fa_IR-gyro-medium")]
        public async Task DownloadModel_ShouldDownloadAndExtractModel_WhenValidModelNameProvided(string modelName)
        {
            // Arrange
            var modelPath = Path.Combine(_testDirectory, modelName);

            // Act
            var models = await PiperDownloader.GetHuggingFaceModelList();
            Assert.NotNull(models);
            
            var model = models[modelName];
            model = await model.DownloadModel(_testDirectory);

            // Assert
            Assert.True(models.Count > 0, "Model list should contain models");
            Assert.Equal(modelName, model.Key);
            Assert.True(Directory.Exists(modelPath), $"Model directory '{modelPath}' should exist");
            Assert.Equal(modelPath, model.ModelLocation);
        }

        [Theory]
        [InlineData("en_US-ryan-high")]
        [InlineData("en_US-hfc_female-medium")]
        [InlineData("en_US-amy-medium")]
        [InlineData("fa_IR-gyro-medium")]
        public async Task LoadModel_ShouldLoadPreviouslyDownloadedModel_WhenModelExists(string modelName)
        {
            // Arrange
            var modelPath = Path.Combine(_testDirectory, modelName);
            
            // Download the model first
            var models = await PiperDownloader.GetHuggingFaceModelList();
            Assert.NotNull(models);
            var modelToDownload = models[modelName];
            await modelToDownload.DownloadModel(_testDirectory);

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
            var modelPath = Path.Combine(_testDirectory, TestModelName);
            
            // Download the model first
            var models = await PiperDownloader.GetHuggingFaceModelList();
            Assert.NotNull(models);
            var modelToDownload = models[TestModelName];
            await modelToDownload.DownloadModel(_testDirectory);
            
            var model = await VoiceModel.LoadModel(modelPath);
            
            var piperProvider = new PiperProvider(new PiperConfiguration
            {
                ExecutableLocation = _piperExecutablePath,
                Model = model,
            });

            // Act
            var result = await piperProvider.InferAsync(TestPhrase);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Length > MinimumExpectedWavFileSize, 
                $"WAV file should be at least {MinimumExpectedWavFileSize} bytes");
            AssertIsValidWavFile(result);
        }

        [Fact]
        public async Task InferAsync_WithWaveProvider_ShouldGenerateValidAudioStream_WhenProvidedWithText()
        {
            // Arrange
            var modelPath = Path.Combine(_testDirectory, TestModelName);
            
            // Download the model first
            var models = await PiperDownloader.GetHuggingFaceModelList();
            Assert.NotNull(models);
            var modelToDownload = models[TestModelName];
            await modelToDownload.DownloadModel(_testDirectory);
            
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
            Assert.True(bytesRead > 0, "Should read audio data from wave provider");
            
            // Convert to WAV format for validation
            using var rawStream = new RawSourceWaveStream(buffer, 0, bytesRead, piperWaveProvider.WaveFormat);
            using var pcmStream = WaveFormatConversionStream.CreatePcmStream(rawStream);
            using var testStream = new MemoryStream();
            
            WaveFileWriter.WriteWavFileToStream(testStream, pcmStream);
            testStream.Seek(0, SeekOrigin.Begin);
            
            var wavBytes = testStream.ToArray();
            AssertIsValidWavFile(wavBytes);
        }

        [Fact]
        public async Task GetHuggingFaceModelList_ShouldReturnNonEmptyList()
        {
            // Act
            var models = await PiperDownloader.GetHuggingFaceModelList();

            // Assert
            Assert.NotNull(models);
            Assert.True(models!.Count > 0, "Should retrieve at least one model from HuggingFace");
            Assert.True(models.ContainsKey(TestModelName), 
                $"Model list should contain the test model '{TestModelName}'");
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