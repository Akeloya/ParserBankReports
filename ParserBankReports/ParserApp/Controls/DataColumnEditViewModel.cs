using Caliburn.Micro;

using ParserCore;

namespace ParserApp.Controls
{
    public class DataColumnEditViewModel : PropertyChangedBase
    {
        private readonly DataColumn _column;

        public DataColumnEditViewModel(DataColumn column)
        {
            _column = column;
        }

        public string Name
        {
            get { return _column.Name; }
            set { _column.Name = value; }
        }

        public string AttributeName
        {
            get {return _column.AttributeName;}
            set { _column.AttributeName = value; }
        }

        public DataContainerType ContainerType
        {
            get {return _column.ContainerType;}
            set {_column.ContainerType = value;}
        }

        public string XPath
        {
            get { return _column.XPath; }
            set { _column.XPath = value; }
        }
    }
}
