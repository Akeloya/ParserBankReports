using System;
using System.IO;
namespace ParserCore.Services
{
    public interface IApplicationPaths
    {
        string SettingsPath { get; }
        string DefaultSettingsPath { get; }
        string RecentFilesPath { get; }
        string CardNamesPath { get; }
    }
    public sealed class ApplicationPaths : IApplicationPaths
    {
        private readonly string _directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ParserBankReports");
        public string SettingsPath => Path.Combine(_directory, "settings.json");
        public string DefaultSettingsPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
        public string RecentFilesPath => Path.Combine(_directory, "recent-files.json");
        public string CardNamesPath => Path.Combine(_directory, "card-names.json");
    }
}
