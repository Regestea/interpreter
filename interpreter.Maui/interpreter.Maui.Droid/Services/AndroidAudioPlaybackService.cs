using System;
using System.Threading.Tasks;
using Android.Media;
using Microsoft.Maui.ApplicationModel;
using interpreter.Maui.Services;

namespace interpreter.Maui.Services;

public class AndroidAudioPlaybackService : IAudioPlaybackService
{
    private MediaPlayer? _player;

    public Task PlayAsync(string filePath)
    {
        return Task.Run(() =>
        {
            try
            {
                _player?.Release();
                _player = new MediaPlayer();
                _player.SetDataSource(filePath);
                _player.SetAudioAttributes(new AudioAttributes.Builder()
                    .SetContentType(AudioContentType.Speech)
                    .SetUsage(AudioUsageKind.Media)
                    .Build());
                _player.Prepare();
                _player.Start();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Playback failed: {ex}");
            }
        });
    }
}
