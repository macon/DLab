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
        private CustomHotKey _showClipboardHotKey;
        private CustomHotKey _showSettingsHotKey;
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
            _logger = LogManager.GetLogger("Default");
            ActiveClipboardString = "";
        }

        private void InitializeApp()
        {
            Loaded += ShellView_Loaded;
            Application.Current.Deactivated += Current_Deactivated;
//            _showCommandHotKey = new CustomHotKey("_showCommandHotKey", Key.Space, ModifierKeys.Control | ModifierKeys.Alt, true, SetFocusToCommand);
            _showCommandHotKey = new CustomHotKey("_showCommandHotKey", Key.Space, ModifierKeys.Alt, true, SetFocusToCommand);
            _showClipboardHotKey = new CustomHotKey("_showClipboardHotKey", Key.C, ModifierKeys.Alt, true, SetFocusToClipboard);
            _showSettingsHotKey = new CustomHotKey("_showSettingsHotKey", Key.S, ModifierKeys.Control | ModifierKeys.Alt, true, SetFocusToSettings);
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
            _hotKeyHost.RemoveHotKey(_showClipboardHotKey);
            Win32.RemoveClipboardFormatListener(_hWndSource.Handle);
            _hWndSource.RemoveHook(WndProc);
            notifyIcon.Visibility = Visibility.Hidden;
            base.OnClosing(e);
        }

        void ShellView_Loaded(object sender, RoutedEventArgs e)
        {
            _vm = DataContext as ShellViewModel;
            InitialiseHotKey(this);
            SetFocusToCommand();
        }

        private void InitialiseHotKey(Window window)
        {
            _hotKeyHost = new HotKeyHost(window);
            _hotKeyHost.AddHotKey(_showCommandHotKey);
            _hotKeyHost.AddHotKey(_showClipboardHotKey);
//            _hotKeyHost.AddHotKey(_showSettingsHotKey);
        }

        private void SetFocusToCommand()
        {
            SetFocus(0, () =>
            {
                var view = TabViewModel.Content as TabView;
                var child = FindChild<TextBox>(view.Items, "UserCommand");
                child.Text = "";
                child.Focus();
            });
        }

        private void SetFocusToClipboard()
        {
            ClientHwnd = GetPreviousWindow();

            SetFocus(1, () =>
            {
                var view = TabViewModel.Content as TabView;
                var clipboardItems = FindChild<ListBox>(view.Items, "ClipboardItems");

                clipboardItems.Focus();

                if (clipboardItems.Items.Count == 0) return;
                clipboardItems.SelectedIndex = 0;

                clipboardItems.UpdateLayout();
                var clipboardItem = (ListBoxItem) clipboardItems.ItemContainerGenerator.ContainerFromItem(clipboardItems.SelectedItem);
                clipboardItem.Focus();
            });
        }

        private void SetFocusToSettings()
        {
            ClientHwnd = GetPreviousWindow();

            _vm.Settings();
        }

        private void SetFocus(int tabIndex, Action focusCommand)
        {
            Activate();
            Application.Current.MainWindow.Visibility = Visibility.Visible;

            var view = TabViewModel.Content as TabView;

            Application.Current.Dispatcher.BeginInvoke(
                (Action)delegate
                      {
                          view.Items.SelectedIndex = tabIndex;

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
//                    if (!ShouldCapture) return IntPtr.Zero;
                    DrawContent();
                    break;
            }

            return IntPtr.Zero;
        }

        private void DrawContent()
        {

            if (Clipboard.ContainsText())
            {
                IDataObject clipData = null;
                var s = "";
                try
                {
                    s = Clipboard.GetText();
                    clipData = Clipboard.GetDataObject();
                }
                catch (COMException e)
                {
                    _logger.Error(e);
                    return;
                }

                if (clipData == null) return;

                var clipboardText = "";
                try
                {
                    if (clipData.GetDataPresent(DataFormats.Text))
                    {
                        clipboardText = (string) clipData.GetData(DataFormats.Text, false);
                    }
                    else
                    {
                        _logger.InfoFormat("data in clipboard not in right format");
                    }
                    _logger.InfoFormat("read from clipboard: {0}", clipboardText);
                    _logger.InfoFormat("alt read from clipboard: {0}", s);
                }
                catch (COMException e)
                {
                    _logger.Error("Caught exception from clipData.GetData");
                    _logger.Error(e);
                    return;
                }

                if (clipboardText.Equals(ActiveClipboardString, StringComparison.InvariantCultureIgnoreCase)) return;

                // we have some text in the clipboard.
                if (_vm != null)
                {
                    _vm.ClipboardChanged();
                }
            }
            else if (Clipboard.ContainsFileDropList())
            {
                // we have a file drop list in the clipboard
                var fl = Win.Clipboard.GetFileDropList();
            }
            else if (Clipboard.ContainsImage())
            {
                // Because of a known issue in WPF,
                // we have to use a workaround to get correct
                // image that can be displayed.
                // The image have to be saved to a stream and then 
                // read out to workaround the issue.
                var ms = new MemoryStream();
                var enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(Win.Clipboard.GetImage()));
                enc.Save(ms);
                ms.Seek(0, SeekOrigin.Begin);

                var dec = new BmpBitmapDecoder(ms,
                    BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);

//                Image img = new Image();
//                img.Stretch = Stretch.Uniform;
//                img.Source = dec.Frames[0];
//                pnlContent.Children.Add(img);
            }
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
