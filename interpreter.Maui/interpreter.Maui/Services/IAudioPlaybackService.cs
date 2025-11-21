using System.Threading.Tasks;

namespace interpreter.Maui.Services;

public interface IAudioPlaybackService
{
    Task PlayAsync(string filePath);
}
