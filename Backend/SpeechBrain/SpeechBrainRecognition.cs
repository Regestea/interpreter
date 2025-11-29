using Python.Runtime;
using System.Reflection;

namespace SpeechBrain;

public class SpeechBrainRecognition : IDisposable
{
    private static readonly object _pythonLock = new object();
    private static bool _pythonEngineInitialized = false;
    
    private dynamic? _pythonModule;
    private bool _initialized;
    private bool _disposed;

    public SpeechBrainRecognition()
    {
        InitializePythonEngine();
    }

    private void InitializePythonEngine()
    {
        lock (_pythonLock)
        {
            if (!_pythonEngineInitialized)
            {
                // Set Python DLL before any initialization
                if (!PythonEngine.IsInitialized)
                {
                    Runtime.PythonDLL = "C:\\Users\\Regestea\\AppData\\Local\\Programs\\Python\\Python310\\python310.dll"; // Adjust to your Python version
                }
                
                // Initialize Python engine
                if (!PythonEngine.IsInitialized)
                {
                    PythonEngine.Initialize();
                    PythonEngine.BeginAllowThreads();
                }
                _pythonEngineInitialized = true;
            }
        }
    }

    /// <summary>
    /// Initializes the SpeechBrain model
    /// </summary>
    public void Initialize()
    {
        if (_initialized)
            return;

        using (Py.GIL())
        {
            // Get the directory where the Python script is located
            var assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var pythonScriptPath = Path.Combine(assemblyPath ?? "", "SpeechBrainMain.py");

            if (!File.Exists(pythonScriptPath))
            {
                // Try relative path from project
                pythonScriptPath = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "Backend", "SpeechBrain", "SpeechBrainMain.py"
                );
            }

            if (!File.Exists(pythonScriptPath))
            {
                throw new FileNotFoundException($"SpeechBrainMain.py not found at {pythonScriptPath}");
            }

            // Add the script directory to Python path safely
            dynamic sys = Py.Import("sys");
            string? scriptDir = Path.GetDirectoryName(pythonScriptPath);

            if (scriptDir != null)
            {
                bool alreadyInPath = false;

                foreach (PyObject p in sys.path)
                {
                    if (p.ToString() == scriptDir)
                    {
                        alreadyInPath = true;
                        break;
                    }
                }

                if (!alreadyInPath)
                {
                    sys.path.append(scriptDir);
                }
            }

            // Import the module
            _pythonModule = Py.Import("SpeechBrainMain");

            // Initialize the model
            _pythonModule.init();
            _initialized = true;
        }
    }


    /// <summary>
    /// Gets the audio embedding from the provided audio file
    /// </summary>
    /// <param name="audioBytes">Audio file as byte array</param>
    /// <returns>Audio embedding as a list of floats</returns>
    public List<float> GetAudioEmbedding(byte[] audioBytes)
    {
        if (!_initialized || _pythonModule == null)
        {
            throw new InvalidOperationException("SpeechBrain model not initialized. Call Initialize() first.");
        }

        using (Py.GIL())
        {
            // Convert C# byte array to Python bytes
            using var pyAudio = PyObject.FromManagedObject(audioBytes);

            // Call the Python function
            dynamic result = _pythonModule.get_audio_embedding(pyAudio);

            // Convert Python list to C# List<float>
            var embeddingList = new List<float>();
            foreach (PyObject item in result)
            {
                embeddingList.Add(item.As<float>());
            }

            return embeddingList;
        }
    }

    /// <summary>
    /// Compares an audio byte array with the provided main audio embedding
    /// </summary>
    /// <param name="audioBytes">Audio file as byte array to compare</param>
    /// <param name="mainEmbedding">Main audio embedding obtained from GetAudioEmbedding</param>
    /// <returns>Comparison result containing score and match status</returns>
    public ComparisonResult CompareAudio(byte[] audioBytes, List<float> mainEmbedding)
    {
        if (!_initialized || _pythonModule == null)
        {
            throw new InvalidOperationException("SpeechBrain model not initialized. Call Initialize() first.");
        }

        using (Py.GIL())
        {
            // Convert C# byte array to Python bytes
            using var pyAudio = PyObject.FromManagedObject(audioBytes);
            
            // Convert C# List<float> to Python list
            using var pyEmbedding = PyObject.FromManagedObject(mainEmbedding);

            // Call the Python function
            dynamic result = _pythonModule.compare_bytes(pyAudio, pyEmbedding);

            // Check for error status
            string status = (string)result["status"];
            if (status == "error")
            {
                string errorMessage = "Unknown error";
                try
                {
                    errorMessage = (string)result["message"];
                }
                catch
                {
                    // Message key doesn't exist
                }
                throw new InvalidOperationException(errorMessage);
            }

            // Extract message if it exists
            string? message = null;
            try
            {
                message = (string)result["message"];
            }
            catch
            {
                // Message key doesn't exist
            }

            // Extract results
            return new ComparisonResult
            {
                Score = (double)result["score"],
                IsMatch = (bool)result["is_match"],
                Status = status,
                Message = message
            };
        }
    }
    

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _pythonModule?.Dispose();
        
        // Note: Don't shutdown Python engine here as it might be used by other instances
        // PythonEngine.Shutdown() should be called when the application exits
    }

    /// <summary>
    /// Shuts down the Python engine. Call this when the application is closing.
    /// </summary>
    public static void ShutdownPython()
    {
        if (PythonEngine.IsInitialized)
        {
            PythonEngine.Shutdown();
        }
    }
}