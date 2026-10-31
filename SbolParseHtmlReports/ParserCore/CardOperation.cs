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
        public string OriginalCategory { get; private set; }
        public bool IsCategoryMapped => OriginalCategory != null && Category != OriginalCategory;
        private bool _showOriginalCategory;
        public string DisplayCategory => _showOriginalCategory ? OriginalCategory ?? Category : Category;

        public void ApplyCategoryScheme(CategoryScheme scheme, bool showOriginal = false)
        {
            OriginalCategory ??= Category;
            Category = scheme?.Map(OriginalCategory) ?? OriginalCategory;
            _showOriginalCategory = showOriginal;
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(Category)));
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(DisplayCategory)));
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(IsCategoryMapped)));
        }

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
    }
}
