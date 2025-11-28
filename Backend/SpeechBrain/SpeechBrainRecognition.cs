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
    /// Compares two audio byte arrays and returns a similarity score
    /// </summary>
    /// <param name="audio1">First audio file as byte array</param>
    /// <param name="audio2">Second audio file as byte array</param>
    /// <returns>Comparison result containing score and match status</returns>
    public ComparisonResult CompareAudio(byte[] audio1, byte[] audio2)
    {
        if (!_initialized || _pythonModule == null)
        {
            throw new InvalidOperationException("SpeechBrain model not initialized. Call Initialize() first.");
        }

        using (Py.GIL())
        {
            // Convert C# byte arrays to Python bytes
            using var pyAudio1 = PyObject.FromManagedObject(audio1);
            using var pyAudio2 = PyObject.FromManagedObject(audio2);

            // Call the Python function
            dynamic result = _pythonModule.compare_bytes(pyAudio1, pyAudio2);

            // Extract results
            return new ComparisonResult
            {
                Score = (double)result["score"],
                IsMatch = (bool)result["is_match"],
                Status = (string)result["status"]
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