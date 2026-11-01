using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ParserApp.Controls
{
    public class EnterSchemeNameViewModel : Screen
    {
        private readonly IEnumerable<string> _existingNames;
        private string _schemeName;
        public EnterSchemeNameViewModel(IEnumerable<string> existingNames)
        {
            _existingNames = existingNames.ToArray();
            DisplayName = "Новая схема категорий";
        }
        public string SchemeName
        {
            get => _schemeName;
            set
            {
                _schemeName = value;
                NotifyOfPropertyChange();
                NotifyOfPropertyChange(nameof(ValidationMessage));
                NotifyOfPropertyChange(nameof(CanApply));
            }
        }
        public string ValidationMessage => string.IsNullOrWhiteSpace(SchemeName) ? "Введите название схемы" :
            _existingNames.Any(n => string.Equals(n, SchemeName.Trim(), StringComparison.OrdinalIgnoreCase))
                ? "Схема с таким названием уже существует" : null;
        public bool CanApply => ValidationMessage == null;
        public bool IsAccepted { get; private set; }
        public Task Apply()
        {
            if (!CanApply)
                return Task.CompletedTask;
            IsAccepted = true;
            return TryCloseAsync(true);
        }
        public Task Cancel() => TryCloseAsync(false);
    }
}
