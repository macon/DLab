using System.Windows.Controls;
using System.Windows.Input;
using DLab.ViewModels;

namespace DLab.Views
{
    /// <summary>
    /// Interaction logic for ClipboardView.xaml
    /// </summary>
    public partial class ClipboardView : UserControl
    {
        public ClipboardView()
        {
            InitializeComponent();
            ClipboardItems.Focus();
        }

//        private void ClipboardItems_OnKeyUp(object sender, KeyEventArgs e)
//        {
//            Debug.WriteLine("[ClipboardItems_OnKeyUp] {0}, HAndled:{1}", e.Key, e.Handled);
//            if ((e.Key >= Key.A && e.Key <= Key.Z))
//            {
//                SearchText.Focus();
//
////                e.Handled = true;
////                var routedEvent = Keyboard.KeyUpEvent;
////
////                SearchText.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(SearchText), 0, e.Key)
////                {
////                    RoutedEvent = routedEvent
////                });
//
//            }
//        }
        private void ClipboardItems_OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            SearchText.Focus();
        }

        private void SearchText_OnPreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Up || e.Key == Key.Down)
            {
                ClipboardItems.Focus();
                ClipboardItems.SelectedIndex = 0;

                ClipboardItems.UpdateLayout();
                var clipboardItem = (ListBoxItem)ClipboardItems.ItemContainerGenerator.ContainerFromItem(ClipboardItems.SelectedItem);
                clipboardItem.Focus();
            }
        }

        private void ClipboardItems_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var viewModel = DataContext as ClipboardViewModel;
            if (viewModel == null) return;

            viewModel.Paste();
        }
    }
}
