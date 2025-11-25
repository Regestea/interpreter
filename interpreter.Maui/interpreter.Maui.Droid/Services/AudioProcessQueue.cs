using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Android.OS;
using Android.Util;
using Debug = System.Diagnostics.Debug;
using OperationCanceledException = System.OperationCanceledException;

namespace interpreter.Maui.Services;

/// <summary>
/// Singleton queue that processes audio items sequentially.
/// </summary>
public sealed class AudioProcessQueue
{
    private static readonly Lazy<AudioProcessQueue> _instance = new(() => new AudioProcessQueue());
    private readonly BlockingCollection<AudioProcess> _queue;
    private readonly CancellationTokenSource _cts;
    private readonly Task _processingTask;
    private const string TAG = "AudioProcessQueue";

    public static AudioProcessQueue Instance => _instance.Value;

    private AudioProcessQueue()
    {
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
        Log.Info(TAG, $"Enqueued audio process: {audioProcess.Name}");
    }

    /// <summary>
    /// Continuously processes items from the queue.
    /// </summary>
    private async Task ProcessQueueAsync()
    {
        Log.Info(TAG, "Audio process queue started");

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
        // For now, just log the name
        // TODO: Add actual audio processing logic here
        
        await Task.Delay(100); // Simulate some processing
        
        Log.Info(TAG, $"Completed processing audio: {audioProcess.Name}");
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

