using Caliburn.Micro;

using ParserApp.Controls;
using ParserApp.Services;

using ParserCore;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

using WpfExtendedControls;

using Parser = ParserCore.Parser;

namespace ParserApp
{
    public partial class MainWindowViewModel : Screen
    {
        private Parser _parser;
        private readonly IDialogService _dialogService;
        public MainWindowViewModel(IDialogService dialogService)
        {
            _dialogService = dialogService;
            DisplayName = Properties.Resources.MainWindowTitle;
            LoadCategorySchemes();
            LoadRecentFiles();
        }
        public BindableCollection<CardOperation> Operations { get; set; }
        public string IncomeSummary => BuildSummary("Пополнение", _parser?.ReportIncome,
            Operations?.Where(operation => operation.Summ > 0).Sum(operation => operation.Summ) ?? 0);
        public string ExpenseSummary => BuildSummary("Списание", _parser?.ReportExpense,
            -(Operations?.Where(operation => operation.Summ < 0).Sum(operation => operation.Summ) ?? 0));

        private static string BuildSummary(string name, decimal? reportTotal, decimal parsedTotal)
        {
            var report = reportTotal.HasValue ? reportTotal.Value.ToString("N2") + " ₽" : "нет данных";
            return $"{name}: в отчёте — {report}; по операциям — {parsedTotal:N2} ₽";
        }

        public Task CloseAsync()
        {
            return TryCloseAsync();
        }

        public Task AboutAsync()
        {
            var licenses = new List<LicenseInformation>
            {
                new LicenseInformation("Application", Encoding.UTF8.GetString(Properties.Resources.LICENSE), false)
            };
            var ab = new AboutApp(licenses, null);
            ab.Show();
            return Task.CompletedTask;
        }

        public Task OpenSettingsAsync()
        {
            return _dialogService.ShowAsync(new EditSettingsViewModel());
        }

        public Task OpenHtmlFileAsync() => _dialogService.OpenFileDialog("Выберите отчёт", "Отчёты банков", "*.html;*.pdf", OpenReportPathAsync);
        public Task OpenCsvFileAsync()
        {
            return _dialogService.ShowPupupAsync("Не реализованно!");
        }
        public bool CanSave_CanExecute => _parser != null && _parser.Operations.Any();
        public string[] ExportEncodings { get; } = new[] { "Windows-1251", "UTF-8" };
        public string SelectedExportEncoding { get; set; } = "Windows-1251";
        public Task SaveAsync()
        {
            return _dialogService.SaveFileDialog("Сохранить отчёт", "CSV (*.csv)|*.csv|Excel (*.xlsx)|*.xlsx", (filePath) =>
            {
                var xlsx = string.Equals(Path.GetExtension(filePath), ".xlsx", StringComparison.OrdinalIgnoreCase);
                var encoding = SelectedExportEncoding == "UTF-8"
                    ? new UTF8Encoding(true)
                    : Encoding.GetEncoding(1251);
                ApplyCategoryScheme();
                return xlsx ? _parser.SaveXlsxAsync(filePath) : _parser.SaveAsync(filePath, encoding);
            });
        }

        public Task PrintAsync()
        {
            _dialogService.PrintDialog((printDialog) =>
            {
                var flowDoc = new FlowDocument();
                var table = new Table();
                int numberOfColumns = 8;
                for (int x = 0; x < numberOfColumns; x++)
                {
                    table.Columns.Add(new TableColumn());

                    // Set alternating background colors for the middle colums.
                    if (x % 2 == 0)
                        table.Columns[x].Background = Brushes.Beige;
                    else
                        table.Columns[x].Background = Brushes.LightSteelBlue;
                }
                var titleGroup = new TableRowGroup();
                var titleRow = new TableRow();
                titleGroup.Rows.Add(titleRow);

                titleRow.Background = Brushes.Silver;
                titleRow.FontSize = 40;
                titleRow.FontWeight = FontWeights.Bold;
                titleRow.Cells.Add(new TableCell(new Paragraph(new Run("Данные отчета СБОЛ"))));
                titleRow.Cells[0].ColumnSpan = 6;

                table.RowGroups.Add(titleGroup);
                var tgroup = new TableRowGroup();
                foreach (var item in _parser.Operations)
                {
                    var tRow = new TableRow
                    {
                        FontSize = 12,
                        FontWeight = FontWeights.Normal
                    };
                    tRow.Cells.Add(new TableCell(new Paragraph(new Run(item.RowNumber.ToString()))));
                    tRow.Cells.Add(new TableCell(new Paragraph(new Run(item.Date.ToString()))));
                    tRow.Cells.Add(new TableCell(new Paragraph(new Run(item.Category))));
                    tRow.Cells.Add(new TableCell(new Paragraph(new Run(item.Title))));
                    tRow.Cells.Add(new TableCell(new Paragraph(new Run(item.Summ.ToString()))));
                    tRow.Cells.Add(new TableCell(new Paragraph(new Run(item.BalanceAfter.ToString()))));
                    tRow.Cells.Add(new TableCell(new Paragraph(new Run(item.Location))));
                    tRow.Cells.Add(new TableCell(new Paragraph(new Run(item.ProcessDate.ToString()))));

                    tgroup.Rows.Add(tRow);
                }
                table.RowGroups.Add(tgroup);
                flowDoc.Blocks.Add(table);
                printDialog.PrintDocument(((IDocumentPaginatorSource)flowDoc).DocumentPaginator, "Csv");
                return Task.CompletedTask;
            });

            return Task.CompletedTask;
        }

        public Task HelpAsync()
        {
            return Task.Run(() => Process.Start("https://github.com/Akeloya/ParserBankReports/issues"));
        }
    }
}
