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

        public string Map(string category)
        {
            var mapping = Mappings.LastOrDefault(item =>
                !string.IsNullOrWhiteSpace(item.Target) && item.Matches(category));
            return mapping?.Target.Trim() ?? category;
        }
    }

    public class CategoryMapping : INotifyPropertyChanged
    {
        private string _source;
        private string _target;
        public bool Matches(string category) => !string.IsNullOrWhiteSpace(Source) &&
            string.Equals(Source.Trim(), category?.Trim(), StringComparison.OrdinalIgnoreCase);
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
