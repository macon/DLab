using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Caliburn.Micro;
using DLab.Domain;
using DLab.Infrastructure;
using DLab.Views;
using log4net;
using ILog = log4net.ILog;
using LogManager = log4net.LogManager;

namespace DLab.ViewModels
{
	public class ClipboardViewModel : Screen, ITabViewModel, IHandle<ClipboardChangedEvent>
    {
        private readonly IEventAggregator _eventAggregator;
	    private readonly ClipboardRepo _clipboardRepo;
	    private ClipboardItemViewModel _selectedClipboardItem;
	    private List<ClipboardItemViewModel> _masterList;
	    private string _searchText = "";
	    private bool _hideStateMsg;
        private CancellationTokenSource _cancelClipboardTidyTask;
	    private ILog _logger;
	    private bool _isInitialised;

	    public ClipboardViewModel(IEventAggregator eventAggregator, ClipboardRepo clipboardRepo)
        {
            _logger = LogManager.GetLogger("ClipboardViewModel");
            _eventAggregator = eventAggregator;
	        _clipboardRepo = clipboardRepo;
	        _eventAggregator.Subscribe(this);
            DisplayName = "Clipboard";

            ClipboardItems = new BindableCollection<ClipboardItemViewModel>();

            _masterList = new List<ClipboardItemViewModel>();

	        HideStateMsg = false;

            var clipboardItems = LoadClipboardItems();
            _masterList.AddRange(clipboardItems);

            SafeAddToClipboardHistory(Clipboard.GetText());
            RebuildClipboardItems();

            HideStateMsg = true;
        }

        private async Task ClipboardTidyAsync(TimeSpan interval, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                if (interval > TimeSpan.Zero)
                    await Task.Delay(interval, token);

                _logger.InfoFormat("Tidying {0}", DateTime.Now);
                foreach (var clipboardItemViewModel in _masterList.Where(x => !x.IsSaved && x.PasteCount > 2))
                {
                    _logger.InfoFormat("Persisting clipboard item:{0}", new object[] {clipboardItemViewModel.DisplayText.Substring(0,100)});
                    _clipboardRepo.TrySaveClipboardItem(clipboardItemViewModel.Instance);
                    _clipboardRepo.Flush();
                }
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

	    protected override void OnViewAttached(object view, object context)
	    {
            _logger.Info("In OnViewAttached");
	        base.OnViewAttached(view, context);
            if (_isInitialised) return;

            _cancelClipboardTidyTask = new CancellationTokenSource();
	        Task.Run(() => 
                ClipboardTidyAsync(TimeSpan.FromMinutes(30), _cancelClipboardTidyTask.Token), 
                _cancelClipboardTidyTask.Token);

	        _isInitialised = true;
	    }

	    private async Task<List<ClipboardItemViewModel>> LoadClipboardItemsAsync()
	    {
	        return await Task.Run(() => _clipboardRepo.Items.Select(x => new ClipboardItemViewModel(x)).ToList());
	    }

	    private List<ClipboardItemViewModel> LoadClipboardItems()
	    {
	        return _clipboardRepo.Items.Select(x => new ClipboardItemViewModel(x)).ToList();
	    }

	    public BindableCollection<ClipboardItemViewModel> ClipboardItems { get; set; }

        public int Order
        {
            get { return 1; }
        }

        public void Handle(ClipboardChangedEvent message)
        {
            if (!Clipboard.ContainsText())
            {
                _logger.Info("Could not read from CB 2");
                return;
            }

            var clipboardText = SafeReadClipboardText();
            if (string.IsNullOrWhiteSpace(clipboardText)) return;

            SafeAddToClipboardHistory(clipboardText);
            RebuildClipboardItems();
        }

        public string SafeReadClipboardText()
        {
            IDataObject clipData;

            try
            {
                clipData = Clipboard.GetDataObject();
            }
            catch (COMException e)
            {
                _logger.WarnFormat("Caught following exception from Clipboard.GetDataObject...");
                _logger.Error(e);
                return null;
            }

            if (clipData == null || !clipData.GetDataPresent(DataFormats.Text))
            {
                _logger.Info("Could not read from CB");
                return null;
            }

            string clipboardText;
            try
            {
                clipboardText = (string)clipData.GetData(DataFormats.Text, false);
                _logger.InfoFormat("read from clipboard: {0}", clipboardText);
            }
            catch (COMException e)
            {
                _logger.Error("Caught exception from clipData.GetData");
                _logger.Error(e);
                return null;
            }
            return clipboardText;
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
            _logger.InfoFormat("Inserted '{0}' at top of _masterList", text);
	        foreach (var model in _masterList)
	        {
                _logger.InfoFormat("\t\t{0}", model.Text);
            }
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
