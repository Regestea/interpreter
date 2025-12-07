using System.Collections.ObjectModel;

namespace interpreter.Maui.Components;

/// <summary>
/// Reusable component for displaying and managing voice profiles list.
/// </summary>
public partial class VoiceProfileList : ContentView
{
    /// <summary>
    /// Event fired when refresh button is clicked
    /// </summary>
    public event EventHandler? RefreshClicked;

    /// <summary>
    /// Event fired when delete button is clicked for a profile
    /// </summary>
    public event EventHandler<Guid>? DeleteClicked;

    /// <summary>
    /// Gets or sets the items source for the list
    /// </summary>
    public IEnumerable<VoiceProfileItem>? ItemsSource
    {
        get => ProfilesCollection.ItemsSource as IEnumerable<VoiceProfileItem>;
        set => ProfilesCollection.ItemsSource = value;
    }

    public VoiceProfileList()
    {
        InitializeComponent();
    }

    private void OnRefreshClicked(object? sender, EventArgs e)
    {
        RefreshClicked?.Invoke(this, EventArgs.Empty);
    }

    private void OnDeleteClicked(object? sender, EventArgs e)
    {
        if (sender is Border border && border.GestureRecognizers[0] is TapGestureRecognizer tap)
        {
            if (tap.CommandParameter is Guid id)
            {
                DeleteClicked?.Invoke(this, id);
            }
        }
    }

    /// <summary>
    /// Updates the list with new items
    /// </summary>
    public void UpdateItems(IEnumerable<VoiceProfileItem> items)
    {
        ProfilesCollection.ItemsSource = items;
    }
}

/// <summary>
/// Model for displaying voice profiles in the list
/// </summary>
public class VoiceProfileItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

