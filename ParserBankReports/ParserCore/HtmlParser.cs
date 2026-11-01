using HtmlAgilityPack;

using System.Text;

namespace ParserCore
{
    internal class HtmlParser : Parser
    {
        public HtmlParser(string path, string settingsPath, char delimetr) : base(path, settingsPath, delimetr)
        {            
        }

        public HtmlParser(string path, IDataSet ds, char delimetr) : base(path, ds, delimetr)
        {
        }

        public override void RunParse(string cardName = null)
        {
            var htmlDoc = new HtmlDocument();
            var encoding = Encoding.GetEncoding(_dataSet.EncodingPage);
            htmlDoc.Load(_path, encoding);

            var root = htmlDoc.DocumentNode.SelectSingleNode(_dataSet.RootTableXpath);
            var sumStr = htmlDoc.DocumentNode.SelectSingleNode(_dataSet.RestXPath).InnerText?.Trim();
            var rest = sumStr.AsDecimal();

            if (root != null)
            {
                int rowNum = 1;
                var nodes = root.SelectNodes(_dataSet.DataXPath);
                foreach (var node in nodes)
                {
                    rest = GetValues(rest, rowNum++, node, cardName);
                }
            }
        }

        private decimal GetValues(decimal rest, int rowNum, HtmlNode node, string cardName)
        {
            var sumNode = node.SelectSingleNode(_dataSet.Summ.XPath);
            var factor = -1;
            if (sumNode.FirstChild.Attributes["class"].Value == "trs_st-refill")
                factor = 1;

            var sum = GetNodeValue(node, _dataSet.Summ).AsDecimal() * factor;
            rest += sum;
            _operations.Add(new CardOperation
            {
                RowNumber = rowNum,
                Title = GetNodeValue(node, _dataSet.Title),
                Category = GetNodeValue(node, _dataSet.Category),
                Date = GetNodeValue(node, _dataSet.Date).AsDate(),
                Location = GetNodeValue(node, _dataSet.Location),
                ProcessDate = GetNodeValue(node, _dataSet.DateProceed).AsDate(),
                Summ = sum,
                BalanceAfter = rest,
                CardName = cardName
            });
            return rest;
        }

        private static string GetNodeValue(HtmlNode node, DataColumn col)
        {
            string result = null;
            var selectedNode = node.SelectSingleNode(col.XPath);
            if (selectedNode != null)
            {
                if (col.ContainerType == DataContainerType.InnerText)
                    result = selectedNode.InnerText;
                else
                    result = selectedNode.Attributes[col.AttributeName].Value;
            }

            if (!string.IsNullOrEmpty(result))
                result = result.Trim();
            return result;
        }
    }
}
