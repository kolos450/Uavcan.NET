using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CanardApp.Tools.BusMonitor
{
    /// <summary>
    /// Interaction logic for TableFilterControl.xaml
    /// </summary>
    partial class TableFilterControl : UserControl
    {
        public TableFilterControl()
        {
            InitializeComponent();
        }

        public FilterModel Value { get; } = new FilterModel();
        public event EventHandler RemoveButtonClicked;

        void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            RemoveButtonClicked?.Invoke(sender, e);
        }

        void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            Value.Content = ((TextBox)sender).Text;
        }

        void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            Value.Enabled = ((ToggleButton)sender).IsChecked == true;
        }

        void NegateButton_Click(object sender, RoutedEventArgs e)
        {
            Value.Negate = ((ToggleButton)sender).IsChecked == true;
        }

        void RegexButton_Click(object sender, RoutedEventArgs e)
        {
            Value.Regex = ((ToggleButton)sender).IsChecked == true;
        }

        void CaseButton_Click(object sender, RoutedEventArgs e)
        {
            Value.CaseSensitive = ((ToggleButton)sender).IsChecked == true;
        }
    }
}
