using Caliburn.Micro;

using ParserApp.Properties;

using ParserCore;

using System.IO;
using System.Threading.Tasks;

namespace ParserApp.Controls
{
    public class EditSettingsViewModel : Screen
    {
        private readonly string _settingsPath;
        public EditSettingsViewModel()
        {
            _settingsPath = App.GetSettingsPath();
            DisplayName = Resources.WndTitleEditSettings;
        }

        protected override void OnViewLoaded(object view)
        {
            try
            {
                Data = DataSet.LoadSettings(_settingsPath);
            }
            catch (FileNotFoundException)
            {
                Data = DataSet.LoadDefault();
            }
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
            if (File.Exists(_settingsPath))
            {
                var current = DataSet.LoadSettings(_settingsPath);
                Data.CategorySchemes = current.CategorySchemes;
                Data.SelectedCategoryScheme = current.SelectedCategoryScheme;
            }
            return Data.SaveAsync(_settingsPath);
        }
    }
}
