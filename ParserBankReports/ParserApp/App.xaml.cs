using System;
using System.IO;
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

        public static string GetSettingsPath()
        {
            var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ParserBankReports");
            var path = Path.Combine(directory, "settings.json");
            if (!File.Exists(path))
            {
                Directory.CreateDirectory(directory);
                var portablePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
                if (File.Exists(portablePath))
                    File.Copy(portablePath, path, false);
                else
                    ParserCore.DataSet.LoadDefault().Save(path);
            }
            return path;
        }
    }
}
