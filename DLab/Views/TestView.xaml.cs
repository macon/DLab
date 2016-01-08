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
            AddHandler(Keyboard.KeyDownEvent, (KeyEventHandler)HandleKeyDownEvent);
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

        private void HandleKeyDownEvent(object sender, KeyEventArgs e)
        {
            var vm = (TestViewModel)DataContext;

//            if (e.Key == Key.E && (Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Shift)) == (ModifierKeys.Control | ModifierKeys.Shift))
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control) 
                && !Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) 
                && !Keyboard.Modifiers.HasFlag(ModifierKeys.Alt))
            {
                var kc = new KeyConverter();
                var charVal = (string)kc.ConvertTo(e.Key, typeof (string));
                if (charVal == null) { return; }
                vm.DoCommand(charVal.ToCharArray()[0]);
                e.Handled = true;
            }
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

        private void CtrlCCopyCmdExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            var lb = (ListBox)(sender);
            var selected = lb.SelectedItem as HyperJumpFolderViewModel;
            if (selected != null) { Clipboard.SetText(selected.FullPath);}
        }

        private void CtrlCCopyCmdCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }
    }
}
