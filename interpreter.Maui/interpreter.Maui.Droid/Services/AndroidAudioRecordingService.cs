using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Android.Content;
using Android.OS;
using Android;
using Android.Content.PM;
using Microsoft.Maui.ApplicationModel;
using interpreter.Maui.Services;

namespace interpreter.Maui.Services;

public class AndroidAudioRecordingService : IAndroidAudioRecordingService
{
    private readonly IAudioRecorderService _audioRecorderService;

    public bool IsRecording => RecordingState.IsRecording;

    public AndroidAudioRecordingService(IAudioRecorderService audioRecorderService)
    {
        _audioRecorderService = audioRecorderService ?? throw new ArgumentNullException(nameof(audioRecorderService));
    }

    public async Task<bool> RequestPermissionsAsync()
    {
        var mic = await Permissions.RequestAsync<Permissions.Microphone>();
        return mic == PermissionStatus.Granted;
    }

    public Task StartAsync()
    {
        var context = Platform.CurrentActivity ?? Platform.AppContext;
        var intent = new Intent(context, typeof(RecordingForegroundService));
        intent.SetAction(RecordingForegroundService.ActionStart);

        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            context.StartForegroundService(intent);
        else
            context.StartService(intent);

        return Task.CompletedTask;
    }

    public async Task<string?> StopAsync()
    {
        var context = Platform.CurrentActivity ?? Platform.AppContext;
        var intent = new Intent(context, typeof(RecordingForegroundService));
        intent.SetAction(RecordingForegroundService.ActionStop);
        context.StartService(intent);

        // Wait until service updates state
        var sw = System.Diagnostics.Stopwatch.StartNew();
        while (RecordingState.IsRecording && sw.Elapsed < TimeSpan.FromSeconds(10))
        {
            await Task.Delay(100);
        }
        return RecordingState.LastFilePath;
    }
    
    public async Task<Stream> RecordAudioTrack(int durationSeconds)
    {
        // Ensure permissions are granted before starting recording
        if (!await RequestPermissionsAsync())
        {
            throw new UnauthorizedAccessException("Microphone permissions are not granted.");
        }

        return await Task.Run(() =>
        {
            var duration = TimeSpan.FromSeconds(durationSeconds);
            return _audioRecorderService.RecordForDuration(duration);
        });
    }
}

internal static class RecordingState
{
    private static readonly object _gate = new();
    private static bool _isRecording;
    private static string? _lastFilePath;

    public static bool IsRecording { get { lock (_gate) return _isRecording; } set { lock (_gate) _isRecording = value; } }
    public static string? LastFilePath { get { lock (_gate) return _lastFilePath; } set { lock (_gate) _lastFilePath = value; } }
}