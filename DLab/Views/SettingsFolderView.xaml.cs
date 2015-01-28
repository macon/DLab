using System;
using System.Windows;
using System.Windows.Controls;

namespace DLab.Views
{
    /// <summary>
    /// Interaction logic for SettingsFolderView.xaml
    /// </summary>
    public partial class SettingsFolderView : UserControl
    {
        public SettingsFolderView()
        {
            InitializeComponent();
            Loaded += SettingsView_Loaded;
            Dispatcher.ShutdownStarted += Dispatcher_ShutdownStarted;
        }

        void Dispatcher_ShutdownStarted(object sender, EventArgs e)
        {
            Loaded -= SettingsView_Loaded;
            Dispatcher.ShutdownStarted -= Dispatcher_ShutdownStarted;
        }

        void SettingsView_Loaded(object sender, RoutedEventArgs e)
        {
            Folders.Focus();
        }

    }
}
