using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParserCore
{
    public abstract partial class Parser
    {
        protected string _path;
        protected readonly char _delimetr;
        protected IDataSet _dataSet;
        protected readonly List<CardOperation> _operations = [];
        protected Parser(string path, string settingsPath, char delimeter)
        {
            _dataSet = DataSet.LoadSettings(settingsPath);
            _path = path;
            _delimetr = delimeter;
        }

        protected Parser(string path, IDataSet dataSet, char delimeter)
        {
            _dataSet = dataSet;
            _path = path;
            _delimetr = delimeter;
        }
        public static Parser Get(string path, IDataSet ds, char delimetr)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentNullException(nameof(path));
            if (ds == null)
                throw new ArgumentNullException(nameof(ds));

            var fileInfo = new FileInfo(path);

            switch (fileInfo.Extension)
            {
                case ".html":
                    return new HtmlParser(path, ds, delimetr);
                case ".pdf":
                    return new PdfParser(path, ds, delimetr);
                default:
                    throw new NotImplementedException($"Не реализован парсер для {fileInfo.Extension}");
            }
        }
        public static Parser Get(string path, string settingsPath, char delimetr)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentNullException(nameof(path));
            if (string.IsNullOrWhiteSpace(settingsPath))
                throw new ArgumentNullException(nameof(settingsPath));

            var fileInfo = new FileInfo(path);

            switch (fileInfo.Extension)
            {
                case ".html":
                    return new HtmlParser(path, settingsPath, delimetr);
                case ".pdf":
                    return new PdfParser(path, settingsPath, delimetr);
                default:
                    throw new NotImplementedException($"Не реализован парсер для {fileInfo.Extension}");
            }
        }

        public IEnumerable<CardOperation> Operations => _operations;
        public decimal? ReportIncome { get; protected set; }
        public decimal? ReportExpense { get; protected set; }
        public abstract void RunParse(string cardName = null);
        /// <summary>
        /// Saves data to file
        /// </summary>
        /// <param name="filePath">full path with file name and extension</param>
        public void Save(string filePath, Encoding encoding = null)
        {
            if (!Operations.Any())
                return;
            using var file = new StreamWriter(filePath, false, encoding ?? Encoding.GetEncoding(1251));
            StringBuilder headerSb = BuildHeader();
            file.WriteLine(headerSb);

            foreach (var operation in Operations)
            {
                StringBuilder sb = BuildRow(operation);
                file.WriteLine(sb);
            }
            file.Close();
        }

        public async Task SaveAsync(string filePath, Encoding encoding = null)
        {
            if (!Operations.Any())
                return;
            using var file = new StreamWriter(filePath, false, encoding ?? Encoding.GetEncoding(1251));
            StringBuilder headerSb = BuildHeader();
            await file.WriteLineAsync(headerSb.ToString());

            foreach (var operation in Operations)
            {
                StringBuilder sb = BuildRow(operation);
                await file.WriteLineAsync(sb.ToString());
            }
            file.Close();
        }

        private StringBuilder BuildRow(CardOperation operation)
        {
            var sb = new StringBuilder();
            sb.Append(operation.RowNumber).Append(_delimetr)
                .Append(operation.Date).Append(_delimetr)
                .Append(operation.ProcessDate).Append(_delimetr)
                .Append(EscapeCsvField(operation.Category)).Append(_delimetr)
                .Append(operation.Summ).Append(_delimetr)
                .Append(operation.BalanceAfter).Append(_delimetr)
                .Append(operation.Title).Append(_delimetr)
                .Append(operation.Location).Append(_delimetr)
                .Append(_delimetr)
                .Append(EscapeCsvField(operation.CardName)).Append(_delimetr);
            return sb;
        }

        private string EscapeCsvField(string value)
        {
            if (value == null)
                return string.Empty;
            return value.IndexOfAny(new[] { _delimetr, '"', '\r', '\n' }) >= 0
                ? "\"" + value.Replace("\"", "\"\"") + "\""
                : value;
        }

        protected StringBuilder BuildHeader()
        {
            var headerSb = new StringBuilder();
            headerSb.Append("N#").Append(_delimetr)
                .Append(_dataSet.Date.Name).Append(_delimetr)
                .Append(_dataSet.DateProceed.Name).Append(_delimetr)
                .Append(_dataSet.Category.Name).Append(_delimetr)
                .Append(_dataSet.Summ.Name).Append(_delimetr)
                .Append(_dataSet.BalanceArter.Name).Append(_delimetr)
                .Append(_dataSet.Title.Name).Append(_delimetr)
                .Append(_dataSet.Location.Name).Append(_delimetr)
                .Append(_delimetr)
                .Append("Карта/счёт").Append(_delimetr)
                .Append("Статья расходов");
            return headerSb;
        } 
    }
}
