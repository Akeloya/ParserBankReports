using System.Threading.Tasks;

namespace ParserCore
{
    public interface IDataSet
    {
        int EncodingPage { get; set; }
        System.Collections.Generic.List<CategoryScheme> CategorySchemes { get; set; }
        string SelectedCategoryScheme { get; set; }
        string DataXPath { get; set; }
        string RestXPath { get; set; }
        string RootTableXpath { get; set; }
        DataColumn Category { get; set; }
        DataColumn Date { get; set; }
        DataColumn DateProceed { get; set; }
        DataColumn Location { get; set; }
        DataColumn Summ { get; set; }
        DataColumn Title { get; set; }
        DataColumn BalanceArter { get; set; }
        void Save(string jsonPath);
        Task SaveAsync(string jsonPath);
    }
}
