using System.Windows;
using System.Windows.Controls;

namespace ParserApp.Controls
{
    /// <summary>
    /// Логика взаимодействия для EnterCardNameView.xaml
    /// </summary>
    public partial class EnterCardNameView : UserControl
    {
        public EnterCardNameView()
        {
            InitializeComponent();

            Loaded += EnterCardNameView_Loaded;
        }

        private void EnterCardNameView_Loaded(object sender, RoutedEventArgs e)
        {
            TbCardName.Focus();
        }
    }
}
