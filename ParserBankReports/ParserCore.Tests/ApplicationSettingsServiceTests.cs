using System;
using System.IO;
using ParserCore.Services;
using Xunit;

namespace ParserCore.Tests
{
    public class ApplicationSettingsServiceTests : IDisposable
    {
        private readonly TestPaths _paths = new TestPaths();
        public void Dispose() => Directory.Delete(_paths.DirectoryPath, true);

        [Fact]
        public void MissingSettingsAreCreatedAndChangesAreSaved()
        {
            var service = new ApplicationSettingsService(_paths);
            var settings = service.LoadSettings();
            Assert.True(File.Exists(_paths.SettingsPath));
            settings.SelectedCategoryScheme = "Test";
            service.SaveSettings(settings);
            Assert.Equal("Test", new ApplicationSettingsService(_paths).LoadSettings().SelectedCategoryScheme);
            Assert.True(File.Exists(_paths.SettingsPath + ".bak"));
        }

        [Fact]
        public void PortableSettingsAreImportedOnlyOnce()
        {
            var settings = ParserCore.DataSet.LoadDefault();
            settings.EncodingPage = 1251;
            settings.Save(_paths.DefaultSettingsPath);
            var service = new ApplicationSettingsService(_paths);
            Assert.Equal(1251, service.LoadSettings().EncodingPage);
            settings.EncodingPage = 65001;
            settings.Save(_paths.DefaultSettingsPath);
            Assert.Equal(1251, service.LoadSettings().EncodingPage);
        }

        [Fact]
        public void HistoriesDefaultToEmptyAndCanBeReplaced()
        {
            var service = new ApplicationSettingsService(_paths);
            Assert.Empty(service.LoadRecentFiles().Reports);
            Assert.Empty(service.LoadCardNames());
            service.SaveRecentFiles(new RecentFileHistory { Reports = { "report.html" }, Projects = { "project.json" } });
            service.SaveCardNames(new[] { "First" });
            service.SaveCardNames(new[] { "Second" });
            var restored = new ApplicationSettingsService(_paths);
            Assert.Equal("report.html", Assert.Single(restored.LoadRecentFiles().Reports));
            Assert.Equal("project.json", Assert.Single(restored.LoadRecentFiles().Projects));
            Assert.Equal("Second", Assert.Single(restored.LoadCardNames()));
            Assert.Empty(Directory.GetFiles(_paths.DirectoryPath, "*.tmp", SearchOption.AllDirectories));
        }

        [Fact]
        public void MalformedHistoryIsReportedAndPreserved()
        {
            File.WriteAllText(_paths.RecentFilesPath, "{broken");
            Assert.ThrowsAny<Newtonsoft.Json.JsonException>(() => new ApplicationSettingsService(_paths).LoadRecentFiles());
            Assert.Equal("{broken", File.ReadAllText(_paths.RecentFilesPath));
        }

        private sealed class TestPaths : IApplicationPaths
        {
            public string DirectoryPath { get; } = Path.Combine(Path.GetTempPath(), "ParserSettingsTests-" + Guid.NewGuid().ToString("N"));
            public TestPaths() => Directory.CreateDirectory(DirectoryPath);
            public string SettingsPath => Path.Combine(DirectoryPath, "user", "settings.json");
            public string DefaultSettingsPath => Path.Combine(DirectoryPath, "portable.json");
            public string RecentFilesPath => Path.Combine(DirectoryPath, "recent.json");
            public string CardNamesPath => Path.Combine(DirectoryPath, "cards.json");
        }
    }
}
