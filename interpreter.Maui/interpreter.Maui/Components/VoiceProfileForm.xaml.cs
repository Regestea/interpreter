namespace interpreter.Maui.Components;

/// <summary>
/// Reusable form component for creating voice profiles.
/// Contains name entry and voice recording functionality.
/// </summary>
public partial class VoiceProfileForm : ContentView
{
    private bool _isRecording;
    private byte[]? _recordedAudioBytes;

    /// <summary>
    /// Event fired when user clicks Save with valid data
    /// </summary>
    public event EventHandler<VoiceProfileSaveEventArgs>? SaveClicked;

    /// <summary>
    /// Event fired when recording starts
    /// </summary>
    public event EventHandler? RecordingStarted;

    /// <summary>
    /// Event fired when recording stops
    /// </summary>
    public event EventHandler? RecordingStopped;

    /// <summary>
    /// Event fired when validation fails
    /// </summary>
    public event EventHandler<string>? ValidationFailed;

    /// <summary>
    /// Gets or sets the recorded audio bytes
    /// </summary>
    public byte[]? RecordedAudioBytes
    {
        get => _recordedAudioBytes;
        set => _recordedAudioBytes = value;
    }

    /// <summary>
    /// Gets the entered name
    /// </summary>
    public string EnteredName => NameEntry.Text ?? string.Empty;

    /// <summary>
    /// Gets whether currently recording
    /// </summary>
    public bool IsRecording => _isRecording;

    public VoiceProfileForm()
    {
        InitializeComponent();
    }

    private void OnNameTextChanged(object? sender, TextChangedEventArgs e)
    {
        // Reset border color when user types
        NameEntryBorder.Stroke = Color.FromArgb("#374151");
    }

    private async void OnRecordClicked(object? sender, EventArgs e)
    {
        if (!_isRecording)
        {
            // Start recording
            _isRecording = true;
            RecordButton.BackgroundColor = Color.FromArgb("#22C55E");
            RecordIcon.Text = "⏹️";
            RecordingStatusLabel.Text = "Recording...";
            RecordingStatusLabel.TextColor = Color.FromArgb("#EF4444");

            // Pulse animation
            await RecordButton.ScaleToAsync(1.1, 200);
            await RecordButton.ScaleToAsync(1.0, 200);

            RecordingStarted?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            // Stop recording
            _isRecording = false;
            RecordButton.BackgroundColor = Color.FromArgb("#EF4444");
            RecordIcon.Text = "🎙️";
            RecordingStatusLabel.Text = "Recording saved ✓";
            RecordingStatusLabel.TextColor = Color.FromArgb("#22C55E");

            RecordingStopped?.Invoke(this, EventArgs.Empty);
        }
    }

    private void OnSaveClicked(object? sender, EventArgs e)
    {
        var name = NameEntry.Text;

        if (string.IsNullOrWhiteSpace(name))
        {
            NameEntryBorder.Stroke = Color.FromArgb("#EF4444");
            ValidationFailed?.Invoke(this, "Please enter a name for the voice profile.");
            return;
        }

        if (_recordedAudioBytes == null || _recordedAudioBytes.Length == 0)
        {
            ValidationFailed?.Invoke(this, "Please record a voice sample first.");
            return;
        }

        SaveClicked?.Invoke(this, new VoiceProfileSaveEventArgs(name, _recordedAudioBytes));
    }

    /// <summary>
    /// Resets the form to initial state
    /// </summary>
    public void Reset()
    {
        NameEntry.Text = string.Empty;
        NameEntryBorder.Stroke = Color.FromArgb("#374151");
        _isRecording = false;
        _recordedAudioBytes = null;
        RecordButton.BackgroundColor = Color.FromArgb("#EF4444");
        RecordIcon.Text = "🎙️";
        RecordingStatusLabel.Text = "Ready to record";
        RecordingStatusLabel.TextColor = Color.FromArgb("#9CA3AF");
    }

    /// <summary>
    /// Sets the recording status text
    /// </summary>
    public void SetRecordingStatus(string status, bool isSuccess = false)
    {
        RecordingStatusLabel.Text = status;
        RecordingStatusLabel.TextColor = isSuccess
            ? Color.FromArgb("#22C55E")
            : Color.FromArgb("#9CA3AF");
    }
}

/// <summary>
/// Event args for the Save event
/// </summary>
public class VoiceProfileSaveEventArgs : EventArgs
{
    public string Name { get; }
    public byte[] VoiceData { get; }

    public VoiceProfileSaveEventArgs(string name, byte[] voiceData)
    {
        Name = name;
        VoiceData = voiceData;
    }
}
