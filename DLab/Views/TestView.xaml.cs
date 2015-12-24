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
using DLab.ViewModels;

namespace DLab.Views
{
    /// <summary>
    /// Interaction logic for TestView.xaml
    /// </summary>
    public partial class TestView : UserControl
    {
        public TestView()
        {
            InitializeComponent();
        }

        private void UserCommand_OnPreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Down || MatchedItems.Items.Count <= 0) return;
        
            MatchedItems.Focus();
            MatchedItems.SelectedIndex = 0;
        
            MatchedItems.UpdateLayout();
            var matchedItem = (ListBoxItem)MatchedItems.ItemContainerGenerator.ContainerFromItem(MatchedItems.SelectedItem);
            matchedItem.Focus();
        }

        private void MatchedItems_OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            (DataContext as TestViewModel).DoCommand(e.Text.ToCharArray()[0]);
//            UserCommand.Focus();
        }
        
        private void MatchedItems_OnPreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Down)
            {
//                (DataContext as TestViewModel).DoCommand(e.Key.);
                return;
            }
            if (e.Key != Key.Down || MatchedItems.Items.Count <= 0) return;
        
            MatchedItems.Focus();
            MatchedItems.SelectedIndex = 0;
        
            MatchedItems.UpdateLayout();
            var matchedItem = (ListBoxItem)MatchedItems.ItemContainerGenerator.ContainerFromItem(MatchedItems.SelectedItem);
            matchedItem.Focus();
        }
    }
}
