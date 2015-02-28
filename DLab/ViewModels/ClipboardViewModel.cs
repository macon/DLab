using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Caliburn.Micro;
using DLab.Infrastructure;
using DLab.Views;
using Clipboard = System.Windows.Clipboard;
using Screen = Caliburn.Micro.Screen;

namespace DLab.ViewModels
{
    public class ClipboardItem
    {
        public const int LineLength = 200;
        public string Text { get; set; }

        public ClipboardItem(string text)
        {
            Text = text;
        }

        public string DisplayText
        {
            get
            {
                var parts = Text.Split(new[]{'\r', '\n'}, StringSplitOptions.RemoveEmptyEntries);

                return parts.Length > 1 
                    ? string.Format("{0}\n{1}{2}", ParseLine(parts[0]), ParseLine(parts[1]), parts.Length>2 ? "..." : "") 
                    : ParseLine(parts[0]);
            }
        }

        private string ParseLine(string text)
        {
            return text.Length > LineLength ? string.Format("{0}...", text.Substring(0, LineLength)) : text;
        }
    }


    public class ClipboardViewModel : Screen, ITabViewModel, IHandle<ClipboardChangedEvent>
    {
        private readonly IEventAggregator _eventAggregator;
        private ClipboardItem _selectedClipboardItem;
        private static bool _isWritingToClipboard;

        public ClipboardViewModel(IEventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator;
            _eventAggregator.Subscribe(this);
            DisplayName = "Clipboard";

            ClipboardItems = new BindableCollection<ClipboardItem>();
            if (Clipboard.ContainsText())
            {
                ClipboardItems.Add(new ClipboardItem(Clipboard.GetText()));
            }
        }

        public BindableCollection<ClipboardItem> ClipboardItems { get; set; }

        public int Order
        {
            get { return 1; }
        }

        public void Handle(ClipboardChangedEvent message)
        {
            if (!Clipboard.ContainsText()) return;
            Debug.WriteLine("Writing to clipboard from thread {0}", Thread.CurrentThread.ManagedThreadId);
            ClipboardItems.Insert(0, new ClipboardItem(Clipboard.GetText()));
            SelectedClipboardItem = ClipboardItems.First();
        }

        public void SetClipboardBlind(string text)
        {
            var s = Clipboard.ContainsText() ? Clipboard.GetText() : "";
            if (Clipboard.ContainsText() && s.Equals(text, StringComparison.InvariantCultureIgnoreCase)) { return; }

            ShellView.ActiveClipboardString = text;
            Clipboard.SetText(text);
        }

        public void Paste()
        {
            Debug.WriteLine("clipboard text: {0}", new object[] {SelectedClipboardItem});
            SetClipboardBlind(SelectedClipboardItem.Text);

            var title = Win32.GetWindowTitle(ShellView.ClientHwnd);
            Debug.WriteLine("target window title: {0}", new object[] { title });

            SendPasteToClient();

            // re-arrange clipboard listview if needed
            var oldIndex = ClipboardItems.IndexOf(SelectedClipboardItem);
            if (oldIndex != 0)
            {
                ClipboardItems.Move(oldIndex, 0);
                NotifyOfPropertyChange("ClipboardItems");
            }

            _eventAggregator.Publish(new UserActionEvent(), Execute.OnUIThread);
        }

        private static void SendPasteToClient()
        {
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
        }
        
        public ClipboardItem SelectedClipboardItem
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
