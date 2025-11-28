using Python.Runtime;

namespace SpeechBrain.Tests;

public class PythonEngineFixture : IDisposable
{
    public PythonEngineFixture()
    {
        // Set Python DLL before any test runs
        if (!PythonEngine.IsInitialized)
        {
            Runtime.PythonDLL = "python310.dll"; // Adjust to your Python version
        }
    }

    public void Dispose()
    {
        // Optional: Cleanup when all tests are done
    }
}

