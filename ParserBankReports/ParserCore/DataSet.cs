using Newtonsoft.Json;
using ParserCore.Properties;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace ParserCore
{
    public class DataSet : IDataSet
    {
        public int EncodingPage { get; set; }
        public System.Collections.Generic.List<CategoryScheme> CategorySchemes { get; set; } = new System.Collections.Generic.List<CategoryScheme>();
        public string SelectedCategoryScheme { get; set; }
        public string DataXPath { get; set; }
        public string RestXPath { get; set; }
        public string RootTableXpath { get; set; }
        public DataColumn Category { get; set; }
        public DataColumn Date { get; set; }
        public DataColumn DateProceed { get; set; }
        public DataColumn Location { get; set; }
        public DataColumn Summ { get; set; }
        public DataColumn Title { get; set; }
        public DataColumn BalanceArter { get; set; }

        public static IDataSet LoadDefault()
        {
            return new DataSet
            {
                EncodingPage = Encoding.UTF8.CodePage,
                DataXPath = "//div[contains(@class,'trs_it')]",
                RootTableXpath = "//div[contains(@class,'b-trs')]",
                RestXPath = "//*[contains(@class,'state_list')]/li[1]/div[2]",
                Title = new DataColumn("Название операции", ".//div/div[1]", DataContainerType.InnerText),
                Category = new DataColumn("Категория", ".//*[contains(@class,'icat')]", DataContainerType.InnerText),
                Date = new DataColumn("Дата операции", ".//div[contains(@class,'trs_date')]/span", DataContainerType.Attribute, "data-date"),
                Summ = new DataColumn("Сумма", ".//div[contains(@class,'trs_sum')]", DataContainerType.InnerText),
                BalanceArter = new DataColumn("Остаток на карте", null, DataContainerType.InnerText),
                Location = new DataColumn("Место", "//span[contains(@class,'trs_country')]", DataContainerType.InnerText),
                DateProceed = new DataColumn("Дата проведения", "//span[contains(@class,'idate')]", DataContainerType.Attribute, "data-date")
            };
        }
        public static IDataSet LoadSettings(string jsonPath)
        {
            if (!File.Exists(jsonPath))
                throw new FileNotFoundException(Resource.SettingsFileNotFoundExceptionMessage, jsonPath);
            var json = File.ReadAllText(jsonPath);
            var obj = JsonConvert.DeserializeObject<DataSet>(json);
            return obj;
        }

        public static Task<IDataSet> LoadAsync(string jsonPath)
        {
            return Task.Run(() => LoadSettings(jsonPath));
        }

        public void Save(string jsonPath)
        {
            var json = JsonConvert.SerializeObject(this);
            var temporaryPath = jsonPath + ".tmp";
            File.WriteAllText(temporaryPath, json);
            if (File.Exists(jsonPath))
                File.Replace(temporaryPath, jsonPath, jsonPath + ".bak");
            else
                File.Move(temporaryPath, jsonPath);
        }

        public Task SaveAsync(string jsonPath)
        {
            return Task.Run(()=> Save(jsonPath));
        }
    }
}
