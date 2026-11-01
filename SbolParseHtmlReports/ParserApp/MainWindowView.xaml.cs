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

        private async void RecentReport_OnClick(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem item && item.DataContext is string path && DataContext is MainWindowViewModel vm)
            { e.Handled = true; await vm.OpenReportPathAsync(path); }
        }

        private async void RecentProject_OnClick(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem item && item.DataContext is string path && DataContext is MainWindowViewModel vm)
            { e.Handled = true; await vm.OpenProjectPathAsync(path); }
        }

        private static T FindAncestor<T>(DependencyObject source) where T : DependencyObject
        {
            while (source != null && !(source is T))
                source = source is Visual || source is System.Windows.Media.Media3D.Visual3D
                    ? VisualTreeHelper.GetParent(source)
                    : LogicalTreeHelper.GetParent(source);
            return source as T;
        }

        private void View_OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F6 && DataContext is MainWindowViewModel viewModel)
            {
                e.Handled = true;
                viewModel.ClearAllTemporaryCategories();
            }
        }

        private void Operations_OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.F3 || !(DataContext is MainWindowViewModel viewModel)) return;
            var row = FindAncestor<DataGridRow>(e.OriginalSource as DependencyObject);
            var operation = row?.Item as ParserCore.CardOperation ?? OperationsGrid.SelectedItem as ParserCore.CardOperation;
            if (operation == null) return;
            e.Handled = true;
            var mapping = viewModel.FindCategoryMapping(operation);
            if (mapping != null)
                CategoryMappingsGrid.ScrollIntoView(mapping, CategoryMappingsGrid.Columns[1]);
        }

        private void Operations_OnLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (FindAncestor<TextBox>(e.OriginalSource as DependencyObject) != null)
                return;
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
            if (FindAncestor<TextBox>(e.OriginalSource as DependencyObject) != null) return;
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
                CategoryMappingsGrid.CurrentCell = new DataGridCellInfo(mapping, CategoryMappingsGrid.Columns[3]);
                CategoryMappingsGrid.Focus();
                CategoryMappingsGrid.BeginEdit();
            };
            menu.Items.Add(add);
            menu.Items.Add(new Separator());
            var temporary = new MenuItem { Header = "Во временную категорию…" };
            temporary.Click += async (s, args) => await viewModel.SetTemporaryCategoryAsync(operation);
            menu.Items.Add(temporary);
            var clearTemporary = new MenuItem { Header = "Очистить временную категорию", IsEnabled = operation.HasTemporaryCategory };
            clearTemporary.Click += (s, args) => viewModel.ClearTemporaryCategory(operation);
            menu.Items.Add(clearTemporary);
            menu.IsOpen = true;
        }

        private void CategoryMappings_OnRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            ClearCategoryFilter_OnClick(sender, e);
        }

        private void DeleteCategoryMapping_OnClick(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            var button = sender as Button;
            if (!(button?.DataContext is ParserCore.CategoryMapping mapping) ||
                !(DataContext is MainWindowViewModel viewModel))
                return;
            if (!CategoryMappingsGrid.CommitEdit(DataGridEditingUnit.Cell, true) ||
                !CategoryMappingsGrid.CommitEdit(DataGridEditingUnit.Row, true))
                return;
            viewModel.DeleteCategoryMapping(mapping);
        }
        private void ClearCategoryFilter_OnClick(object sender, RoutedEventArgs e)
        {
            if (!(DataContext is MainWindowViewModel viewModel))
                return;
            if (!CategoryMappingsGrid.CommitEdit(DataGridEditingUnit.Cell, true) ||
                !CategoryMappingsGrid.CommitEdit(DataGridEditingUnit.Row, true))
                return;
            CategoryMappingsGrid.UnselectAll();
            viewModel.ClearCategoryFilter();
        }
    }
}
