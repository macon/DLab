using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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
    }
}
