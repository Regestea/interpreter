using System.Threading.Tasks;

namespace interpreter.Maui.Services;

public interface IAndroidAudioRecordingService
{
    Task<bool> RequestPermissionsAsync();
    Task StartAsync();
    Task<string?> StopAsync();
    Task<Stream> RecordAudioTrack(int durationSeconds);
    
    bool IsRecording { get; }
}
