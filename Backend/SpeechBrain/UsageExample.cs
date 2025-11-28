using SpeechBrain;

namespace SpeechBrain.Examples;

/// <summary>
/// Example usage of SpeechBrainRecognition wrapper
/// </summary>
public class UsageExample
{
    public static void ExampleUsage()
    {
        // Create an instance of the SpeechBrain recognition
        using var recognition = new SpeechBrainRecognition();
        
        try
        {
            // Initialize the model (this will download the model on first run)
            Console.WriteLine("Initializing SpeechBrain model...");
            recognition.Initialize();
            Console.WriteLine("Model initialized successfully!");
            
            // Load your audio files as byte arrays
            byte[] audio1 = File.ReadAllBytes("path/to/audio1.wav");
            byte[] audio2 = File.ReadAllBytes("path/to/audio2.wav");
            
            // Compare two audio files
            Console.WriteLine("\nComparing audio files...");
            var result = recognition.CompareAudio(audio1, audio2);
            
            Console.WriteLine($"Similarity Score: {result.Score:F4}");
            Console.WriteLine($"Is Match: {result.IsMatch}");
            Console.WriteLine($"Status: {result.Status}");
            
            // Get embedding for a single audio file
            Console.WriteLine("\nGetting embedding for audio1...");
            float[] embedding = recognition.GetEmbedding(audio1);
            Console.WriteLine($"Embedding dimension: {embedding.Length}");
            Console.WriteLine($"First 5 values: {string.Join(", ", embedding.Take(5).Select(x => x.ToString("F4")))}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
        finally
        {
            // Cleanup Python engine when application exits
            // Only call this once when your application is shutting down
            // SpeechBrainRecognition.ShutdownPython();
        }
    }
    
    public static async Task ExampleUsageAsync()
    {
        // For async usage
        await Task.Run(() =>
        {
            using var recognition = new SpeechBrainRecognition();
            recognition.Initialize();
            
            // ... perform operations
        });
    }
}

