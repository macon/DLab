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
	public class ClipboardViewModel : Screen, ITabViewModel, IHandle<ClipboardChangedEvent>
    {
        private readonly IEventAggregator _eventAggregator;
        private ClipboardItemViewModel _selectedClipboardItemViewModel;
        private static bool _isWritingToClipboard;

        public ClipboardViewModel(IEventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator;
            _eventAggregator.Subscribe(this);
            DisplayName = "Clipboard";

            ClipboardItems = new BindableCollection<ClipboardItemViewModel>();
            if (Clipboard.ContainsText())
            {
                ClipboardItems.Add(new ClipboardItemViewModel(Clipboard.GetText()));
            }
        }

        public BindableCollection<ClipboardItemViewModel> ClipboardItems { get; set; }

        public int Order
        {
            get { return 1; }
        }

        public void Handle(ClipboardChangedEvent message)
        {
            if (!Clipboard.ContainsText()) return;

	        var clipboardText = Clipboard.GetText();
	        if (ClipboardItems.Any(x => x.Text.Equals(clipboardText, StringComparison.InvariantCultureIgnoreCase)))
	        {
		        BringItemToTop(clipboardText);
				return;
	        }

            Debug.WriteLine("Writing to clipboard from thread {0}", Thread.CurrentThread.ManagedThreadId);
            ClipboardItems.Insert(0, new ClipboardItemViewModel(Clipboard.GetText()));
            SelectedClipboardItemViewModel = ClipboardItems.First();
        }

	    public void SetClipboardBlind(string text)
        {
            var s = Clipboard.ContainsText() ? Clipboard.GetText() : "";
            if (Clipboard.ContainsText() && s.Equals(text, StringComparison.InvariantCultureIgnoreCase)) { return; }

            ShellView.ActiveClipboardString = text;
            Clipboard.SetText(text);
        }

		public void Search(string searchText)
		{
			
		}

        public void Paste()
        {
            Debug.WriteLine("clipboard text: {0}", new object[] {SelectedClipboardItemViewModel});
            SetClipboardBlind(SelectedClipboardItemViewModel.Text);

            var title = Win32.GetWindowTitle(ShellView.ClientHwnd);
            Debug.WriteLine("target window title: {0}", new object[] { title });

            SendPasteToClient();
			BringItemToTop(SelectedClipboardItemViewModel.Text);
            _eventAggregator.Publish(new UserActionEvent(), Execute.OnUIThread);
        }

	    private void BringItemToTop(string clipboardText)
	    {
		    var matchedItem = ClipboardItems.FirstOrDefault(x => x.Text.Equals(clipboardText, StringComparison.InvariantCultureIgnoreCase));
		    if (matchedItem == null) return;

		    var oldIndex = ClipboardItems.IndexOf(matchedItem);
		    if (oldIndex == 0) return;

		    ClipboardItems.Move(oldIndex, 0);
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
        
        public ClipboardItemViewModel SelectedClipboardItemViewModel
        {
            get { return _selectedClipboardItemViewModel; }
            set
            {
                _selectedClipboardItemViewModel = value;
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
