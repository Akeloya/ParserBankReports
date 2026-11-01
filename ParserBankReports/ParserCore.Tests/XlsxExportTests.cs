using System;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Xunit;

namespace ParserCore.Tests
{
    public class XlsxExportTests
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ExportsTypedCellsAndMappedCategories(bool asynchronous)
        {
            var path = Path.GetTempFileName();
            try
            {
                var parser = new ExportParser();
                File.WriteAllText(path, new string('x', 10000));
                if (asynchronous)
                    await parser.SaveXlsxAsync(path);
                else
                    parser.SaveXlsx(path);
                using (var file = File.OpenRead(path))
                using (var archive = new ZipArchive(file))
                {
                Assert.Equal(6, archive.Entries.Count);
                foreach (var entry in archive.Entries)
                {
                    using (var stream = entry.Open())
                    XDocument.Load(stream);
                }
                using (var sheet = archive.GetEntry("xl/worksheets/sheet1.xml").Open())
                {
                XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
                var rows = XDocument.Load(sheet).Descendants(ns + "row").ToArray();
                Assert.Equal(2, rows.Length);
                Assert.Equal(11, rows[0].Elements().Count());
                var cells = rows[1].Elements().ToArray();
                Assert.Equal("1", cells[0].Element(ns + "v").Value);
                Assert.Equal(new DateTime(2026, 10, 31, 12, 30, 0).ToOADate(), double.Parse(cells[1].Value, CultureInfo.InvariantCulture));
                Assert.Equal("1", cells[1].Attribute("s").Value);
                Assert.Equal("", cells[2].Value);
                Assert.Equal("Продукты", cells[3].Value);
                Assert.Equal("-1234.56", cells[4].Value);
                Assert.Equal("2", cells[4].Attribute("s").Value);
                Assert.Equal("=Покупка <товаров> & услуги", cells[6].Value);
                Assert.Equal("inlineStr", cells[6].Attribute("t").Value);
                Assert.Empty(cells[6].Elements(ns + "f"));
                Assert.Equal("Основная карта", cells[9].Value);
                Assert.Equal("", cells[10].Value);
                }
                }
            }
            finally { File.Delete(path); }
        }

        private sealed class ExportParser : Parser
        {
            public ExportParser() : base("", DataSet.LoadDefault(), ';')
            {
                var operation = new CardOperation { RowNumber = 1, Date = new DateTime(2026, 10, 31, 12, 30, 0),
                    Summ = -1234.56m, Category = "Магазин", Title = "=Покупка <товаров> & услуги", CardName = "Основная карта" };
                var scheme = new CategoryScheme();
                scheme.Mappings.Add(new CategoryMapping { Source = "Магазин", Target = "Продукты" });
                operation.ApplyCategoryScheme(scheme, true);
                _operations.Add(operation);
            }
            public override void RunParse(string cardName = null) { }
        }
    }
}

