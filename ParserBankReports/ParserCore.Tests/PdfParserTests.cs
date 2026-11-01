using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Xunit;

namespace ParserCore.Tests
{
    public class PdfParserTests
    {
        [LocalStatementTheory]
        [InlineData("ru-RU")]
        [InlineData("en-US")]
        public void ParsesStatementWithCategoriesDatesLocationsAndMatchingTotals(string cultureName)
        {
            var originalCulture = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);
                var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Files", "4215.pdf");
                var parser = Parser.Get(path, DataSet.LoadDefault(), ';');
                parser.RunParse("Основной счёт");
                var operations = parser.Operations.ToArray();
                Assert.Equal(184, operations.Length);
                Assert.Equal(223048.09m, parser.ReportIncome);
                Assert.Equal(212691.53m, parser.ReportExpense);
                Assert.Equal(parser.ReportIncome, operations.Where(o => o.Summ > 0).Sum(o => o.Summ));
                Assert.Equal(parser.ReportExpense, -operations.Where(o => o.Summ < 0).Sum(o => o.Summ));
                Assert.Equal(Enumerable.Range(1, operations.Length), operations.Select(o => o.RowNumber));
                Assert.All(operations, o =>
                {
                    Assert.Equal("Основной счёт", o.CardName);
                    Assert.NotEqual(default(DateTime), o.ProcessDate);
                    Assert.False(string.IsNullOrWhiteSpace(o.Location));
                    Assert.DoesNotContain("Операция по", o.Location);
                });
                var first = operations[0];
                Assert.Equal("Супермаркеты", first.Category);
                Assert.Equal("SUPERMARKET PRODSIB Berdsk RUS", first.Location);
                Assert.Equal(new DateTime(2026, 10, 30, 17, 43, 0), first.Date);
                Assert.Equal(new DateTime(2026, 10, 30), first.ProcessDate);
                Assert.Equal(-1337.80m, first.Summ);
                Assert.Equal(10983.39m, first.BalanceAfter);
                var delayed = Assert.Single(operations, o => o.Date == new DateTime(2026, 10, 26, 3, 22, 0));
                Assert.Equal(new DateTime(2026, 10, 27), delayed.ProcessDate);
                Assert.Equal("Автомобиль", delayed.Category);
                Assert.Equal("IP Gabelkov Robert Ale BERDSK RUS", delayed.Location);
                Assert.Contains(operations, o => o.Category == "Выдача наличных" && o.Location == "Банкомат №60340902");
                Assert.Contains(operations, o => o.Category == "Все для дома" && o.Summ == -1197m);

                parser.RunParse("Другой счёт");
                Assert.Equal(184, parser.Operations.Count());
                Assert.All(parser.Operations, o => Assert.Equal("Другой счёт", o.CardName));
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
            }
        }
    }

    public sealed class LocalStatementTheoryAttribute : TheoryAttribute
    {
        public LocalStatementTheoryAttribute()
        {
            if (!File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Files", "4215.pdf")))
                Skip = "Для проверки локальной выписки добавьте ParserApp/Files/4215.pdf.";
        }
    }
}
