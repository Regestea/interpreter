namespace interpreter.Maui
{
    public partial class AppShell : Shell
    {
        public Command<string> NavigateCommand { get; }

        public AppShell()
        {
            InitializeComponent();

            // Simple navigation command used by drawer items
            NavigateCommand = new Command<string>(OnNavigate);

            // Set BindingContext so drawer items can bind to NavigateCommand
            BindingContext = this;
        }

        private void OnMenuButtonClicked(object sender, EventArgs e)
        {
            // Toggle the Shell flyout (drawer)
            Shell.Current.FlyoutIsPresented = !Shell.Current.FlyoutIsPresented;
        }

        private async void OnNavigate(string? route)
        {
            if (string.IsNullOrWhiteSpace(route))
                return;

            // Check if we're already on the requested page
            var currentRoute = Current?.CurrentState?.Location?.OriginalString ?? string.Empty;
            var targetPage = route.TrimStart('/');
            
            if (currentRoute.Contains(targetPage))
            {
                // Already on this page, just close the flyout
                Shell.Current.FlyoutIsPresented = false;
                return;
            }

            // Close flyout first
            Shell.Current.FlyoutIsPresented = false;

            // Wait for flyout close animation to complete (~250ms is typical)
            await Task.Delay(250);

            // Navigate without animation for instant switch
            await Shell.Current.GoToAsync(route, false);
        }


    }
}