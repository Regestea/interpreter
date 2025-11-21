using System.Threading.Tasks;

namespace interpreter.Maui.Services;

public interface IAudioRecordingService
{
    Task<bool> RequestPermissionsAsync();
    Task StartAsync();
    Task<string?> StopAsync();
    bool IsRecording { get; }
}
