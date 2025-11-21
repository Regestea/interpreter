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
            // Navigate to MainPage and trigger menu via ViewModel
            if (CurrentPage is MainPage mainPage)
            {
                // Access the ViewModel's MenuToggleCommand through BindingContext
                if (mainPage.BindingContext is ViewModels.MainViewModel viewModel)
                {
                    viewModel.MenuToggleCommand.Execute(null);
                }
            }
        }
    }
}
