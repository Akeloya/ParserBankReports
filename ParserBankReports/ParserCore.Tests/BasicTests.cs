using System;
using System.IO;
using Xunit;

namespace ParserCore.Tests
{
    public class BasicTests
    {
        private const string _fileNames = "H:\\TestsData";
        private const string _outFileName = "result.csv";
        [Fact(Skip = "Manual")]        
        public void TestParse()
        {
            var pathWithDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _outFileName);
            var files = Directory.GetFiles(_fileNames, "*.html");

            var testDs = DataSet.LoadDefault();
            Assert.NotNull(testDs.DataXPath);
            Assert.NotNull(testDs.Title);
            Assert.NotNull(testDs.Category);
            Assert.NotNull(testDs.DataXPath);
            Assert.NotNull(testDs.Date);
            Assert.NotNull(testDs.DateProceed);
            Assert.NotEmpty(testDs.RestXPath);
            Assert.NotEmpty(testDs.RootTableXpath);

            foreach (var file in files)
            {
                var parser = Parser.Get(file,testDs,';');
                parser.RunParse();
                Assert.NotNull(parser.Operations);
                Assert.NotEmpty(parser.Operations);
                foreach(var op in parser.Operations)
                {
                    Assert.NotEqual(0, op.RowNumber);
                    Assert.NotEmpty(op.Title);
                    Assert.NotEmpty(op.Category);
                    Assert.NotEqual(0, op.Summ);
                    Assert.NotEmpty(op.Location);
                    Assert.NotEqual(DateTime.MinValue, op.Date);
                    Assert.NotEqual(DateTime.MinValue, op.ProcessDate);
                }
                parser.Save(pathWithDir);
            }
            
        }

        [Fact]
        public void TestDataSetConfig()
        {
            const string jsonFileName = "settings.json";

            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, jsonFileName);
            var ds = DataSet.LoadDefault();
            ds.Save(path);

            var checkDs = DataSet.LoadSettings(path);
            Assert.NotEmpty(checkDs.RootTableXpath);
            Assert.NotEmpty(checkDs.RestXPath);
            Assert.NotEmpty(checkDs.DataXPath);
            Assert.NotNull(checkDs.Title);
            Assert.NotNull(checkDs.Category);
            Assert.NotNull(checkDs.DataXPath);
            Assert.NotNull(checkDs.Date);
            Assert.NotNull(checkDs.DateProceed);

            Assert.Throws<FileNotFoundException>(()=> DataSet.LoadSettings(path + ".ttt"));
        }
    }
}
