using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace ParserCore.Tests
{
    public class ExportEncodingTests
    {
        [Theory]
        [InlineData(false, false)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(true, true)]
        public async Task ExportWritesHeaderAndRowsInSelectedEncoding(bool asynchronous, bool utf8)
        {
            var path = Path.GetTempFileName();
            try
            {
                var parser = new ExportParser();
                var encoding = utf8 ? (Encoding)new UTF8Encoding(true) : Encoding.GetEncoding(1251);
                File.WriteAllText(path, new string('x', 4096));
                if (asynchronous)
                {
                    if (utf8)
                        await parser.SaveAsync(path, encoding);
                    else
                        await parser.SaveAsync(path);
                }
                else if (utf8)
                    parser.Save(path, encoding);
                else
                    parser.Save(path);

                var bytes = File.ReadAllBytes(path);
                var preamble = encoding.GetPreamble();
                Assert.Equal(preamble, bytes.Take(preamble.Length).ToArray());
                var text = encoding.GetString(bytes, preamble.Length, bytes.Length - preamble.Length);
                Assert.StartsWith("N#;Дата операции;", text);
                Assert.Contains("Покупка продуктов", text);
                var lines = text.Split(new[] { System.Environment.NewLine }, System.StringSplitOptions.RemoveEmptyEntries);
                var header = lines[0].Split(';');
                var row = lines[1].Split(';');
                Assert.Equal(11, header.Length);
                Assert.Equal(header.Length, row.Length);
                Assert.Equal(new[] { "Место", "", "Карта/счёт", "Статья расходов" }, header.Skip(7).ToArray());
                Assert.Equal(new[] { "Новосибирск", "", "Основная карта", "" }, row.Skip(7).ToArray());
                Assert.DoesNotContain("\uFFFD", text, System.StringComparison.Ordinal);
                Assert.DoesNotContain("xxxx", text);
                Assert.Equal(encoding.GetBytes(text), bytes.Skip(preamble.Length).ToArray());
                // Verify Cyrillic bytes independently of the decoding round trip.
                var expected = utf8 ? new byte[] { 0xD0, 0x94, 0xD0, 0xB0 } : new byte[] { 0xC4, 0xE0 };
                Assert.Equal(expected, bytes.Skip(preamble.Length + 3).Take(expected.Length).ToArray());
            }
            finally
            {
                File.Delete(path);
            }
        }

        private sealed class ExportParser : Parser
        {
            public ExportParser() : base(string.Empty, DataSet.LoadDefault(), ';')
            {
                _operations.Add(new CardOperation
                {
                    RowNumber = 1,
                    Title = "Покупка продуктов",
                    Location = "Новосибирск",
                    CardName = "Основная карта"
                });
            }

            public override void RunParse(string cardName = null) { }
        }
    }
}
