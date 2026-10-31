using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace ParserCore.Tests
{
    public class CategorySchemeTests
    {
        [Fact]
        public void TitleRulesSplitCategoriesAndOverrideIncorrectBankCategories()
        {
            var scheme = new CategoryScheme();
            var personal = new CategoryMapping { Source = "Перевод СБП", TitleContains = " иван ", Target = "Частным лицам" };
            scheme.Mappings.Add(personal);
            scheme.Mappings.Add(new CategoryMapping { Source = "Перевод СБП", TitleContains = "банк", Target = "Между счетами" });
            scheme.Mappings.Add(new CategoryMapping { TitleContains = "магазин", Target = "Покупки" });
            scheme.Mappings.Add(new CategoryMapping { Source = "Перевод СБП", Target = "Прочие переводы" });
            Assert.Equal("Частным лицам", scheme.Map(" перевод сбп ", "ИВАН Петров"));
            Assert.Equal("Между счетами", scheme.Map("Перевод СБП", "Другой банк"));
            Assert.Equal("Прочие переводы", scheme.Map("Перевод СБП", "Неизвестный получатель"));
            Assert.Equal("Прочие переводы", scheme.Map("Перевод СБП", null));
            Assert.Equal("Покупки", scheme.Map("Ошибочная категория", "Магазин продуктов"));
            Assert.Equal("Без изменений", scheme.Map("Без изменений", null));
            Assert.False(personal.Matches("Перевод СБП", "Другой банк"));
            Assert.False(personal.Matches("Другая категория", "Иван"));
            scheme.Mappings.Add(new CategoryMapping { TitleContains = "иван", Target = "Общее правило" });
            Assert.Equal("Частным лицам", scheme.Map("Перевод СБП", "Иван"));
            scheme.Mappings.Add(new CategoryMapping { Source = "Перевод СБП", TitleContains = "иван", Target = "Новое правило" });
            Assert.Equal("Новое правило", scheme.Map("Перевод СБП", "Иван"));
            scheme.Mappings.Add(new CategoryMapping { Source = "Перевод СБП", TitleContains = "иван", Target = " " });
            var operation = new CardOperation { Category = "Перевод СБП", Title = "Иван" };
            operation.ApplyCategoryScheme(scheme);
            Assert.Equal("Новое правило", operation.Category);
            operation.ApplyCategoryScheme(null);
            Assert.Equal("Перевод СБП", operation.Category);
        }

        [Fact]
        public void TitleConditionsSurviveSettingsRoundTrip()
        {
            var path = Path.GetTempFileName();
            try
            {
                var settings = DataSet.LoadDefault();
                var scheme = new CategoryScheme { Name = "Правила" };
                scheme.Mappings.Add(new CategoryMapping { TitleContains = "магазин", Target = "Покупки" });
                settings.CategorySchemes.Add(scheme);
                settings.Save(path);
                var loaded = DataSet.LoadSettings(path).CategorySchemes.Single();
                Assert.Equal("магазин", loaded.Mappings.Single().TitleContains);
                Assert.Equal("Покупки", loaded.Map("Прочее", "МАГАЗИН"));
            }
            finally { File.Delete(path); }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ChangingSchemesAlwaysUsesOriginalCategoryAndDisplayToggleDoesNotChangeExport(bool asynchronous)
        {
            var first = new CategoryScheme { Name = "Семья" };
            first.Mappings.Add(new CategoryMapping { Source = " супермаркеты ", Target = " Продукты " });
            var second = new CategoryScheme { Name = "Работа" };
            second.Mappings.Add(new CategoryMapping { Source = "Супермаркеты", Target = "Питание" });
            second.Mappings.Add(new CategoryMapping { Source = "Продукты", Target = "Неверная повторная замена" });
            var parser = new ExportParser();
            var operation = parser.Operations.Single();
            operation.ApplyCategoryScheme(first);
            Assert.Equal("Продукты", operation.DisplayCategory);
            Assert.True(operation.IsCategoryMapped);
            operation.ApplyCategoryScheme(second, showOriginal: true);
            Assert.Equal("Супермаркеты", operation.DisplayCategory);
            Assert.Equal("Питание", operation.Category);
            Assert.True(operation.IsCategoryMapped);
            var path = Path.GetTempFileName();
            try
            {
                if (asynchronous)
                    await parser.SaveAsync(path, new UTF8Encoding(false));
                else
                    parser.Save(path, new UTF8Encoding(false));
                Assert.Equal("Питание", File.ReadAllLines(path)[1].Split(';')[3]);
                second.Mappings[0].Target = "Продукты; супермаркеты";
                operation.ApplyCategoryScheme(second, showOriginal: true);
                await parser.SaveAsync(path, new UTF8Encoding(false));
                Assert.Contains(";\"Продукты; супермаркеты\";", File.ReadAllLines(path)[1]);
            }
            finally { File.Delete(path); }
            operation.ApplyCategoryScheme(null);
            Assert.Equal("Супермаркеты", operation.Category);
            Assert.False(operation.IsCategoryMapped);
            Assert.Equal("Прочее", first.Map("Прочее"));
            first.Mappings[0].Target = " ";
            Assert.Equal("Супермаркеты", first.Map("Супермаркеты"));
        }

        [Fact]
        public void CategoryFilterUsesSourceEvenWhenSeveralCategoriesHaveSameReplacement()
        {
            var scheme = new CategoryScheme { Name = "Семья" };
            var groceries = new CategoryMapping { Source = "Супермаркеты", Target = "Питание" };
            scheme.Mappings.Add(groceries);
            scheme.Mappings.Add(new CategoryMapping { Source = "Рестораны и кафе", Target = "Питание" });
            var operations = new[]
            {
                new CardOperation { Category = "Супермаркеты" },
                new CardOperation { Category = "Рестораны и кафе" }
            };
            foreach (var operation in operations)
                operation.ApplyCategoryScheme(scheme);
            Assert.All(operations, operation => Assert.Equal("Питание", operation.Category));
            var filtered = operations.Where(operation => groceries.Matches(operation.OriginalCategory ?? operation.Category));
            Assert.Same(operations[0], Assert.Single(filtered));
            Assert.True(groceries.Matches(" супермаркеты "));
            Assert.False(groceries.Matches(null));
        }

        [Fact]
        public void SettingsRoundTripMultipleSchemesAndRemainCompatibleWithOldFiles()
        {
            var path = Path.GetTempFileName();
            try
            {
                File.WriteAllText(path, "{\"EncodingPage\":65001}");
                Assert.Empty(DataSet.LoadSettings(path).CategorySchemes);
                var settings = DataSet.LoadDefault();
                for (var index = 0; index < 100; index++)
                {
                    var scheme = new CategoryScheme { Name = "Схема " + index };
                    scheme.Mappings.Add(new CategoryMapping { Source = "Супермаркеты", Target = "Продукты " + index });
                    settings.CategorySchemes.Add(scheme);
                }
                settings.SelectedCategoryScheme = "Схема 99";
                settings.Save(path);
                var loaded = DataSet.LoadSettings(path);
                Assert.Equal(100, loaded.CategorySchemes.Count);
                Assert.Equal("Схема 99", loaded.SelectedCategoryScheme);
                Assert.Equal("Продукты 99", loaded.CategorySchemes[99].Map("Супермаркеты"));
                Assert.Equal(settings.Date.Name, loaded.Date.Name);
                Assert.Equal(settings.DataXPath, loaded.DataXPath);
                loaded.Save(path);
                Assert.Equal(100, DataSet.LoadSettings(path).CategorySchemes.Count);
            }
            finally { File.Delete(path); }
        }

        private sealed class ExportParser : Parser
        {
            public ExportParser() : base(string.Empty, DataSet.LoadDefault(), ';')
            {
                _operations.Add(new CardOperation { RowNumber = 1, Category = "Супермаркеты" });
            }
            public override void RunParse(string cardName = null) { }
        }
    }
}
