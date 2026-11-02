using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
namespace ParserCore.Services
{
    public interface IApplicationSettingsService
    {
        IDataSet LoadSettings();
        void SaveSettings(IDataSet settings);
        RecentFileHistory LoadRecentFiles();
        void SaveRecentFiles(RecentFileHistory history);
        string[] LoadCardNames();
        void SaveCardNames(IEnumerable<string> names);
    }
    public sealed class ApplicationSettingsService : IApplicationSettingsService
    {
        private readonly IApplicationPaths _paths;
        public ApplicationSettingsService(IApplicationPaths paths)
        {
            _paths = paths ?? throw new ArgumentNullException(nameof(paths));
        }
        public IDataSet LoadSettings()
        {
            if (!File.Exists(_paths.SettingsPath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_paths.SettingsPath));
                if (File.Exists(_paths.DefaultSettingsPath))
                    File.Copy(_paths.DefaultSettingsPath, _paths.SettingsPath, false);
                else
                    SaveSettings(DataSet.LoadDefault());
            }
            return DataSet.LoadSettings(_paths.SettingsPath)
                ?? throw new JsonSerializationException("Файл настроек не содержит объект настроек.");
        }
        public void SaveSettings(IDataSet settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            Directory.CreateDirectory(Path.GetDirectoryName(_paths.SettingsPath));
            settings.Save(_paths.SettingsPath);
        }
        public RecentFileHistory LoadRecentFiles() => Read<RecentFileHistory>(_paths.RecentFilesPath) ?? new RecentFileHistory();
        public void SaveRecentFiles(RecentFileHistory history) => Write(_paths.RecentFilesPath, history);
        public string[] LoadCardNames() => Read<string[]>(_paths.CardNamesPath) ?? new string[0];
        public void SaveCardNames(IEnumerable<string> names) => Write(_paths.CardNamesPath, names);
        private static T Read<T>(string path) where T : class =>
            File.Exists(path) ? JsonConvert.DeserializeObject<T>(File.ReadAllText(path)) : null;
        private static void Write(string path, object value)
        {
            var json = JsonConvert.SerializeObject(value, Formatting.Indented);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var temporaryPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.WriteAllText(temporaryPath, json);
                if (File.Exists(path)) File.Replace(temporaryPath, path, null);
                else File.Move(temporaryPath, path);
            }
            finally
            {
                if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
            }
        }
    }
    public sealed class RecentFileHistory
    {
        public List<string> Reports { get; set; } = new List<string>();
        public List<string> Projects { get; set; } = new List<string>();
    }
}
