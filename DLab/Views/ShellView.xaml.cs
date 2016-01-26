using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using DLab.Infrastructure;
using DLab.ViewModels;
using log4net;
using Action = System.Action;
using ListBox = System.Windows.Controls.ListBox;
using TextBox = System.Windows.Controls.TextBox;
using Win = System.Windows;

namespace DLab.Views
{
    /// <summary>
    /// Interaction logic for ShellView.xaml
    /// </summary>
    public partial class ShellView : Window
    {
        private HotKeyHost _hotKeyHost;
        private HotKey _showCommandHotKey;
        private HotKey _showProcessHotKey;
        private CustomHotKey _showClipboardHotKey;
        private CustomHotKey _showSettingsHotKey;
        private CustomHotKey _showDirHotKey;
        private HwndSource _hWndSource;
        private ShellViewModel _vm;
        public static IntPtr ClientHwnd;
        private ILog _logger;

        /// <summary>
        /// Occurs when the user clicks Exit from the tray icon context menu
        /// </summary>
        private bool _userRequestedExit;

        public ShellView()
        {
            InitializeComponent();
            InitializeApp();
            _logger = LogManager.GetLogger("ShellView");
            ActiveClipboardString = "";
        }

        private void InitializeApp()
        {
            Loaded += ShellView_Loaded;
//            Application.Current.Deactivated += Current_Deactivated;
            _showCommandHotKey = new CustomHotKey("_showCommandHotKey", Key.Space, ModifierKeys.Alt, true, SetFocusToCommand);
            _showProcessHotKey = new CustomHotKey("_showProcessHotKey", Key.Tab, ModifierKeys.Control, true, SetFocusToProcess);
            _showClipboardHotKey = new CustomHotKey("_showClipboardHotKey", Key.C, ModifierKeys.Alt, true, SetFocusToClipboard);
            _showSettingsHotKey = new CustomHotKey("_showSettingsHotKey", Key.S, ModifierKeys.Control | ModifierKeys.Alt, true, SetFocusToSettings);
            _showDirHotKey = new CustomHotKey("_showDirHotKey", Key.D, ModifierKeys.Control | ModifierKeys.Alt, true, SetFocusToTest);
        }

        void Current_Deactivated(object sender, EventArgs e)
        {
            _vm.IsHidden = true;
        }

        private void OnNotifyIconDoubleClick(object sender, MouseButtonEventArgs e)
        {
            OnOpenClick(sender, new RoutedEventArgs());
        }

        private void OnOpenClick(object sender, RoutedEventArgs e)
        {
            SetFocusToCommand();
        }

        private void OnOpenClipboardClick(object sender, RoutedEventArgs e)
        {
            SetFocusToClipboard();
        }

        private void OnOpenDirClick(object sender, RoutedEventArgs e)
        {
            SetFocusToDir();
        }

        private void OnOpenSettingsClick(object sender, RoutedEventArgs e)
        {
            SetFocusToSettings();
        }

        private void OnExitClick(object sender, RoutedEventArgs e)
        {
            _userRequestedExit = true;
            Close();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (!_userRequestedExit)
            {
                Application.Current.MainWindow.Visibility = Visibility.Hidden;
                e.Cancel = true;
                return;
            }

            Loaded -= ShellView_Loaded;
            Application.Current.Deactivated -= Current_Deactivated;
            _hotKeyHost.RemoveHotKey(_showCommandHotKey);
            _hotKeyHost.RemoveHotKey(_showProcessHotKey);
            _hotKeyHost.RemoveHotKey(_showClipboardHotKey);
//            _hotKeyHost.RemoveHotKey(_showSettingsHotKey);
            _hotKeyHost.RemoveHotKey(_showDirHotKey);
            Win32.RemoveClipboardFormatListener(_hWndSource.Handle);
            _hWndSource.RemoveHook(WndProc);
            notifyIcon.Visibility = Visibility.Hidden;
            base.OnClosing(e);
        }

        void ShellView_Loaded(object sender, RoutedEventArgs e)
        {
            _vm = DataContext as ShellViewModel;
            InitialiseHotKey(this);
//            SetFocusToCommand();
            SetFocusToCommand();
        }

        private void InitialiseHotKey(Window window)
        {
            _hotKeyHost = new HotKeyHost(window);
            _hotKeyHost.AddHotKey(_showCommandHotKey);
            _hotKeyHost.AddHotKey(_showProcessHotKey);
            _hotKeyHost.AddHotKey(_showClipboardHotKey);
            _hotKeyHost.AddHotKey(_showDirHotKey);
//            _hotKeyHost.AddHotKey(_showSettingsHotKey);
        }

        private void SetFocusToCommand()
        {
            _vm.ActivateCommandModel();
            SetFocus(() =>
            {
                var view = ActiveItem.Content as CommandView;
                var child = view.UserCommand;
                child.Text = "";
                child.Focus();
                Keyboard.Focus(child);
            });
        }

        private void SetFocusToProcess()
        {
            ClientHwnd = GetPreviousWindow();

            _vm.ActivateProcessModel();
            SetFocus(() =>
            {
                var view = ActiveItem.Content as ProcessView;
                var child = view.UserCommand;
                child.Text = "";
                child.Focus();
                Keyboard.Focus(child);
            });
        }

        private void SetFocusToClipboard()
        {
            ClientHwnd = GetPreviousWindow();
            _vm.ActivateClipboardModel();

            SetFocus(() =>
            {
                var view = ActiveItem.Content as ClipboardView;
                var clipboardItems = view.ClipboardItems;

                clipboardItems.Focus();

                if (clipboardItems.Items.Count == 0) return;
                clipboardItems.SelectedIndex = 0;

                clipboardItems.UpdateLayout();
                var clipboardItem = (ListBoxItem) clipboardItems.ItemContainerGenerator.ContainerFromItem(clipboardItems.SelectedItem);
                if (clipboardItem == null)
                {
                    _logger.WarnFormat("Failed to get SelectedItem from ClipboardItems {0}", clipboardItems.SelectedItem);
                    return;
                }
                clipboardItem.Focus();
            });
        }

        private void SetFocusToSettings()
        {
            ClientHwnd = GetPreviousWindow();

            _vm.Settings();
        }

        private void SetFocusToDir()
        {
            SetFocus(() =>
            {
                _vm.ActivateHyperspaceModel();
                var view = ActiveItem.Content as HyperspaceView;
//                var child = view.UserCommand;
//                child.Text = "";
//                child.Focus();

            });
        }

        private void SetFocusToTest()
        {
            _vm.ActivateTestModel();
            SetFocus(() =>
            {
                var view = ActiveItem.Content as TestView;
                var child = view.UserCommand;
//                child.Text = "";
                child.Focus();
                Keyboard.Focus(child);
            });
        }

        private void SetFocus(Action focusCommand)
        {
            Activate();
            Application.Current.MainWindow.Visibility = Visibility.Visible;

//            var view = ActiveViewModel.Content as TabView;

            Application.Current.Dispatcher.BeginInvoke(
                (Action)delegate
                      {
//                          view.Items.SelectedIndex = tabIndex;

                          Application.Current.Dispatcher.BeginInvoke
                                    (focusCommand, DispatcherPriority.Render, null);

                      }, DispatcherPriority.Render, null);
        }

        //---------------------------------------------------------

        /// <summary>
        /// Finds a Child of a given item in the visual tree. 
        /// </summary>
        /// <param name="parent">A direct parent of the queried item.</param>
        /// <typeparam name="T">The type of the queried item.</typeparam>
        /// <param name="childName">x:Name or Name of child. </param>
        /// <returns>The first parent item that matches the submitted type parameter. 
        /// If not matching item can be found, 
        /// a null parent is being returned.</returns>
        public static T FindChild<T>(DependencyObject parent, string childName) where T : DependencyObject
        {
            // Confirm parent and childName are valid. 
            if (parent == null) return null;

            T foundChild = null;

            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                // If the child is not of the request child type child
                T childType = child as T;
                if (childType == null)
                {
                    // recursively drill down the tree
                    foundChild = FindChild<T>(child, childName);

                    // If the child is found, break so we do not overwrite the found child. 
                    if (foundChild != null) break;
                }
                else if (!string.IsNullOrEmpty(childName))
                {
                    var frameworkElement = child as FrameworkElement;
                    // If the child's name is set for search
                    if (frameworkElement != null && frameworkElement.Name == childName)
                    {
                        // if the child's name is of the request name
                        foundChild = (T)child;
                        break;
                    }
                }
                else
                {
                    // child element found.
                    foundChild = (T)child;
                    break;
                }
            }

            return foundChild;
        }

        public IntPtr GetPreviousWindow()
        {
            var activeAppWindow = Win32.GetForegroundWindow();
            if (activeAppWindow == IntPtr.Zero)
                return IntPtr.Zero;

            var prevAppWindow = Win32.GetLastActivePopup(activeAppWindow);
            return Win32.IsWindowVisible(prevAppWindow) ? prevAppWindow : IntPtr.Zero;
        }

        public void FocusToPreviousWindow()
        {
            var prevWindow = GetPreviousWindow();
            if (prevWindow != IntPtr.Zero)
                Win32.SetForegroundWindow(prevWindow);
        }


        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var wih = new WindowInteropHelper(this);
            _hWndSource = HwndSource.FromHwnd(wih.Handle);

            _hWndSource.AddHook(WndProc);
            Win32.AddClipboardFormatListener(_hWndSource.Handle);
        }

        public static string ActiveClipboardString { get; set; }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_CLIPBOARDUPDATE = 0x031D;

            switch (msg)
            {
                case WM_CLIPBOARDUPDATE:
                    _vm.ClipboardChanged();
                    break;
            }

            return IntPtr.Zero;
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Clicked");
        }
    }

    public class CloseThisWindowCommand : ICommand
    {
        #region ICommand Members

        public bool CanExecute(object parameter)
        {
            return (parameter is Window);
        }

        public event EventHandler CanExecuteChanged;

        public void Execute(object parameter)
        {
            if (CanExecute(parameter))
            {
                Application.Current.MainWindow.Visibility = Visibility.Hidden;
            }
        }

        #endregion

        private CloseThisWindowCommand()
        {
        }

        public static readonly ICommand Instance = new CloseThisWindowCommand();
    }
}
