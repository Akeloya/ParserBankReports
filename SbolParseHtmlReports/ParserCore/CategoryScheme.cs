using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace ParserCore
{
    public class CategoryScheme
    {
        public string Name { get; set; }
        public ObservableCollection<CategoryMapping> Mappings { get; set; } = new ObservableCollection<CategoryMapping>();

        public string Map(string category, string title = null)
        {
            var mapping = Mappings.Where(item =>
                !string.IsNullOrWhiteSpace(item.Target) && item.Matches(category, title))
                .OrderBy(item => string.IsNullOrWhiteSpace(item.TitleContains) ? 0 :
                    string.IsNullOrWhiteSpace(item.Source) ? 1 : 2)
                .LastOrDefault();
            return mapping?.Target.Trim() ?? category;
        }
    }

    public class CategoryMapping : INotifyPropertyChanged
    {
        private string _source;
        private string _target;
        private string _titleContains;
        public bool Matches(string category) => !string.IsNullOrWhiteSpace(Source) &&
            string.Equals(Source.Trim(), category?.Trim(), StringComparison.OrdinalIgnoreCase);
        public bool Matches(string category, string title)
        {
            var hasTitleCondition = !string.IsNullOrWhiteSpace(TitleContains);
            return (Matches(category) || (hasTitleCondition && string.IsNullOrWhiteSpace(Source))) &&
                (!hasTitleCondition || (title?.IndexOf(TitleContains.Trim(), StringComparison.OrdinalIgnoreCase) ?? -1) >= 0);
        }
        public string TitleContains
        {
            get => _titleContains;
            set { _titleContains = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TitleContains))); }
        }
        public string Source
        {
            get => _source;
            set { _source = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Source))); }
        }
        public string Target
        {
            get => _target;
            set { _target = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Target))); }
        }
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
