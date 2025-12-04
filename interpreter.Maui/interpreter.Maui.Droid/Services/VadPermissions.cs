using Android.Content.PM;
using AndroidX.Core.App;
using AndroidX.Core.Content;

namespace interpreter.Maui.Services;

/// <summary>
/// Android permissions required for VAD audio recording.
/// Required AndroidManifest.xml entry:
/// <uses-permission android:name="android.permission.RECORD_AUDIO" />
/// </summary>
public static class VadPermissions
{
    public const string RecordAudio = Android.Manifest.Permission.RecordAudio;

    /// <summary>
    /// Checks if RECORD_AUDIO permission is granted.
    /// </summary>
    public static bool HasRecordAudioPermission(Android.Content.Context context)
    {
        return ContextCompat.CheckSelfPermission(context, RecordAudio) == Permission.Granted;
    }

    /// <summary>
    /// Requests RECORD_AUDIO permission.
    /// </summary>
    public static void RequestRecordAudioPermission(Android.App.Activity activity, int requestCode = 1001)
    {
        ActivityCompat.RequestPermissions(activity, new[] { RecordAudio }, requestCode);
    }

    /// <summary>
    /// MAUI-specific permission request using ApplicationModel.Permissions.
    /// </summary>
    public static async Task<bool> RequestMauiMicrophonePermissionAsync()
    {
        var status = await Microsoft.Maui.ApplicationModel.Permissions.CheckStatusAsync<Microsoft.Maui.ApplicationModel.Permissions.Microphone>();

        if (status != Microsoft.Maui.ApplicationModel.PermissionStatus.Granted)
        {
            status = await Microsoft.Maui.ApplicationModel.Permissions.RequestAsync<Microsoft.Maui.ApplicationModel.Permissions.Microphone>();
        }

        return status == Microsoft.Maui.ApplicationModel.PermissionStatus.Granted;
    }
}

