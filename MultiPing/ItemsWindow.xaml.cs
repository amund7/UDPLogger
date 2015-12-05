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
using System.Windows.Shapes;
using MultiPing;
using System.Windows.Threading;

namespace UDPLogger {
  /// <summary>
  /// Interaction logic for ItemsWindow.xaml
  /// </summary>
  public partial class ItemsWindow : Window {
    public ItemsWindow() {
      InitializeComponent();
    }

    ResultsCollection c;

    public ItemsWindow(ResultsCollection resultsCollection) {
      InitializeComponent();
      c = resultsCollection;
      dataGrid.Items.Clear();
      dataGrid.ItemsSource = c._collection;
      this.Show();
    }

    private void DataGridCell_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
      DataGridCell cell = sender as DataGridCell;
      GridColumnFastEdit(cell, e);
    }

    private void DataGridCell_PreviewTextInput(object sender, TextCompositionEventArgs e) {
      DataGridCell cell = sender as DataGridCell;
      GridColumnFastEdit(cell, e);
    }

    private static void GridColumnFastEdit(DataGridCell cell, RoutedEventArgs e) {
      if (cell == null || cell.IsEditing || cell.IsReadOnly)
        return;

      DataGrid dataGrid = FindVisualParent<DataGrid>(cell);
      if (dataGrid == null)
        return;

      if (!cell.IsFocused) {
        cell.Focus();
      }

      if (cell.Content is CheckBox) {
        if (dataGrid.SelectionUnit != DataGridSelectionUnit.FullRow) {
          if (!cell.IsSelected)
            cell.IsSelected = true;
        } else {
          DataGridRow row = FindVisualParent<DataGridRow>(cell);
          if (row != null && !row.IsSelected) {
            row.IsSelected = true;
          }
        }
      } else {
        ComboBox cb = cell.Content as ComboBox;
        if (cb != null) {
          //DataGrid dataGrid = FindVisualParent<DataGrid>(cell);
          dataGrid.BeginEdit(e);
          cell.Dispatcher.Invoke(
           DispatcherPriority.Background,
           new Action(delegate { }));
          cb.IsDropDownOpen = true;
        }
      }
    }


    private static T FindVisualParent<T>(UIElement element) where T : UIElement {
      UIElement parent = element;
      while (parent != null) {
        T correctlyTyped = parent as T;
        if (correctlyTyped != null) {
          return correctlyTyped;
        }

        parent = VisualTreeHelper.GetParent(parent) as UIElement;
      }
      return null;
    }
  }
}
