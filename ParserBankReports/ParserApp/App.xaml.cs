

using System.Windows;

namespace ParserApp
{
    public partial class App : Application
    {
        private readonly ShellBootstrapper<MainWindowViewModel> _bootstrapper = new ShellBootstrapper<MainWindowViewModel>();

        public App()
        {
            _bootstrapper.Initialize();
        }

    }
}
