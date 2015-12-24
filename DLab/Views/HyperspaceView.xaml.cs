using System.Windows.Controls;
using System.Windows.Input;
using DLab.ViewModels;

namespace DLab.Views
{
    /// <summary>
    /// Interaction logic for HyperJumpView.xaml
    /// </summary>
    public partial class HyperspaceView : UserControl
    {
        public HyperspaceView()
        {
            InitializeComponent();
        }

//        private void UserCommand_OnPreviewKeyUp(object sender, KeyEventArgs e)
//        {
//            if (e.Key != Key.Down || MatchedItems.Items.Count <= 0) return;
//
//            MatchedItems.Focus();
//            MatchedItems.SelectedIndex = 0;
//
//            MatchedItems.UpdateLayout();
//            var matchedItem = (ListBoxItem)MatchedItems.ItemContainerGenerator.ContainerFromItem(MatchedItems.SelectedItem);
//            matchedItem.Focus();
//        }
//
//        private void MatchedItems_OnPreviewTextInput(object sender, TextCompositionEventArgs e)
//        {
////            (DataContext as HyperspaceViewModel).DoCommand(e.Text);
//            UserCommand.Focus();
//        }
//
//        private void MatchedItems_OnPreviewKeyUp(object sender, KeyEventArgs e)
//        {
//            if (e.Key != Key.Down || MatchedItems.Items.Count <= 0) return;
//
//            MatchedItems.Focus();
//            MatchedItems.SelectedIndex = 0;
//
//            MatchedItems.UpdateLayout();
//            var matchedItem = (ListBoxItem)MatchedItems.ItemContainerGenerator.ContainerFromItem(MatchedItems.SelectedItem);
//            matchedItem.Focus();
//        }
    }
}
