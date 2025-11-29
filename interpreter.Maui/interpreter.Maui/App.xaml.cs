namespace interpreter.Maui
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            
            // Enforce modern dark mode across the app
            UserAppTheme = AppTheme.Dark;
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }
    }
}
