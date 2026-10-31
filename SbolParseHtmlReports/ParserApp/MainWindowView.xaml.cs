using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ParserApp
{
    /// <summary>
    /// Interaction logic for MainWIndowView.xaml
    /// </summary>
    public partial class MainWindowView : UserControl
    {
        public MainWindowView()
        {
            InitializeComponent();
        }

        private static T FindAncestor<T>(DependencyObject source) where T : DependencyObject
        {
            while (source != null && !(source is T))
                source = source is Visual || source is System.Windows.Media.Media3D.Visual3D
                    ? VisualTreeHelper.GetParent(source)
                    : LogicalTreeHelper.GetParent(source);
            return source as T;
        }

        private void Operations_OnLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount != 2)
                return;
            var cell = FindAncestor<DataGridCell>(e.OriginalSource as DependencyObject);
            if (cell == null)
                return;
            e.Handled = true;
            var content = cell.Column.GetCellContent(cell.DataContext);
            var text = content is TextBlock block ? block.Text :
                content is CheckBox checkBox ? checkBox.IsChecked?.ToString() ?? string.Empty : string.Empty;
            try
            {
                Clipboard.SetDataObject(text, true);
            }
            catch (System.Runtime.InteropServices.ExternalException)
            {
                MessageBox.Show("Не удалось скопировать текст: буфер обмена занят. Повторите попытку.",
                    "Копирование", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void Operations_OnRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var row = FindAncestor<DataGridRow>(e.OriginalSource as DependencyObject);
            if (!(row?.Item is ParserCore.CardOperation operation) ||
                !(DataContext is MainWindowViewModel viewModel))
                return;

            e.Handled = true;
            row.IsSelected = true;
            var menu = new ContextMenu { PlacementTarget = row };
            var add = new MenuItem { Header = "Добавить соответствие" };
            add.Click += async (s, args) =>
            {
                // Finish edits before changing the mapping collection and its selection.
                if (!CategoryMappingsGrid.CommitEdit(DataGridEditingUnit.Cell, true) ||
                    !CategoryMappingsGrid.CommitEdit(DataGridEditingUnit.Row, true))
                    return;
                var mapping = await viewModel.AddCategoryMappingForOperation(operation);
                if (mapping == null)
                    return;
                CategoryMappingsGrid.ScrollIntoView(mapping);
                CategoryMappingsGrid.CurrentCell = new DataGridCellInfo(mapping, CategoryMappingsGrid.Columns[2]);
                CategoryMappingsGrid.Focus();
                CategoryMappingsGrid.BeginEdit();
            };
            menu.Items.Add(add);
            menu.IsOpen = true;
        }

        private void ClearCategoryFilter_OnRightClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is MainWindowViewModel viewModel)
            {
                viewModel.ClearCategoryFilter();
                CategoryMappingsGrid.UnselectAll();
                e.Handled = true;
            }
        }
    }
}
