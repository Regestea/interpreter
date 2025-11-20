namespace interpreter.Maui
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
        }

        private void OnMenuButtonClicked(object sender, EventArgs e)
        {
            // Navigate to MainPage and trigger menu
            if (CurrentPage is MainPage mainPage)
            {
                mainPage.ToggleMenu();
            }
        }
    }
}
