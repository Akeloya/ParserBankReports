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
                _selectedCategoryMapping = value;
                NotifyOfPropertyChange();
                NotifyOfPropertyChange(nameof(VisibleOperations));
            }
        }

        public IEnumerable<CardOperation> VisibleOperations => Operations == null
            ? Enumerable.Empty<CardOperation>()
            : Operations.Where(operation => SelectedCategoryMapping == null ||
                SelectedCategoryMapping.Matches(operation.OriginalCategory ?? operation.Category));

        public void ClearCategoryFilter() => SelectedCategoryMapping = null;

        public BindableCollection<CategoryScheme> CategorySchemes { get; } = new BindableCollection<CategoryScheme>();
        public CategoryScheme SelectedCategoryScheme
        {
            get => _selectedCategoryScheme;
            set
            {
                if (_selectedCategoryScheme == value)
                    return;
                if (_selectedCategoryScheme != null)
                    _selectedCategoryScheme.Mappings.CollectionChanged -= MappingsChanged;
                foreach (var mapping in _observedMappings)
                    mapping.PropertyChanged -= MappingChanged;
                _observedMappings.Clear();
                _selectedCategoryScheme = value;
                ClearCategoryFilter();
                if (value != null)
                    value.Mappings.CollectionChanged += MappingsChanged;
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
                var settings = File.Exists(App.GetSettingsPath()) ? DataSet.LoadSettings(App.GetSettingsPath()) : DataSet.LoadDefault();
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
            if (!HasCategoryScheme || Operations == null)
                return;
            var wasLoading = _loadingSchemes;
            _loadingSchemes = true;
            try
            {
                foreach (var category in Operations.Select(o => o.OriginalCategory ?? o.Category)
                    .Where(c => !string.IsNullOrWhiteSpace(c)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(c => c))
                {
                    if (!SelectedCategoryScheme.Mappings.Any(m => string.Equals(m.Source?.Trim(), category.Trim(), StringComparison.OrdinalIgnoreCase)))
                        SelectedCategoryScheme.Mappings.Add(new CategoryMapping { Source = category });
                }
            }
            finally { _loadingSchemes = wasLoading; }
            SaveCategorySchemes();
        }

        private void ObserveMappings()
        {
            var mappings = SelectedCategoryScheme?.Mappings;
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
            ObserveMappings();
            if (SelectedCategoryMapping != null && !SelectedCategoryScheme.Mappings.Contains(SelectedCategoryMapping))
                ClearCategoryFilter();
            if (_loadingSchemes)
                return;
            ApplyCategoryScheme();
            SaveCategorySchemes();
        }
        private void MappingChanged(object sender, PropertyChangedEventArgs e)
        {
            ApplyCategoryScheme();
            SaveCategorySchemes();
        }
        private void ApplyCategoryScheme()
        {
            if (Operations == null)
                return;
            foreach (var operation in Operations)
                operation.ApplyCategoryScheme(HasCategoryScheme ? SelectedCategoryScheme : null, ShowOriginalCategories);
            NotifyOfPropertyChange(nameof(VisibleOperations));
        }
        private void SaveCategorySchemes()
        {
            if (_loadingSchemes || !_canPersistSchemes)
                return;
            try
            {
                var path = App.GetSettingsPath();
                var settings = File.Exists(path) ? DataSet.LoadSettings(path) : DataSet.LoadDefault();
                settings.CategorySchemes = CategorySchemes.Where(s => s != _noScheme).ToList();
                settings.SelectedCategoryScheme = HasCategoryScheme ? SelectedCategoryScheme.Name : null;
                settings.Save(path);
                SchemeError = null;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is JsonException)
            {
                SchemeError = "Не удалось сохранить схемы: " + ex.Message;
            }
        }
    }
}
