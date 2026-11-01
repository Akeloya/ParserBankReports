using System;

namespace ParserCore
{
    public class DataColumn
    {
        public string Name { get; set; }
        public string XPath { get; set; }
        public DataContainerType ContainerType { get; set; }
        public string AttributeName { get; set; }

        public DataColumn(string name, string xPath, DataContainerType type, string attributeName = null)
        {
            Name = name;
            XPath = xPath;
            ContainerType = type;
            if (type == DataContainerType.Attribute && string.IsNullOrEmpty(attributeName))
                throw new ArgumentNullException(nameof(attributeName));
            AttributeName = attributeName;
        }
    }

    public enum DataContainerType
    {
        InnerText,
        Attribute
    }
}
