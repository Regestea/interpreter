using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace interpreter.Maui.ViewModels
{
    /// <summary>
    /// ViewModel for MainPage following MVVM pattern
    /// </summary>
    public class MainViewModel : INotifyPropertyChanged
    {
        private bool _isRecording;
        private bool _isDarkTheme = true; // Force dark theme by default
        private bool _isMenuVisible;
        private string _transcriptText = "Your transcription will appear here...";

        public event PropertyChangedEventHandler? PropertyChanged;

        public bool IsRecording
        {
            get => _isRecording;
            set => SetProperty(ref _isRecording, value);
        }

        public bool IsDarkTheme
        {
            get => _isDarkTheme;
            set => SetProperty(ref _isDarkTheme, value);
        }

        public bool IsMenuVisible
        {
            get => _isMenuVisible;
            set => SetProperty(ref _isMenuVisible, value);
        }

        public string TranscriptText
        {
            get => _transcriptText;
            set => SetProperty(ref _transcriptText, value);
        }

        public bool IsInitialStateVisible => !IsRecording;
        public bool IsRecordingStateVisible => IsRecording;

        public ICommand ActionButtonCommand { get; }
        public ICommand VoiceTuneCommand { get; }
        public ICommand NoiseAdjustCommand { get; }
        public ICommand MenuToggleCommand { get; }
        public ICommand ThemeToggleCommand { get; }

        public MainViewModel()
        {
            ActionButtonCommand = new Command(OnActionButtonExecuted);
            VoiceTuneCommand = new Command(OnVoiceTuneExecuted);
            NoiseAdjustCommand = new Command(OnNoiseAdjustExecuted);
            MenuToggleCommand = new Command(OnMenuToggleExecuted);
            // Theme toggle disabled in dark-only mode
            ThemeToggleCommand = new Command(() => { /* no-op in dark-only theme */ });
        }

        private void OnActionButtonExecuted()
        {
            IsRecording = !IsRecording;
            OnPropertyChanged(nameof(IsInitialStateVisible));
            OnPropertyChanged(nameof(IsRecordingStateVisible));
        }

        private void OnVoiceTuneExecuted()
        {
            // Add your voice tune detection logic here
        }

        private void OnNoiseAdjustExecuted()
        {
            // Add your noise adjustment logic here
        }

        private void OnMenuToggleExecuted()
        {
            IsMenuVisible = !IsMenuVisible;
        }

        // Removed theme toggle behavior (dark-only)

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}

