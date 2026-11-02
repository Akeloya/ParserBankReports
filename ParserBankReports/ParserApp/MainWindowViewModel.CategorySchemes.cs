using Caliburn.Micro;
using Newtonsoft.Json;
using ParserApp.Controls;
using ParserCore;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ParserApp
{
    public partial class MainWindowViewModel
    {
        public System.Collections.ObjectModel.ObservableCollection<CategoryMapping> ProjectMappings { get; } = new System.Collections.ObjectModel.ObservableCollection<CategoryMapping>();
        private readonly Dictionary<CategoryMapping, CategoryMapping> _sharedMappings = new Dictionary<CategoryMapping, CategoryMapping>();

        private void ResetProjectMappings(IEnumerable<CategoryMapping> saved = null)
        {
            var wasLoading = _loadingSchemes;
            _loadingSchemes = true;
            ProjectMappings.Clear();
            _sharedMappings.Clear();
            foreach (var item in saved ?? SelectedCategoryScheme?.Mappings ?? Enumerable.Empty<CategoryMapping>())
            {
                var copy = new CategoryMapping { Source = item.Source, Target = item.Target, TitleContains = item.TitleContains,
                    AddToScheme = saved == null || item.AddToScheme };
                ProjectMappings.Add(copy);
                if (copy.AddToScheme && HasCategoryScheme)
                {
                    var shared = SelectedCategoryScheme.Mappings.FirstOrDefault(m => m.Source == copy.Source && m.TitleContains == copy.TitleContains && m.Target == copy.Target);
                    if (shared != null) _sharedMappings[copy] = shared;
                }
            }
            _loadingSchemes = wasLoading;
            ObserveMappings();
            ClearCategoryFilter();
            ApplyCategoryScheme();
        }

        private void SyncSharedMapping(CategoryMapping mapping)
        {
            if (_loadingSchemes || !HasCategoryScheme) return;
            if (!mapping.AddToScheme)
            {
                // Unchecking keeps the edited rule local; existing shared rules remain available to other projects.

                return;
            }
            if (!_sharedMappings.TryGetValue(mapping, out var shared))
            {
                shared = new CategoryMapping();
                _sharedMappings[mapping] = shared;
                SelectedCategoryScheme.Mappings.Add(shared);
            }
            shared.Source = mapping.Source;
            shared.TitleContains = mapping.TitleContains;
            shared.Target = mapping.Target;
            SaveCategorySchemes();
        }

        private readonly CategoryScheme _noScheme = new CategoryScheme { Name = "Без схемы" };
        private readonly HashSet<CategoryMapping> _observedMappings = new HashSet<CategoryMapping>();
        private CategoryScheme _selectedCategoryScheme;
        private bool _showOriginalCategories;
        private bool _loadingSchemes;
        private bool _canPersistSchemes = true;
        private string _schemeError;
        private CategoryMapping _selectedCategoryMapping;

        public CategoryMapping SelectedCategoryMapping
        {
            get => _selectedCategoryMapping;
            set
            {
                if (ReferenceEquals(_selectedCategoryMapping, value)) return;
                _selectedCategoryMapping = value;
                NotifyOfPropertyChange();
                RefreshOperationsView();
                NotifyOfPropertyChange(nameof(CategoryFilterStatus));
            }
        }

        private System.Windows.Data.ListCollectionView _operationsView;
        private object _operationsViewSource;
        public System.ComponentModel.ICollectionView VisibleOperations
        {
            get
            {
                if (Operations == null) return null;
                if (!ReferenceEquals(_operationsViewSource, Operations))
                {
                    _operationsViewSource = Operations;
                    _operationsView = new System.Windows.Data.ListCollectionView(Operations);
                    _operationsView.Filter = item => item is CardOperation operation &&
                        (SelectedCategoryMapping == null || SelectedCategoryMapping.Matches(operation.SourceCategory, operation.Title));
                    _operationsView.SortDescriptions.Add(new SortDescription(nameof(CardOperation.SourceCategory), ListSortDirection.Ascending));
                }
                return _operationsView;
            }
        }

        private void RefreshOperationsView()
        {
            VisibleOperations?.Refresh();
            NotifyOfPropertyChange(nameof(VisibleOperations));
        }

        public async Task SetTemporaryCategoryAsync(CardOperation operation)
        {
            if (operation == null) return;
            var dialog = new TemporaryCategoryViewModel(operation.TemporaryCategory ?? operation.Category);
            await _dialogService.ShowAsync(dialog);
            if (!dialog.IsAccepted) return;
            operation.TemporaryCategory = dialog.Category.Trim();
            RefreshOperationsView();
        }

        public void ClearTemporaryCategory(CardOperation operation)
        {
            if (operation == null) return;
            operation.TemporaryCategory = null;
            ApplyCategoryScheme();
        }

        public void ClearAllTemporaryCategories()
        {
            var operations = Operations?.Where(o => o.HasTemporaryCategory).ToArray();
            var count = operations?.Length ?? 0;
            if (count == 0) return;
            if (System.Windows.MessageBox.Show($"В проекте используется {count} временных категорий. Очистить все?",
                "Очистка временных категорий", System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question, System.Windows.MessageBoxResult.No) != System.Windows.MessageBoxResult.Yes)
                return;
            foreach (var operation in operations) operation.TemporaryCategory = null;
            ApplyCategoryScheme();
        }

        public CategoryMapping FindCategoryMapping(CardOperation operation)
        {
            if (operation == null) return null;
            return ProjectMappings.Where(mapping => mapping.Matches(operation.SourceCategory, operation.Title))
                .OrderBy(mapping => string.IsNullOrWhiteSpace(mapping.TitleContains) ? 0 :
                    string.IsNullOrWhiteSpace(mapping.Source) ? 1 : 2)
                .LastOrDefault();
        }
        public string CategoryFilterStatus => SelectedCategoryMapping == null
            ? "Показаны все операции"
            : "Показаны операции выбранного соответствия";

        public void ClearCategoryFilter() => SelectedCategoryMapping = null;

        public void DeleteCategoryMapping(CategoryMapping mapping)
        {
            if (mapping == null)
                return;
            if (mapping.AddToScheme && _sharedMappings.TryGetValue(mapping, out var shared) && HasCategoryScheme)
                SelectedCategoryScheme.Mappings.Remove(shared);
            _sharedMappings.Remove(mapping);
            ProjectMappings.Remove(mapping);
        }

        public Task<CategoryMapping> AddCategoryMappingForOperation(CardOperation operation)
        {
            if (operation == null)
                return Task.FromResult<CategoryMapping>(null);


            var mapping = new CategoryMapping
            {
                Source = operation.OriginalCategory ?? operation.Category,
                TitleContains = operation.Title
            };
            ProjectMappings.Add(mapping);
            SelectedCategoryMapping = mapping;
            return Task.FromResult(mapping);
        }

        public BindableCollection<CategoryScheme> CategorySchemes { get; } = new BindableCollection<CategoryScheme>();
        public CategoryScheme SelectedCategoryScheme
        {
            get => _selectedCategoryScheme;
            set
            {
                if (_selectedCategoryScheme == value)
                    return;
                if (_selectedCategoryScheme != null)
                    ProjectMappings.CollectionChanged -= MappingsChanged;
                foreach (var mapping in _observedMappings)
                    mapping.PropertyChanged -= MappingChanged;
                _observedMappings.Clear();
                var localRules = ProjectMappings.Where(m => !m.AddToScheme).ToArray();
                _selectedCategoryScheme = value;
                ClearCategoryFilter();
                if (value != null)
                    ProjectMappings.CollectionChanged += MappingsChanged;
                ResetProjectMappings();
                foreach (var localRule in localRules) ProjectMappings.Add(localRule);
                ObserveMappings();
                PopulateCategoryMappings();
                ApplyCategoryScheme();
                NotifyOfPropertyChange();
                NotifyOfPropertyChange(nameof(HasCategoryScheme));
                SaveCategorySchemes();
            }
        }
        public bool HasCategoryScheme => SelectedCategoryScheme != null && SelectedCategoryScheme != _noScheme;
        public bool ShowOriginalCategories
        {
            get => _showOriginalCategories;
            set
            {
                _showOriginalCategories = value;
                ApplyCategoryScheme();
                NotifyOfPropertyChange();
            }
        }
        public string SchemeError
        {
            get => _schemeError;
            private set { _schemeError = value; NotifyOfPropertyChange(); }
        }

        private void LoadCategorySchemes()
        {
            _loadingSchemes = true;
            CategorySchemes.Add(_noScheme);
            try
            {
                var settings = _settingsService.LoadSettings();
                foreach (var scheme in settings.CategorySchemes ?? new List<CategoryScheme>())
                {
                    if (scheme == null || string.IsNullOrWhiteSpace(scheme.Name))
                        continue;
                    scheme.Mappings = scheme.Mappings ?? new System.Collections.ObjectModel.ObservableCollection<CategoryMapping>();
                    CategorySchemes.Add(scheme);
                }
                SelectedCategoryScheme = CategorySchemes.FirstOrDefault(s => s != _noScheme && s.Name == settings.SelectedCategoryScheme) ?? _noScheme;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is JsonException)
            {
                _canPersistSchemes = false;
                SchemeError = "Не удалось загрузить схемы: " + ex.Message;
                SelectedCategoryScheme = _noScheme;
            }
            finally { _loadingSchemes = false; }
        }

        public async Task AddCategoryScheme()
        {
            var dialog = new EnterSchemeNameViewModel(CategorySchemes.Select(s => s.Name));
            await _dialogService.ShowAsync(dialog);
            if (!dialog.IsAccepted)
                return;
            var scheme = new CategoryScheme { Name = dialog.SchemeName.Trim() };
            CategorySchemes.Add(scheme);
            SelectedCategoryScheme = scheme;
        }

        private void PopulateCategoryMappings()
        {
            if (Operations == null)
                return;
            var wasLoading = _loadingSchemes;
            _loadingSchemes = true;
            try
            {
                foreach (var category in Operations.Select(o => o.OriginalCategory ?? o.Category)
                    .Where(c => !string.IsNullOrWhiteSpace(c)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(c => c))
                {
                    if (!ProjectMappings.Any(m => string.Equals(m.Source?.Trim(), category.Trim(), StringComparison.OrdinalIgnoreCase)))
                        ProjectMappings.Add(new CategoryMapping { Source = category });
                }
            }
            finally { _loadingSchemes = wasLoading; }
            SaveCategorySchemes();
        }

        private void ObserveMappings()
        {
            var mappings = ProjectMappings;
            foreach (var removed in _observedMappings.Where(m => mappings == null || !mappings.Contains(m)).ToArray())
            {
                removed.PropertyChanged -= MappingChanged;
                _observedMappings.Remove(removed);
            }
            if (mappings == null)
                return;
            foreach (var mapping in mappings)
                if (_observedMappings.Add(mapping))
                    mapping.PropertyChanged += MappingChanged;
        }

        private void MappingsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (!_loadingSchemes && e.OldItems != null)
                foreach (CategoryMapping removed in e.OldItems)
                {
                    if (removed.AddToScheme && HasCategoryScheme && _sharedMappings.TryGetValue(removed, out var shared))
                        SelectedCategoryScheme.Mappings.Remove(shared);
                    _sharedMappings.Remove(removed);
                }
            ObserveMappings();
            if (SelectedCategoryMapping != null && !ProjectMappings.Contains(SelectedCategoryMapping))
                ClearCategoryFilter();
            if (_loadingSchemes)
                return;
            ApplyCategoryScheme();
            SaveCategorySchemes();
        }
        private void MappingChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender is CategoryMapping mapping) SyncSharedMapping(mapping);
            ApplyCategoryScheme();
            SaveCategorySchemes();
        }
        private void ApplyCategoryScheme()
        {
            if (Operations == null)
                return;
            foreach (var operation in Operations)
                operation.ApplyCategoryScheme(new CategoryScheme { Mappings = ProjectMappings }, ShowOriginalCategories);
            RefreshOperationsView();
        }
        private void SaveCategorySchemes()
        {
            if (_loadingSchemes || !_canPersistSchemes)
                return;
            try
            {
                var settings = _settingsService.LoadSettings();
                settings.CategorySchemes = CategorySchemes.Where(s => s != _noScheme).ToList();
                settings.SelectedCategoryScheme = HasCategoryScheme ? SelectedCategoryScheme.Name : null;
                _settingsService.SaveSettings(settings);
                SchemeError = null;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is JsonException)
            {
                SchemeError = "Не удалось сохранить схемы: " + ex.Message;
            }
        }
    }
}
