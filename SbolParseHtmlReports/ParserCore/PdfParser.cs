using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace ParserCore
{
    internal class PdfParser : Parser
    {
        private const string Money = @"[+\-]?(?:\d{1,3}(?: \d{3})+|\d+),\d{2}";
        private static readonly Regex OperationLine = new Regex(
            @"^(?<date>\d{2}\.\d{2}\.\d{4})\s+(?<time>\d{2}:\d{2})\s+(?<category>.+?)\s+(?<amount>" + Money + @")\s+(?<balance>" + Money + @")$",
            RegexOptions.Compiled);
        private static readonly Regex ProcessingLine = new Regex(
            @"^(?<date>\d{2}\.\d{2}\.\d{4})\s+(?<code>\d+)\s+(?<description>.+)$",
            RegexOptions.Compiled);

        internal PdfParser(string path, string settingsPath, char delimeter) : base(path, settingsPath, delimeter) { }
        internal PdfParser(string path, IDataSet ds, char delimeter) : base(path, ds, delimeter) { }

        public override void RunParse(string cardName = null)
        {
            _operations.Clear();
            ReportIncome = null;
            ReportExpense = null;
            using var reader = new PdfReader(_path);
            using var document = new PdfDocument(reader);
            CardOperation current = null;
            string description = null;

            for (var pageNumber = 1; pageNumber <= document.GetNumberOfPages(); pageNumber++)
            {
                var text = PdfTextExtractor.GetTextFromPage(document.GetPage(pageNumber), new LocationTextExtractionStrategy())
                    .Replace('\u00a0', ' ').Replace('\u202f', ' ');
                ReadTotals(text);
                foreach (var rawLine in text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var line = Regex.Replace(rawLine.Trim(), @"\s+", " ");
                    var match = OperationLine.Match(line);
                    if (match.Success)
                    {
                        CompleteDescription(current, description);
                        var amountText = match.Groups["amount"].Value;
                        var amount = ParseMoney(amountText);
                        current = new CardOperation
                        {
                            RowNumber = _operations.Count + 1,
                            Date = DateTime.ParseExact(match.Groups["date"].Value + " " + match.Groups["time"].Value,
                                "dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture),
                            Category = match.Groups["category"].Value,
                            Summ = amountText.StartsWith("+") ? amount : -Math.Abs(amount),
                            BalanceAfter = ParseMoney(match.Groups["balance"].Value),
                            CardName = cardName
                        };
                        _operations.Add(current);
                        description = null;
                        continue;
                    }

                    if (current == null)
                        continue;
                    var processing = ProcessingLine.Match(line);
                    if (processing.Success)
                    {
                        current.ProcessDate = DateTime.ParseExact(processing.Groups["date"].Value,
                            "dd.MM.yyyy", CultureInfo.InvariantCulture);
                        description = processing.Groups["description"].Value;
                    }
                    else if (description != null)
                    {
                        // Page headers and footers are outside the operation description.
                        if (line.StartsWith("Продолжение") || line.StartsWith("Дата формирования") || line == "*")
                        {
                            CompleteDescription(current, description);
                            description = null;
                        }
                        else
                            description += " " + line;
                    }
                }
                CompleteDescription(current, description);
                description = null;
            }
        }

        private void ReadTotals(string text)
        {
            var income = Regex.Match(text, @"Пополнение\s+(?<value>" + Money + ")");
            var expense = Regex.Match(text, @"Списание\s+(?<value>" + Money + ")");
            if (income.Success)
                ReportIncome = ParseMoney(income.Groups["value"].Value);
            if (expense.Success)
                ReportExpense = ParseMoney(expense.Groups["value"].Value);
        }

        private static decimal ParseMoney(string text)
        {
            return decimal.Parse(text.Replace(" ", "").Replace(',', '.'),
                NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);
        }

        private static void CompleteDescription(CardOperation operation, string description)
        {
            if (operation == null || description == null)
                return;
            var suffix = Regex.Match(description, @"\.?\s*Операция\s+по\s+(?:карте|сч[её]ту)\b");
            operation.Title = suffix.Success ? description.Substring(0, suffix.Index).Trim() : description.Trim();
            operation.Location = operation.Title;
        }
    }
}
