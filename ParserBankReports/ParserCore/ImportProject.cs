using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace ParserCore
{
    public class ImportProject
    {
        public int Version { get; set; } = 1;
        public string ReportPath { get; set; }
        public string SchemeName { get; set; }
        public string ExportEncoding { get; set; }
        public bool ShowOriginalCategories { get; set; }
        public decimal? ReportIncome { get; set; }
        public decimal? ReportExpense { get; set; }
        public List<CardOperation> Operations { get; set; } = new List<CardOperation>();
        public List<CategoryMapping> Mappings { get; set; } = new List<CategoryMapping>();

        public void Save(string path) => File.WriteAllText(path, JsonConvert.SerializeObject(this, Formatting.Indented));
        public static ImportProject Load(string path)
        {
            var project = JsonConvert.DeserializeObject<ImportProject>(File.ReadAllText(path));
            if (project == null || project.Version != 1 || project.Operations == null || project.Mappings == null ||
                project.Operations.Exists(o => o == null) || project.Mappings.Exists(m => m == null))
                throw new InvalidDataException("Некорректный или неподдерживаемый файл проекта.");
            return project;
        }
    }

    public sealed class ProjectParser : Parser
    {
        public ProjectParser(ImportProject project) : base(string.Empty, DataSet.LoadDefault(), ';')
        {
            _operations.AddRange(project.Operations);
            ReportIncome = project.ReportIncome;
            ReportExpense = project.ReportExpense;
        }
        public override void RunParse(string cardName = null) { }
    }
}
