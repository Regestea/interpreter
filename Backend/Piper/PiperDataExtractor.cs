using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using PiperSharp.Models;

namespace PiperSharp
{
    /// <summary>
    /// Manages Piper TTS extraction and model loading from local PiperData.zip file.
    /// Usage:
    /// 1. Call EnsurePiperExtracted() to extract Piper and models from PiperFiles/PiperData.zip
    /// 2. Use GetAvailableModels() to list available voice models
    /// 3. Use GetModelByKey(modelKey) to load a specific model
    /// </summary>
    public static class PiperDataExtractor
    {
        private const string PIPER_FILES_FOLDER = "PiperFiles";
        private const string PIPER_DATA_ZIP = "PiperData.zip";

        public static string DefaultLocation => AppContext.BaseDirectory;
        public static string DefaultModelLocation => Path.Join(DefaultLocation, "models");
        public static string DefaultPiperLocation => Path.Join(DefaultLocation, "piper");
        public static string PiperExecutable => Environment.OSVersion.Platform == PlatformID.Win32NT ? "piper.exe" : "piper";
        public static string DefaultPiperExecutableLocation => Path.Join(DefaultPiperLocation, PiperExecutable);
        public static string PiperFilesPath => Path.Join(GetProjectRoot(), PIPER_FILES_FOLDER);
        public static string PiperDataZipPath => Path.Join(PiperFilesPath, PIPER_DATA_ZIP);
    
        private static Dictionary<string, VoiceModel>? _voiceModels;

        private static string GetProjectRoot()
        {
            // First check the application's base directory (where the app is running from - bin/Debug/net10.0)
            var appBaseDir = AppContext.BaseDirectory;
            var piperFilesPath = Path.Join(appBaseDir, PIPER_FILES_FOLDER);
            if (Directory.Exists(piperFilesPath))
            {
                return appBaseDir;
            }

            // Then check current directory
            var currentDir = DefaultLocation;
            piperFilesPath = Path.Join(currentDir, PIPER_FILES_FOLDER);
            if (Directory.Exists(piperFilesPath))
            {
                return currentDir;
            }

            // Search upwards from current directory
            while (!string.IsNullOrEmpty(currentDir))
            {
                piperFilesPath = Path.Join(currentDir, PIPER_FILES_FOLDER);
                if (Directory.Exists(piperFilesPath))
                {
                    return currentDir;
                }
                var parentDir = Directory.GetParent(currentDir);
                if (parentDir == null) break;
                currentDir = parentDir.FullName;
            }

            // Check the assembly location (Backend/Piper folder)
            var assemblyLocation = Path.GetDirectoryName(typeof(PiperDataExtractor).Assembly.Location);
            if (!string.IsNullOrEmpty(assemblyLocation))
            {
                piperFilesPath = Path.Join(assemblyLocation, PIPER_FILES_FOLDER);
                if (Directory.Exists(piperFilesPath))
                {
                    return assemblyLocation;
                }
                
                // Search upwards from assembly location
                var assemblyDir = assemblyLocation;
                while (!string.IsNullOrEmpty(assemblyDir))
                {
                    piperFilesPath = Path.Join(assemblyDir, PIPER_FILES_FOLDER);
                    if (Directory.Exists(piperFilesPath))
                    {
                        return assemblyDir;
                    }
                    var parentDir = Directory.GetParent(assemblyDir);
                    if (parentDir == null) break;
                    assemblyDir = parentDir.FullName;
                }
            }

            // If not found, default to application base directory
            return appBaseDir;
        }

        /// <summary>
        /// Ensures Piper and models are extracted from the local PiperData.zip file
        /// </summary>
        public static void EnsurePiperExtracted()
        {
            EnsurePiperExtracted(DefaultLocation);
        }

        public static void EnsurePiperExtracted(string extractTo)
        {
            if (!File.Exists(PiperDataZipPath))
            {
                throw new FileNotFoundException($"PiperData.zip not found at {PiperDataZipPath}");
            }

            // Check if already extracted in the target location
            var targetPiperLocation = Path.Join(extractTo, "piper");
            var targetModelLocation = Path.Join(extractTo, "models");
            
            if (Directory.Exists(targetPiperLocation) && Directory.Exists(targetModelLocation))
            {
                return; // Already extracted
            }

            // Create models directory
            if (!Directory.Exists(targetModelLocation))
            {
                Directory.CreateDirectory(targetModelLocation);
            }

            // Extract the zip file
            using (var archive = ZipFile.OpenRead(PiperDataZipPath))
            {
                foreach (var entry in archive.Entries)
                {
                    // Skip directories
                    if (string.IsNullOrEmpty(entry.Name))
                        continue;

                    var fullName = entry.FullName.Replace('\\', '/');
                    string destinationPath;

                    // Check if this is a piper executable or library file
                    if (fullName.StartsWith("piper/"))
                    {
                        // Extract to piper directory
                        destinationPath = Path.Join(extractTo, fullName);
                    }
                    else
                    {
                        // This is a model file - extract to models subdirectory
                        destinationPath = Path.Join(targetModelLocation, fullName);
                    }

                    var destinationDir = Path.GetDirectoryName(destinationPath);
                    
                    if (!string.IsNullOrEmpty(destinationDir) && !Directory.Exists(destinationDir))
                    {
                        Directory.CreateDirectory(destinationDir);
                    }

                    entry.ExtractToFile(destinationPath, overwrite: true);
                }
            }

            // Make piper executable on Unix systems
            var piperExecutablePath = Path.Join(targetPiperLocation, PiperExecutable);
            if (Environment.OSVersion.Platform != PlatformID.Win32NT && File.Exists(piperExecutablePath))
            {
                MakePiperExecutable(piperExecutablePath);
            }
        }

        private static void MakePiperExecutable(string piperExecutablePath)
        {
            try
            {
                using var process = Process.Start(new ProcessStartInfo
                {
                    FileName = "/bin/chmod",
                    Arguments = $"+x {piperExecutablePath}",
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true
                });

                if (process != null)
                {
                    process.WaitForExit();
                    
                    if (process.ExitCode != 0)
                    {
                        var error = process.StandardError.ReadToEnd();
                        throw new InvalidOperationException($"Failed to make Piper executable: {error}");
                    }
                }
            }
            catch
            {
                // Ignore errors on Windows or if chmod is not available
            }
        }

        /// <summary>
        /// Gets available models from the local models directory
        /// </summary>
        public static Dictionary<string, VoiceModel> GetAvailableModels()
        {
            return GetAvailableModels(DefaultModelLocation);
        }

        /// <summary>
        /// Gets available models from the specified models directory
        /// </summary>
        public static Dictionary<string, VoiceModel> GetAvailableModels(string modelsLocation)
        {
            var voiceModels = new Dictionary<string, VoiceModel>();

            if (!Directory.Exists(modelsLocation))
            {
                return voiceModels;
            }

            // Scan all subdirectories in models folder
            var modelDirs = Directory.GetDirectories(modelsLocation);
            foreach (var modelDir in modelDirs)
            {
                try
                {
                    var modelKey = Path.GetFileName(modelDir);
                    var onnxFiles = Directory.GetFiles(modelDir, "*.onnx");
                    var onnxJsonFiles = Directory.GetFiles(modelDir, "*.onnx.json");

                    if (onnxFiles.Length > 0 && onnxJsonFiles.Length > 0)
                    {
                        // Try to load model.json if exists
                        var modelJsonPath = Path.Join(modelDir, "model.json");
                        VoiceModel? model = null;

                        if (File.Exists(modelJsonPath))
                        {
                            var jsonContent = File.ReadAllText(modelJsonPath);
                            model = JsonSerializer.Deserialize<VoiceModel>(jsonContent);
                        }

                        // If model.json doesn't exist or failed to load, create from onnx.json
                        if (model == null)
                        {
                            var onnxJsonContent = File.ReadAllText(onnxJsonFiles[0]);
                            model = JsonSerializer.Deserialize<VoiceModel>(onnxJsonContent);
                            
                            if (model != null)
                            {
                                model.Key = modelKey;
                                model.Files = new Dictionary<string, dynamic>();
                                foreach (var file in Directory.GetFiles(modelDir))
                                {
                                    var fileName = Path.GetFileName(file);
                                    model.Files[fileName] = new { };
                                }
                            }
                        }

                        if (model != null)
                        {
                            model.ModelLocation = modelDir;
                            voiceModels[modelKey] = model;
                        }
                    }
                }
                catch
                {
                    // Skip invalid model directories
                    continue;
                }
            }

            return voiceModels;
        }

        public static VoiceModel? GetModelByKey(string modelName)
        {
            var models = GetAvailableModels();
            return models.GetValueOrDefault(modelName);
        }
    
        public static VoiceModel? TryGetModelByKey(string modelKey)
        {
            var models = GetAvailableModels();
            return models.GetValueOrDefault(modelKey);
        }
    }
}