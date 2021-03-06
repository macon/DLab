﻿using System.Windows.Controls;
using System.Windows.Input;

namespace DLab.Views
{
    /// <summary>
    /// Interaction logic for CommandView.xaml
    /// </summary>
    public partial class CommandView : UserControl
    {
        public CommandView()
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
            UserCommand.Focus();
        }
    }
}
