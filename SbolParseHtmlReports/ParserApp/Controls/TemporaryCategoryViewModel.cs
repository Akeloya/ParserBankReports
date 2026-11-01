using Caliburn.Micro;
using System.Threading.Tasks;

namespace ParserApp.Controls
{
    public class TemporaryCategoryViewModel : Screen
    {
        private string _category;
        public TemporaryCategoryViewModel(string category)
        {
            DisplayName = "Временная категория";
            Category = category;
        }
        public string Category
        {
            get => _category;
            set { _category = value; NotifyOfPropertyChange(); NotifyOfPropertyChange(nameof(CanApply)); }
        }
        public bool CanApply => !string.IsNullOrWhiteSpace(Category);
        public bool IsAccepted { get; private set; }
        public Task Apply()
        {
            if (!CanApply) return Task.CompletedTask;
            IsAccepted = true;
            return TryCloseAsync(true);
        }
        public Task Cancel() => TryCloseAsync(false);
    }
}
