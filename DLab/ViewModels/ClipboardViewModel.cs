using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using Caliburn.Micro;
using DLab.Infrastructure;
using DLab.Views;

namespace DLab.ViewModels
{
    public class ClipboardViewModel : Screen, ITabViewModel, IHandle<ClipboardChangedEvent>
    {
        private readonly IEventAggregator _eventAggregator;
        private string _selectedClipboardItem;

        public ClipboardViewModel(IEventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator;
            _eventAggregator.Subscribe(this);
            DisplayName = "Clipboard";

            ClipboardItems = new BindableCollection<string>();
            if (Clipboard.ContainsText())
            {
                ClipboardItems.Add(Clipboard.GetText());
            }
        }

        public BindableCollection<string> ClipboardItems { get; set; }

        public int Order
        {
            get { return 1; }
        }

        public void Handle(ClipboardChangedEvent message)
        {
            if (Clipboard.ContainsText() && !IsWritingToClipboard)
            {
                ClipboardItems.Insert(0, Clipboard.GetText());
                SelectedClipboardItem = ClipboardItems.First();
            }
        }

        public bool IsWritingToClipboard { get; set; }

        public void SetClipboardBlind(string text)
        {
            IsWritingToClipboard = true;
            Clipboard.SetText(SelectedClipboardItem);
            IsWritingToClipboard = false;
        }

        public void Paste()
        {
            Debug.WriteLine("clipboard text: {0}", SelectedClipboardItem);
            SetClipboardBlind(SelectedClipboardItem);

            Win32.BringWindowToTop(ShellView.ClientHwnd);

            var ip = new Win32.INPUT {type = Win32.INPUT_KEYBOARD};
            ip.U.ki.wScan = 0;
            ip.U.ki.time = 0;
            ip.U.ki.dwExtraInfo = new UIntPtr(0);

            // press CTRL key
            ip.U.ki.wVk = Win32.VirtualKeyShort.CONTROL;
            ip.U.ki.dwFlags = 0;
            Win32.SendInput(1, new[] {ip}, Win32.INPUT.Size);

            // press V key
            ip.U.ki.wVk = Win32.VirtualKeyShort.KEY_V;
            ip.U.ki.dwFlags = 0;
            Win32.SendInput(1, new[] {ip}, Win32.INPUT.Size);

            // release V key
            ip.U.ki.wVk = Win32.VirtualKeyShort.KEY_V;
            ip.U.ki.dwFlags = Win32.KEYEVENTF.KEYUP;
            Win32.SendInput(1, new[] {ip}, Win32.INPUT.Size);

            // release CTRL key
            ip.U.ki.wVk = Win32.VirtualKeyShort.CONTROL;
            ip.U.ki.dwFlags = Win32.KEYEVENTF.KEYUP;
            Win32.SendInput(1, new[] {ip}, Win32.INPUT.Size);

            Application.Current.MainWindow.Visibility = Visibility.Hidden;
        }

        public string SelectedClipboardItem
        {
            get { return _selectedClipboardItem; }
            set
            {
                _selectedClipboardItem = value;
                NotifyOfPropertyChange();
            }
        }
    }

    public class ClipboardChangedEvent
    {
    }

    public interface ITabViewModel : IScreen
    {
        int Order { get; }
    }
}
