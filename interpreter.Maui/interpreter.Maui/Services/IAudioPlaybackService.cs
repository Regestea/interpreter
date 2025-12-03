using System.IO;
using System.Threading.Tasks;

namespace interpreter.Maui.Services;

public interface IAudioPlaybackService
{
    Task PlayAsync(Stream audioStream);
}
