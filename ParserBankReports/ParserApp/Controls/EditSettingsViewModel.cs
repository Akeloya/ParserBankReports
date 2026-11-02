using Caliburn.Micro;

using ParserApp.Properties;

using ParserCore;

using ParserCore.Services;
using System.Threading.Tasks;

namespace ParserApp.Controls
{
    public class EditSettingsViewModel : Screen
    {
        private readonly IApplicationSettingsService _settingsService;
        public EditSettingsViewModel(IApplicationSettingsService settingsService)
        {
            _settingsService = settingsService;
            DisplayName = Resources.WndTitleEditSettings;
        }

        protected override void OnViewLoaded(object view)
        {
            Data = _settingsService.LoadSettings();
            base.OnViewLoaded(view);
        }

        public IDataSet Data { get; private set; }

        public Task CloseAsync()
        {
            return TryCloseAsync();
        }

        public Task SaveAsync()
        {
            // Schemes may have changed in the main window since this editor was opened.
            var current = _settingsService.LoadSettings();
            Data.CategorySchemes = current.CategorySchemes;
            Data.SelectedCategoryScheme = current.SelectedCategoryScheme;
            _settingsService.SaveSettings(Data);
            return Task.CompletedTask;
        }
    }
}
