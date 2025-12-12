using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Android.OS;
using Android.Util;
using Models.Shared.Enums;
using Models.Shared.Requests;
using Models.Shared.Responses;
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
    private int _audioFileCounter = 0;
    private readonly string _debugAudioFolder;

    public static AudioProcessQueue Instance => _instance.Value;

    private AudioProcessQueue()
    {
        _opusCodecService = new OpusCodecService();
        _queue = new BlockingCollection<AudioProcess>();
        _cts = new CancellationTokenSource();
        
        // Initialize debug audio folder
        _debugAudioFolder = Path.Combine(Android.App.Application.Context.GetExternalFilesDir(null)?.AbsolutePath ?? "", "DebugAudio");
        if (!Directory.Exists(_debugAudioFolder))
        {
            Directory.CreateDirectory(_debugAudioFolder);
        }
        
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
                // Save audio chunk to debug folder for inspection
                try
                {
                    // Increment counter and save audio file with number in name
                    int fileNumber = Interlocked.Increment(ref _audioFileCounter);
                    string fileName = $"audio_{fileNumber:D4}.wav";
                    string filePath = Path.Combine(_debugAudioFolder, fileName);
                    
                    // Reset stream position to beginning before saving
                    audioProcess.AudioStream.Position = 0;
                    
                    using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                    {
                        await audioProcess.AudioStream.CopyToAsync(fileStream);
                    }
                    
                    // Reset stream position for further processing if needed
                    audioProcess.AudioStream.Position = 0;
                    
                    Debug.WriteLine($"Saved audio file: {filePath}");
                    Log.Info(TAG, $"Saved audio chunk #{fileNumber} to: {filePath}");

                    var app = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity?.Application as IPlatformApplication;
                    var sp = app?.Services;
                    
                    // var apiClient= sp?.GetService<IApiClient>() ?? throw new InvalidOperationException("IApiClient not registered in DI.");
                    // var encodedAudio= await _opusCodecService.EncodeAsync(audioProcess.AudioStream);
                    //
                    // var memoryStream=new MemoryStream();
                    // await encodedAudio.CopyToAsync(memoryStream);
                    //
                    // var request = new InterpreterRequest()
                    // {
                    //     AudioFile = Convert.ToBase64String(memoryStream.ToArray()),
                    //     InputAudioLanguages = InputAudioLanguages.English,
                    //     EnglishVoiceModels = EnglishVoiceModels.EnUsHfcFemaleMedium,
                    //     Modes = Modes.IgnoreMyTalks,
                    //     OutputLanguages = OutputLanguages.Persian,
                    //     UserVoiceDetectorName = "test"
                    // };
                    //
                    // var result=await apiClient.SendAsync("api/Interpreter/UploadEncodeAudio", HttpMethod.Post,request,false);
                    //
                    // var jsonOptions = new JsonSerializerOptions
                    // {
                    //     PropertyNameCaseInsensitive = true
                    // };
                    // var interpreterResponse = JsonSerializer.Deserialize<InterpreterResponse>(result.Content, jsonOptions);
                    //
                    // var streamMemory = new MemoryStream(interpreterResponse.TranslatedAudio);
                    // var decodedAudioStream=await _opusCodecService.DecodeAsync(streamMemory);
                    //
                    // var player= sp?.GetService<IAudioPlaybackService>() ?? throw new InvalidOperationException("IApiClient not registered in DI.");
                    //
                    // await player.PlayAsync(decodedAudioStream);
                    

                }
                catch (Exception ex)
                {
                    Log.Warn(TAG, $"Failed to save debug audio chunk: {ex.Message}");
                }
                
                
                // Dispose the stream after processing
                audioProcess.AudioStream.Dispose();
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

