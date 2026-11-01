using System;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ParserCore
{
    public abstract partial class Parser
    {
        private static readonly XNamespace SpreadsheetNs = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

        public Task SaveXlsxAsync(string filePath) => Task.Run(() => SaveXlsx(filePath));

        public void SaveXlsx(string filePath)
        {
            if (!Operations.Any())
                return;
            if (_operations.Count > 1048575)
                throw new InvalidOperationException("Превышено максимальное количество строк Excel.");

            var ns = SpreadsheetNs;
            var rows = new XElement(ns + "sheetData");
            var headers = new[] { "N#", _dataSet.Date.Name, _dataSet.DateProceed.Name,
                _dataSet.Category.Name, _dataSet.Summ.Name, _dataSet.BalanceArter.Name,
                _dataSet.Title.Name, _dataSet.Location.Name, "", "Карта/счёт", "Статья расходов" };
            rows.Add(new XElement(ns + "row", new XAttribute("r", 1),
                headers.Select((value, index) => XlsxCell(index, 1, value))));
            var rowNumber = 2;
            foreach (var operation in Operations)
            {
                object[] values = { operation.RowNumber, operation.Date, operation.ProcessDate,
                    operation.Category, operation.Summ, operation.BalanceAfter, operation.Title,
                    operation.Location, "", operation.CardName, "" };
                var currentRow = rowNumber++;
                rows.Add(new XElement(ns + "row", new XAttribute("r", currentRow),
                    values.Select((value, index) => XlsxCell(index, currentRow, value))));
            }

            using var file = File.Create(filePath);
            using var archive = new ZipArchive(file, ZipArchiveMode.Create);
            WriteXlsxPart(archive, "[Content_Types].xml", XElement.Parse(
                "<Types xmlns='http://schemas.openxmlformats.org/package/2006/content-types'><Default Extension='rels' ContentType='application/vnd.openxmlformats-package.relationships+xml'/><Default Extension='xml' ContentType='application/xml'/><Override PartName='/xl/workbook.xml' ContentType='application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml'/><Override PartName='/xl/worksheets/sheet1.xml' ContentType='application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml'/><Override PartName='/xl/styles.xml' ContentType='application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml'/></Types>"));
            WriteXlsxPart(archive, "_rels/.rels", XElement.Parse(
                "<Relationships xmlns='http://schemas.openxmlformats.org/package/2006/relationships'><Relationship Id='rId1' Type='http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument' Target='xl/workbook.xml'/></Relationships>"));
            WriteXlsxPart(archive, "xl/workbook.xml", XElement.Parse(
                "<workbook xmlns='http://schemas.openxmlformats.org/spreadsheetml/2006/main' xmlns:r='http://schemas.openxmlformats.org/officeDocument/2006/relationships'><sheets><sheet name='Операции' sheetId='1' r:id='rId1'/></sheets></workbook>"));
            WriteXlsxPart(archive, "xl/_rels/workbook.xml.rels", XElement.Parse(
                "<Relationships xmlns='http://schemas.openxmlformats.org/package/2006/relationships'><Relationship Id='rId1' Type='http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet' Target='worksheets/sheet1.xml'/><Relationship Id='rId2' Type='http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles' Target='styles.xml'/></Relationships>"));
            WriteXlsxPart(archive, "xl/styles.xml", XElement.Parse(
                "<styleSheet xmlns='http://schemas.openxmlformats.org/spreadsheetml/2006/main'><numFmts count='1'><numFmt numFmtId='164' formatCode='dd.mm.yyyy hh:mm:ss'/></numFmts><fonts count='1'><font><sz val='11'/><name val='Calibri'/></font></fonts><fills count='2'><fill><patternFill patternType='none'/></fill><fill><patternFill patternType='gray125'/></fill></fills><borders count='1'><border/></borders><cellStyleXfs count='1'><xf numFmtId='0' fontId='0' fillId='0' borderId='0'/></cellStyleXfs><cellXfs count='3'><xf numFmtId='0' fontId='0' fillId='0' borderId='0' xfId='0'/><xf numFmtId='164' fontId='0' fillId='0' borderId='0' xfId='0' applyNumberFormat='1'/><xf numFmtId='4' fontId='0' fillId='0' borderId='0' xfId='0' applyNumberFormat='1'/></cellXfs></styleSheet>"));
            WriteXlsxPart(archive, "xl/worksheets/sheet1.xml", new XElement(ns + "worksheet", rows));
        }

        private static XElement XlsxCell(int column, int row, object value)
        {
            var ns = SpreadsheetNs;
            var cell = new XElement(ns + "c", new XAttribute("r", ((char)('A' + column)).ToString() + row));
            if (value is DateTime date && date >= new DateTime(1900, 1, 1))
            {
                cell.Add(new XAttribute("s", 1), new XElement(ns + "v", date.ToOADate().ToString("R", CultureInfo.InvariantCulture)));
            }
            else if (value is decimal || value is int)
            {
                if (value is decimal)
                    cell.Add(new XAttribute("s", 2));
                cell.Add(new XElement(ns + "v", Convert.ToString(value, CultureInfo.InvariantCulture)));
            }
            else
            {
                cell.Add(new XAttribute("t", "inlineStr"), new XElement(ns + "is",
                    new XElement(ns + "t", new XAttribute(XNamespace.Xml + "space", "preserve"),
                        value is DateTime ? "" : value?.ToString() ?? "")));
            }
            return cell;
        }

        private static void WriteXlsxPart(ZipArchive archive, string path, XElement content)
        {
            using var stream = archive.CreateEntry(path).Open();
            content.Save(stream);
        }
    }
}
