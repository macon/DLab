using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using DLab.Infrastructure;
using DLab.ViewModels;
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
        private HwndSource _hWndSource;
        private ShellViewModel _vm;
        public static IntPtr ClientHwnd;

        /// <summary>
        /// Occurs when the user clicks Exit from the tray icon context menu
        /// </summary>
        private bool _userRequestedExit;

        public ShellView()
        {
            InitializeComponent();
            InitializeApp();
        }

        private void InitializeApp()
        {
            Loaded += ShellView_Loaded;
            _showCommandHotKey = new CustomHotKey("_showCommandHotKey", Key.Space, ModifierKeys.Control | ModifierKeys.Alt, true, SetFocusToCommand);
            _showClipboardHotKey = new CustomHotKey("_showClipboardHotKey", Key.C, ModifierKeys.Control | ModifierKeys.Alt, true, SetFocusToClipboard);
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
            SetFocus(1, () =>
            {
                var view = TabViewModel.Content as TabView;
                var child = FindChild<ListBox>(view.Items, "ClipboardItems");
                child.SelectedIndex = 0;
                child.Focus();
            });
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

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var wih = new WindowInteropHelper(this);
            _hWndSource = HwndSource.FromHwnd(wih.Handle);

            _hWndSource.AddHook(WndProc);
            var result = Win32.AddClipboardFormatListener(_hWndSource.Handle);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_CLIPBOARDUPDATE = 0x031D;

            switch (msg)
            {
                case  WM_CLIPBOARDUPDATE:
                    DrawContent();
                    break;
            }

            return IntPtr.Zero;
        }

        private void DrawContent()
        {
            if (Clipboard.ContainsText())
            {
                // we have some text in the clipboard.
                var t = Win.Clipboard.GetText();
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
                Win.Application.Current.MainWindow.Visibility = Visibility.Hidden;
            }
        }

        #endregion

        private CloseThisWindowCommand()
        {
        }

        public static readonly ICommand Instance = new CloseThisWindowCommand();
    }
}
