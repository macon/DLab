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

namespace DLab.Views
{
    /// <summary>
    /// Interaction logic for ProcessView.xaml
    /// </summary>
    public partial class ProcessView : UserControl
    {
        public ProcessView()
        {
            InitializeComponent();
        }
        private void UserCommand_OnPreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Down || ProcessNames.Items.Count <= 0) return;

            ProcessNames.Focus();
            ProcessNames.SelectedIndex = 0;

            ProcessNames.UpdateLayout();
            var matchedItem = (ListBoxItem)ProcessNames.ItemContainerGenerator.ContainerFromItem(ProcessNames.SelectedItem);
            matchedItem.Focus();
        }

        private void ProcessNames_OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            UserCommand.Focus();
        }
    }
}
