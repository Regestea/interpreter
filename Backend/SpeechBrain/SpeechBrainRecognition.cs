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
    /// Sets the main audio model embedding that will be used for comparison
    /// </summary>
    /// <param name="mainAudio">Main audio file as byte array</param>
    /// <returns>Result indicating success or failure</returns>
    public ComparisonResult SetMainModelEmbedding(byte[] mainAudio)
    {
        if (!_initialized || _pythonModule == null)
        {
            throw new InvalidOperationException("SpeechBrain model not initialized. Call Initialize() first.");
        }

        using (Py.GIL())
        {
            // Convert C# byte array to Python bytes
            using var pyAudio = PyObject.FromManagedObject(mainAudio);

            // Call the Python function
            dynamic result = _pythonModule.set_main_model_embedding(pyAudio);

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
                Score = 0.0,
                IsMatch = false,
                Status = (string)result["status"],
                Message = message
            };
        }
    }

    /// <summary>
    /// Compares an audio byte array with the main model embedding
    /// </summary>
    /// <param name="audio">Audio file as byte array to compare</param>
    /// <returns>Comparison result containing score and match status</returns>
    public ComparisonResult CompareAudio(byte[] audio)
    {
        if (!_initialized || _pythonModule == null)
        {
            throw new InvalidOperationException("SpeechBrain model not initialized. Call Initialize() first.");
        }

        using (Py.GIL())
        {
            // Convert C# byte array to Python bytes
            using var pyAudio = PyObject.FromManagedObject(audio);

            // Call the Python function
            dynamic result = _pythonModule.compare_bytes(pyAudio);

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