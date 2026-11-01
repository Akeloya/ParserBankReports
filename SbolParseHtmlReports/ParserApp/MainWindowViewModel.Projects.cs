using Caliburn.Micro;
using Newtonsoft.Json;
using ParserApp.Controls;
using ParserCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace ParserApp
{
    public partial class MainWindowViewModel
    {
        private string _reportPath;
        private string _projectPath;
        public BindableCollection<string> RecentReports { get; } = new BindableCollection<string>();
        public BindableCollection<string> RecentProjects { get; } = new BindableCollection<string>();
        private static string HistoryPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SberParseHtmlReports", "recent-files.json");

        private void LoadRecentFiles()
        {
            try
            {
                if (!File.Exists(HistoryPath)) return;
                var history = JsonConvert.DeserializeObject<RecentFileHistory>(File.ReadAllText(HistoryPath));
                RecentReports.AddRange((history?.Reports ?? new List<string>()).Where(p => !string.IsNullOrWhiteSpace(p)).Take(10));
                RecentProjects.AddRange((history?.Projects ?? new List<string>()).Where(p => !string.IsNullOrWhiteSpace(p)).Take(10));
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is JsonException)
            { SchemeError = "Не удалось загрузить последние файлы: " + ex.Message; }
        }

        private void RememberFile(string path, BindableCollection<string> list)
        {
            path = Path.GetFullPath(path);
            foreach (var old in list.Where(p => string.Equals(p, path, StringComparison.OrdinalIgnoreCase)).ToArray()) list.Remove(old);
            list.Insert(0, path);
            while (list.Count > 10) list.RemoveAt(list.Count - 1);
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(HistoryPath));
                File.WriteAllText(HistoryPath, JsonConvert.SerializeObject(new RecentFileHistory
                { Reports = RecentReports.ToList(), Projects = RecentProjects.ToList() }, Formatting.Indented));
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            { SchemeError = "Не удалось сохранить последние файлы: " + ex.Message; }
        }

        private async Task<bool> ConfirmReplaceProject()
        {
            if (_parser == null) return true;
            var answer = MessageBox.Show("Сохранить текущую обработку в файл проекта перед продолжением?", "Проект обработки", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
            if (answer == MessageBoxResult.Cancel) return false;
            if (answer == MessageBoxResult.No) return true;
            var saved = false;
            await SaveProjectToDialog(() => saved = true);
            return saved;
        }

        public override Task<bool> CanCloseAsync(System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) => ConfirmReplaceProject();

        public Task SaveProjectAsync() => SaveProjectToDialog();
        private Task SaveProjectToDialog(System.Action saved = null)
        {
            if (_parser == null) return Task.CompletedTask;
            return _dialogService.SaveFileDialog("Сохранить проект обработки", "Проект обработки (*.json)|*.json", async path =>
            {
                try
                {
                    ApplyCategoryScheme();
                    new ImportProject { ReportPath = _reportPath, SchemeName = HasCategoryScheme ? SelectedCategoryScheme.Name : null,
                        ExportEncoding = SelectedExportEncoding, ShowOriginalCategories = ShowOriginalCategories,
                        ReportIncome = _parser.ReportIncome, ReportExpense = _parser.ReportExpense,
                        Operations = _parser.Operations.ToList(), Mappings = ProjectMappings.ToList() }.Save(path);
                    _projectPath = path;
                    DisplayName = "Проект: " + Path.GetFileName(path);
                    RememberFile(path, RecentProjects);
                    saved?.Invoke();
                }
                catch (Exception ex) { await _dialogService.ShowErrorAsync(ex); }
            });
        }

        public Task OpenProjectAsync() => _dialogService.OpenFileDialog("Открыть проект обработки", "Проект обработки", "*.json", OpenProjectPathAsync);

        public async Task OpenProjectPathAsync(string path)
        {
            try
            {
                var project = ImportProject.Load(path);
                if (!await ConfirmReplaceProject()) return;
                _parser = new ProjectParser(project);
                Operations = new BindableCollection<CardOperation>(_parser.Operations);
                SelectedCategoryScheme = CategorySchemes.FirstOrDefault(s => s != _noScheme && s.Name == project.SchemeName) ?? _noScheme;
                ResetProjectMappings(project.Mappings);
                ShowOriginalCategories = project.ShowOriginalCategories;
                SelectedExportEncoding = project.ExportEncoding == "UTF-8" ? "UTF-8" : "Windows-1251";
                _reportPath = project.ReportPath;
                _projectPath = path;
                DisplayName = "Проект: " + Path.GetFileName(path);
                NotifyProjectLoaded();
                RememberFile(path, RecentProjects);
            }
            catch (Exception ex) { await _dialogService.ShowErrorAsync(ex); }
        }

        public async Task OpenReportPathAsync(string path)
        {
            try
            {
                if (!File.Exists(path)) throw new FileNotFoundException("Отчёт не найден.", path);
                if (!await ConfirmReplaceProject()) return;
                var vm = new EnterCardNameViewModel();
                await _dialogService.ShowAsync(vm);
                if (!vm.IsAccepted) return;
                var parser = ParserCore.Parser.Get(path, App.GetSettingsPath(), ';');
                parser.RunParse(vm.CardName);
                _parser = parser;
                Operations = new BindableCollection<CardOperation>(parser.Operations);
                ResetProjectMappings();
                PopulateCategoryMappings();
                ApplyCategoryScheme();
                _reportPath = path;
                _projectPath = null;
                DisplayName = "Отчёт: " + Path.GetFileName(path);
                NotifyProjectLoaded();
                RememberFile(path, RecentReports);
            }
            catch (Exception ex) { await _dialogService.ShowErrorAsync(ex); }
        }

        private void NotifyProjectLoaded()
        {
            NotifyOfPropertyChange(nameof(Operations));
            NotifyOfPropertyChange(nameof(VisibleOperations));
            NotifyOfPropertyChange(nameof(IncomeSummary));
            NotifyOfPropertyChange(nameof(ExpenseSummary));
            NotifyOfPropertyChange(nameof(SelectedExportEncoding));
        }
    }

    public class RecentFileHistory
    {
        public List<string> Reports { get; set; } = new List<string>();
        public List<string> Projects { get; set; } = new List<string>();
    }
}
