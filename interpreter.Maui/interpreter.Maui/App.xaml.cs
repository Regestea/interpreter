namespace interpreter.Maui
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            
            // Set light theme as default on app start
            UserAppTheme = AppTheme.Light;
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }
    }
}
