using System;
using System.IO;
using System.Linq;
using Xunit;

namespace ParserCore.Tests
{
    public class ImportProjectTests
    {
        [Fact]
        public void ProjectRoundTripPreservesOriginalCategoriesAndLocalRules()
        {
            var path = Path.GetTempFileName();
            try
            {
                var operation = new CardOperation { RowNumber = 1, Category = "SBOL", Title = "Перевод Ивану", Summ = -250.50m,
                    Date = new DateTime(2026, 10, 31, 15, 20, 0), CardName = "Карта" };
                var local = new CategoryMapping { Source = "SBOL", TitleContains = "Ивану", Target = "Оплата", AddToScheme = false };
                var scheme = new CategoryScheme();
                scheme.Mappings.Add(local);
                operation.ApplyCategoryScheme(scheme);
                var project = new ImportProject { ReportPath = "missing.pdf", ReportIncome = 1000, ReportExpense = 250.50m };
                project.Operations.Add(operation);
                project.Mappings.Add(local);
                project.Save(path);
                var restored = ImportProject.Load(path);
                var parser = new ProjectParser(restored);
                Assert.Equal("SBOL", parser.Operations.Single().OriginalCategory);
                Assert.Equal("Оплата", parser.Operations.Single().Category);
                Assert.Equal(operation.Date, parser.Operations.Single().Date);
                Assert.Equal(-250.50m, parser.Operations.Single().Summ);
                Assert.Equal(250.50m, parser.ReportExpense);
                Assert.False(restored.Mappings.Single().AddToScheme);
                parser.Operations.Single().ApplyCategoryScheme(null);
                Assert.Equal("SBOL", parser.Operations.Single().Category);
            }
            finally { File.Delete(path); }
        }

        [Fact]
        public void TemporaryCategoryIsPerOperationAndSurvivesProjectAndExports()
        {
            var path = Path.GetTempFileName();
            var csv = Path.GetTempFileName();
            var xlsx = Path.GetTempFileName();
            try
            {
                var first = new CardOperation { Category = "Перевод на карту", Title = "SBOL" };
                var second = new CardOperation { Category = "Перевод на карту", Title = "SBOL" };
                var scheme = new CategoryScheme();
                scheme.Mappings.Add(new CategoryMapping { Source = "Перевод на карту", Target = "Пополнение" });
                first.ApplyCategoryScheme(scheme);
                second.ApplyCategoryScheme(scheme);
                first.TemporaryCategory = "Счёт зарплаты";
                Assert.True(first.HasTemporaryCategory);
                second.TemporaryCategory = "Накопительный счёт";
                first.ApplyCategoryScheme(scheme, true);
                Assert.Equal("Перевод на карту", first.DisplayCategory);
                Assert.Equal("Счёт зарплаты", first.Category);
                Assert.Single(scheme.Mappings);
                var project = new ImportProject();
                project.Operations.Add(first);
                project.Operations.Add(second);
                project.Save(path);
                var restored = ImportProject.Load(path);
                var parser = new ProjectParser(restored);
                foreach (var operation in parser.Operations) operation.ApplyCategoryScheme(scheme);
                Assert.Equal("Счёт зарплаты", restored.Operations[0].Category);
                Assert.Equal("Накопительный счёт", restored.Operations[1].Category);
                parser.Save(csv, System.Text.Encoding.UTF8);
                Assert.Contains("Счёт зарплаты", File.ReadAllText(csv));
                Assert.Contains("Накопительный счёт", File.ReadAllText(csv));
                parser.SaveXlsx(xlsx);
                using (var file = File.OpenRead(xlsx))
                using (var archive = new System.IO.Compression.ZipArchive(file))
                using (var sheet = archive.GetEntry("xl/worksheets/sheet1.xml").Open())
                using (var reader = new StreamReader(sheet))
                {
                    var xml = reader.ReadToEnd();
                    Assert.Contains("Счёт зарплаты", xml);
                    Assert.Contains("Накопительный счёт", xml);
                }
                restored.Operations[0].TemporaryCategory = " ";
                Assert.Equal("Пополнение", restored.Operations[0].Category);
                Assert.False(restored.Operations[0].HasTemporaryCategory);
                Assert.Equal("Накопительный счёт", restored.Operations[1].Category);
            }
            finally { File.Delete(path); File.Delete(csv); File.Delete(xlsx); }
        }

        [Theory]
        [InlineData("null")]
        [InlineData("{\"Version\":2}")]
        [InlineData("{\"Operations\":null}")]
        [InlineData("{\"Mappings\":[null]}")]
        public void InvalidProjectIsRejected(string json)
        {
            var path = Path.GetTempFileName();
            try { File.WriteAllText(path, json); Assert.Throws<InvalidDataException>(() => ImportProject.Load(path)); }
            finally { File.Delete(path); }
        }
    }
}
