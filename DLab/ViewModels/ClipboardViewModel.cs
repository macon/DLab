using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Caliburn.Micro;
using DLab.Domain;
using DLab.Infrastructure;
using DLab.Views;
using Action = System.Action;
using Clipboard = System.Windows.Clipboard;
using Screen = Caliburn.Micro.Screen;

namespace DLab.ViewModels
{
	public class ClipboardViewModel : Screen, ITabViewModel, IHandle<ClipboardChangedEvent>
    {
        private readonly IEventAggregator _eventAggregator;
	    private readonly ICatalog _catalog;
	    private ClipboardItemViewModel _selectedClipboardItem;
        private static bool _isWritingToClipboard;
	    private List<ClipboardItemViewModel> _masterList;
	    private string _searchText = "";
	    private bool _hideStateMsg;
	    private Task _clipboardTidyTask;
        private CancellationTokenSource _cancelClipboardTidyTask;

	    public ClipboardViewModel(IEventAggregator eventAggregator, ICatalog catalog)
        {
            _eventAggregator = eventAggregator;
	        _catalog = catalog;
	        _eventAggregator.Subscribe(this);
            DisplayName = "Clipboard";

            ClipboardItems = new BindableCollection<ClipboardItemViewModel>();

            _masterList = new List<ClipboardItemViewModel>();

	        HideStateMsg = false;
        }

        private async Task ClipboardTidyAsync(TimeSpan interval, CancellationToken token)
        {
            if (interval > TimeSpan.Zero)
                await Task.Delay(interval, token);

            while (!token.IsCancellationRequested)
            {
                Debug.WriteLine("Tidying {0}", DateTime.Now);
                foreach (var clipboardItemViewModel in _masterList.Where(x => !x.IsSaved && x.PasteCount > 2))
                {
                    Debug.WriteLine("Persisting clipboard item:{0}", new object[] {clipboardItemViewModel.DisplayText});
//                    _catalog.Save(clipboardItemViewModel.Instance);
                    _catalog.TrySaveClipboardItem(clipboardItemViewModel.Instance);
                }

                if (interval > TimeSpan.Zero)
                    await Task.Delay(interval, token);
            }
        }

	    public bool HideStateMsg
	    {
	        get { return _hideStateMsg; }
	        set
	        {
	            _hideStateMsg = value;
                NotifyOfPropertyChange();
	        }
	    }

	    protected override void OnDeactivate(bool close)
	    {
	        base.OnDeactivate(close);
            _cancelClipboardTidyTask.Cancel();
	    }

	    protected async override void OnViewAttached(object view, object context)
	    {
	        base.OnViewAttached(view, context);
            var clipboardItems = await LoadClipboardItemsAsync();
            _masterList.Clear();
            _masterList.AddRange(clipboardItems);

            SafeAddToClipboardHistory(Clipboard.GetText());
            RebuildClipboardItems();

            HideStateMsg = true;

            //            Application.Current.Dispatcher.BeginInvoke(
            //                (Action) async delegate
            //                {
            //                    _cancelClipboardTidyTask = new CancellationTokenSource();
            //                    _clipboardTidyTask = ClipboardTidyAsync(TimeSpan.FromSeconds(10), _cancelClipboardTidyTask.Token);
            //           	        await _clipboardTidyTask;
            //                }, DispatcherPriority.Render, null);
            _cancelClipboardTidyTask = new CancellationTokenSource();
            _clipboardTidyTask = ClipboardTidyAsync(TimeSpan.FromSeconds(10), _cancelClipboardTidyTask.Token);
            //           	        await _clipboardTidyTask;
	    }

	    private async Task<List<ClipboardItemViewModel>> LoadClipboardItemsAsync()
	    {
//	        await Task.Delay(5000);
	        return await Task.Run(() => _catalog.ClipboardItems().Select(x => new ClipboardItemViewModel(x)).ToList());
	    }

	    public BindableCollection<ClipboardItemViewModel> ClipboardItems { get; set; }

        public int Order
        {
            get { return 1; }
        }

        public void Handle(ClipboardChangedEvent message)
        {
            if (!Clipboard.ContainsText()) return;

            SafeAddToClipboardHistory(Clipboard.GetText());

            RebuildClipboardItems();
            SelectedClipboardItem = ClipboardItems.First();
        }

	    private void SafeAddToClipboardHistory(string text)
	    {
            if (string.IsNullOrEmpty(text)) return;

            if (_masterList.Any(x => x.Text.Equals(text, StringComparison.InvariantCultureIgnoreCase)))
            {
                BringItemToTop(text);
                return;
            }
            _masterList.Insert(0, new ClipboardItemViewModel(text));
	    }

	    public void SetClipboardBlind(string text)
        {
            var s = Clipboard.ContainsText() ? Clipboard.GetText() : "";
            if (Clipboard.ContainsText() && s.Equals(text, StringComparison.InvariantCultureIgnoreCase)) { return; }

            ShellView.ActiveClipboardString = text;
            Clipboard.SetText(text);
        }

	    public string SearchText
	    {
	        get { return _searchText; }
	        set
	        {
                if (_searchText.Equals(value, StringComparison.InvariantCultureIgnoreCase)) return;
	            _searchText = value;
	            if (string.IsNullOrEmpty(_searchText))
	            {
                    RebuildClipboardItems();
                    return;
	            }
	            var matchedItems = _masterList.Where(x => x.Text.IndexOf(_searchText, StringComparison.InvariantCultureIgnoreCase) >= 0);
                RebuildClipboardItems(matchedItems);
	        }
	    }

	    public void Paste()
        {
            Debug.WriteLine("clipboard text: {0}", new object[] {SelectedClipboardItem});
            SetClipboardBlind(SelectedClipboardItem.Text);
	        SelectedClipboardItem.PasteCount++;
	        ReportPasteCounts();

            var title = Win32.GetWindowTitle(ShellView.ClientHwnd);
            Debug.WriteLine("target window title: {0}", new object[] { title });

            Win32.SendPasteToClient(ShellView.ClientHwnd);
			BringItemToTop(SelectedClipboardItem.Text);
            _eventAggregator.Publish(new UserActionEvent(), Execute.OnUIThread);
        }

	    private void ReportPasteCounts()
	    {
            Debug.WriteLine("Paste counts:");
            foreach (var model in _masterList)
	        {
	            Debug.WriteLine("'{0}' --> {1}", new object[]{model.DisplayText, model.PasteCount});
	        }
	    }

	    private void BringItemToTop(string clipboardText)
	    {
		    var matchedItem = _masterList.FirstOrDefault(x => x.Text.Equals(clipboardText, StringComparison.InvariantCultureIgnoreCase));
		    if (matchedItem == null) return;

		    var oldIndex = _masterList.IndexOf(matchedItem);
		    if (oldIndex == 0) return;

		    _masterList.RemoveAt(oldIndex);
            _masterList.Insert(0, matchedItem);
	        RebuildClipboardItems();
	    }

	    private void RebuildClipboardItems(IEnumerable<ClipboardItemViewModel> items = null)
	    {
	        ClipboardItems.Clear();
            ClipboardItems.AddRange(items ?? _masterList);
	        SelectedClipboardItem = ClipboardItems.First();
	    }

       
        public ClipboardItemViewModel SelectedClipboardItem
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
