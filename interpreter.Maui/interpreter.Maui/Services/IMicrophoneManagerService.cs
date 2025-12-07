namespace interpreter.Maui.Services;

/// <summary>
/// Interface for managing microphone devices.
/// </summary>
public interface IMicrophoneManagerService
{
    /// <summary>
    /// Gets the list of available microphones.
    /// </summary>
    /// <returns>List of available microphones.</returns>
    IReadOnlyList<IMicrophoneInfo> GetAvailableMicrophones();

    /// <summary>
    /// Gets the currently selected microphone.
    /// </summary>
    /// <returns>The selected microphone or null if none selected.</returns>
    IMicrophoneInfo? GetSelectedMicrophone();

    /// <summary>
    /// Sets the selected microphone by its ID.
    /// </summary>
    /// <param name="microphoneId">The ID of the microphone to select.</param>
    /// <returns>True if the microphone was found and selected, false otherwise.</returns>
    bool SetSelectedMicrophone(int microphoneId);

    /// <summary>
    /// Sets the selected microphone.
    /// </summary>
    /// <param name="microphone">The microphone to select.</param>
    void SetSelectedMicrophone(IMicrophoneInfo microphone);

    /// <summary>
    /// Resets the selected microphone to the default phone microphone.
    /// </summary>
    void ResetToDefault();

    /// <summary>
    /// Refreshes the list of available microphones.
    /// </summary>
    void RefreshMicrophones();
}

