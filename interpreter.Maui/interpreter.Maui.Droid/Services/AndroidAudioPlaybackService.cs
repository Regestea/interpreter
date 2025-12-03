using System;
using System.IO;
using System.Threading.Tasks;
using Android.Media;
using Microsoft.Maui.ApplicationModel;
using interpreter.Maui.Services;

namespace interpreter.Maui.Services;

public class AndroidAudioPlaybackService : IAudioPlaybackService
{
    private MediaPlayer? _player;

    public Task PlayAsync(System.IO.Stream audioStream)
    {
        return Task.Run(async () =>
        {
            try
            {
                _player?.Release();
                _player = new MediaPlayer();
                
                // MediaPlayer requires a file path or file descriptor, so write stream to temp file
                var tempPath = Path.Combine(Path.GetTempPath(), $"audio_{Guid.NewGuid()}.opus");
                
                using (var fileStream = File.Create(tempPath))
                {
                    await audioStream.CopyToAsync(fileStream);
                }
                
                _player.SetDataSource(tempPath);
                _player.SetAudioAttributes(new AudioAttributes.Builder()
                    .SetContentType(AudioContentType.Speech)
                    .SetUsage(AudioUsageKind.Media)
                    .Build());
                _player.Prepare();
                _player.Start();
                
                // Clean up temp file after playback completes
                _player.Completion += (sender, args) =>
                {
                    try
                    {
                        if (File.Exists(tempPath))
                        {
                            File.Delete(tempPath);
                        }
                    }
                    catch
                    {
                        // Ignore cleanup errors
                    }
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Playback failed: {ex}");
            }
        });
    }
}
