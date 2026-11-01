using System;

namespace ParserCore
{
    public record CardOperation : System.ComponentModel.INotifyPropertyChanged
    {
        public int RowNumber { get; set; }
        public string Title { get; set; }
        public DateTime Date { get; set; }
        public decimal Summ { get; set; } 
        public string Location { get; set; }
        public DateTime ProcessDate { get; set; }
        public decimal BalanceAfter { get; set; }
        public string Category { get; set; }
        public string CardName { get; set; }
        [Newtonsoft.Json.JsonProperty]
        public string OriginalCategory { get; private set; }
        [Newtonsoft.Json.JsonIgnore]
        public string SourceCategory => OriginalCategory ?? Category;
        [Newtonsoft.Json.JsonIgnore]
        public bool IsCategoryMapped => OriginalCategory != null && Category != OriginalCategory;
        private bool _showOriginalCategory;
        [Newtonsoft.Json.JsonIgnore]
        public string DisplayCategory => _showOriginalCategory ? OriginalCategory ?? Category : Category;

        [Newtonsoft.Json.JsonIgnore]
        public bool HasTemporaryCategory => !string.IsNullOrWhiteSpace(TemporaryCategory);
        private string _temporaryCategory;
        private string _mappedCategory;
        public string TemporaryCategory
        {
            get => _temporaryCategory;
            set
            {
                if (_temporaryCategory == value) return;
                OriginalCategory ??= Category;
                _temporaryCategory = value;
                Category = string.IsNullOrWhiteSpace(value) ? _mappedCategory ?? OriginalCategory : value.Trim();
                PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(TemporaryCategory)));
                PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(HasTemporaryCategory)));
                NotifyCategoryChanged();
            }
        }

        private void NotifyCategoryChanged()
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(Category)));
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(DisplayCategory)));
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(IsCategoryMapped)));
        }

        public void ApplyCategoryScheme(CategoryScheme scheme, bool showOriginal = false)
        {
            OriginalCategory ??= Category;
            _mappedCategory = scheme?.Map(OriginalCategory, Title) ?? OriginalCategory;
            Category = string.IsNullOrWhiteSpace(TemporaryCategory) ? _mappedCategory : TemporaryCategory.Trim();
            _showOriginalCategory = showOriginal;
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(Category)));
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(DisplayCategory)));
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(IsCategoryMapped)));
        }

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
    }
}
