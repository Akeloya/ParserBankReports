using Caliburn.Micro;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ParserApp.Controls
{
    internal class EnterCardNameViewModel : Screen
    {
        private readonly string _historyPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SberParseHtmlReports", "card-names.json");
        private string _cardName;
        private string _errorMessage;

        public EnterCardNameViewModel()
        {
            DisplayName = Properties.Resources.Card;
            try
            {
                if (File.Exists(_historyPath))
                {
                    var names = JsonConvert.DeserializeObject<string[]>(File.ReadAllText(_historyPath));
                    CardNames.AddRange((names ?? new string[0])
                        .Where(name => !string.IsNullOrWhiteSpace(name))
                        .Select(name => name.Trim())
                        .Distinct(StringComparer.CurrentCultureIgnoreCase)
                        .OrderBy(name => name, StringComparer.CurrentCultureIgnoreCase));
                }
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is JsonException)
            {
                ErrorMessage = "Не удалось загрузить сохранённые имена карт: " + ex.Message;
            }
        }

        public BindableCollection<string> CardNames { get; } = new BindableCollection<string>();
        public bool IsAccepted { get; private set; }
        public string ErrorMessage
        {
            get => _errorMessage;
            private set
            {
                _errorMessage = value;
                NotifyOfPropertyChange();
            }
        }
        public string CardName
        {
            get => _cardName;
            set
            {
                _cardName = value;
                NotifyOfPropertyChange();
                NotifyOfPropertyChange(nameof(CanApply));
            }
        }
        public bool CanApply => !string.IsNullOrWhiteSpace(CardName);

        public async Task Apply()
        {
            if (!CanApply)
                return;

            CardName = CardName.Trim();
            try
            {
                var names = CardNames.Concat(new[] { CardName })
                    .Distinct(StringComparer.CurrentCultureIgnoreCase)
                    .OrderBy(name => name, StringComparer.CurrentCultureIgnoreCase);
                Directory.CreateDirectory(Path.GetDirectoryName(_historyPath));
                var temporaryPath = _historyPath + ".tmp";
                File.WriteAllText(temporaryPath, JsonConvert.SerializeObject(names, Formatting.Indented));
                if (File.Exists(_historyPath))
                    File.Replace(temporaryPath, _historyPath, null);
                else
                    File.Move(temporaryPath, _historyPath);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                ErrorMessage = "Не удалось сохранить имя карты: " + ex.Message;
                return;
            }
            IsAccepted = true;
            await TryCloseAsync(true);
        }

        public Task Cancel()
        {
            return TryCloseAsync(false);
        }
    }
}
