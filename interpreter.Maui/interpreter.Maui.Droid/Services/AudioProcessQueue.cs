using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Android.OS;
using Android.Util;
using Opus.Services;
using Debug = System.Diagnostics.Debug;
using OperationCanceledException = System.OperationCanceledException;

namespace interpreter.Maui.Services;

/// <summary>
/// Singleton queue that processes audio items sequentially.
/// </summary>
public sealed class AudioProcessQueue
{
    private readonly IOpusCodecService _opusCodecService;
    private static readonly Lazy<AudioProcessQueue> _instance = new(() => new AudioProcessQueue());
    private readonly BlockingCollection<AudioProcess> _queue;
    private readonly CancellationTokenSource _cts;
    private readonly Task _processingTask;
    private const string TAG = "AudioProcessQueue";

    public static AudioProcessQueue Instance => _instance.Value;

    private AudioProcessQueue()
    {
        _opusCodecService = new OpusCodecService();
        _queue = new BlockingCollection<AudioProcess>();
        _cts = new CancellationTokenSource();
        _processingTask = Task.Run(ProcessQueueAsync);
    }

    /// <summary>
    /// Enqueues an audio process item for processing.
    /// </summary>
    public void Enqueue(AudioProcess audioProcess)
    {
        if (audioProcess == null)
            throw new ArgumentNullException(nameof(audioProcess));

        _queue.Add(audioProcess);
        
        Debug.WriteLine(TAG, $"Enqueued audio process: {audioProcess.Name}");
    }

    /// <summary>
    /// Continuously processes items from the queue.
    /// </summary>
    private async Task ProcessQueueAsync()
    {
        Debug.WriteLine(TAG," Starting audio process queue");

        try
        {
            foreach (var audioProcess in _queue.GetConsumingEnumerable(_cts.Token))
            {
                try
                {
                    await ProcessAudioAsync(audioProcess);
                }
                catch (Exception ex)
                {
                    Log.Error(TAG, $"Error processing audio '{audioProcess.Name}': {ex.Message}");
                }
            }
        }
        catch (OperationCanceledException)
        {
            Log.Info(TAG, "Audio process queue cancelled");
        }
        catch (Exception ex)
        {
            Log.Error(TAG, $"Fatal error in audio process queue: {ex.Message}");
        }
    }

    /// <summary>
    /// Processes a single audio item.
    /// </summary>
    private async Task ProcessAudioAsync(AudioProcess audioProcess)
    {
        Debug.WriteLine($"Processing audio: {audioProcess.Name}");
        
        try
        {
            if (audioProcess.AudioStream != null)
            {
                // Calculate audio duration from stream size
                // WAV format: 44 bytes header + PCM data
                // Sample rate: 44100 Hz, Channels: 1 (Mono), Bits per sample: 16
                long streamSize = audioProcess.AudioStream.Length;
                long audioDataSize = streamSize - 44; // Subtract WAV header
                
                const int sampleRate = 44100;
                const int channels = 1;
                const int bitsPerSample = 16;
                int bytesPerSecond = sampleRate * channels * (bitsPerSample / 8);
                
                double durationSeconds = audioDataSize > 0 
                    ? (double)audioDataSize / bytesPerSecond 
                    : 0;
                
                // Process stream-based audio (from chunked recording)
                Debug.WriteLine($"Processing audio stream chunk: {audioProcess.Name}, Size: {streamSize} bytes, Duration: {durationSeconds:F2}s");
                Debug.WriteLine(TAG, $"Audio chunk duration: {durationSeconds:F2}s ({audioDataSize} bytes of audio data)");
                
                // Save audio chunk to debug folder for inspection
                try
                {

                    var encodedAudio= await _opusCodecService.EncodeAsync(audioProcess.AudioStream);
                    
                  
                }
                catch (Exception ex)
                {
                    Log.Warn(TAG, $"Failed to save debug audio chunk: {ex.Message}");
                }
                
                // TODO: Add actual audio processing logic here (e.g., send to Whisper API)
                await Task.Delay(100); // Simulate some processing
                
                // Dispose the stream after processing
                audioProcess.AudioStream.Dispose();
            }
            else
            {
                // Process file-based audio (legacy path)
                Debug.WriteLine($"Processing audio file: {audioProcess.Name}");
                
                // TODO: Add actual audio processing logic here
                await Task.Delay(100); // Simulate some processing
            }
            
            Log.Info(TAG, $"Completed processing audio: {audioProcess.Name}");
        }
        finally
        {
            // Ensure stream is disposed even if processing fails
            audioProcess.AudioStream?.Dispose();
        }
    }

    /// <summary>
    /// Stops the queue processing.
    /// </summary>
    public void Stop()
    {
        Log.Info(TAG, "Stopping audio process queue");
        _queue.CompleteAdding();
        _cts.Cancel();
    }

    /// <summary>
    /// Gets the current queue count.
    /// </summary>
    public int QueueCount => _queue.Count;
}

