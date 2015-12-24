using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using Caliburn.Micro;
using DLab.Domain;
using DLab.Infrastructure;
using DLab.Repositories;
using DLab.Views;
using log4net;
using Console = System.Console;
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

            SafeAddToClipboardHistory(BuildViewModelFromClipboard());
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

        public int Order => 1;

	    public void Handle(ClipboardChangedEvent message)
        {
	        try
	        {
                if (!CanHandleClipboardData())
	            {
	                var clipboardData = Clipboard.GetDataObject();
	                var formats = string.Join(",", clipboardData.GetFormats());
	                _logger.Info($"Cannot handle clipboard data type: {formats}");
	                return;
	            }

	            var vm = BuildViewModelFromClipboard();
	            if (vm == null) return;

	            SafeAddToClipboardHistory(vm);
	            RebuildClipboardItems();
	        }
	        catch (COMException exception)
	        {
                _logger.WarnFormat("Caught following exception within Handle(ClipboardChangedEvent message) operation...");
                _logger.Error(exception);
                HandleClipboardException();
            }
        }

	    private bool CanHandleClipboardData()
	    {
	        var clipboardData = Clipboard.GetDataObject();

	        if (Clipboard.ContainsImage())
	        {
	            var img = Clipboard.GetImage();
	        }

	        return clipboardData != null && 
                (clipboardData.GetDataPresent(DataFormats.Text) || 
                clipboardData.GetDataPresent(DataFormats.StringFormat) ||
                clipboardData.GetDataPresent(DataFormats.FileDrop) ||
                Clipboard.ContainsImage());
	    }

	    public ClipboardItemViewModel BuildViewModelFromClipboard()
        {
	        try
	        {
	            var clipData = Clipboard.GetDataObject();
	            if (clipData == null) { return null;}

	            if (clipData.GetDataPresent(DataFormats.Text))
	            {
	                var clipboardText = (string)clipData.GetData(DataFormats.Text, true);
	                return ClipboardItemViewModel.ByText(clipboardText);
	            }

	            if (clipData.GetDataPresent(DataFormats.FileDrop))
	            {
	                var collection = (string[])clipData.GetData(DataFormats.FileDrop, true);
	                return ClipboardItemViewModel.ByFileDropList(collection);
	            }

	            if (Clipboard.ContainsImage())
	            {
	                var bmi = Clipboard.GetImage();
	                return ClipboardItemViewModel.ByImage(bmi);
	            }
	            return null;
	        }
	        catch (COMException)
	        {
	            var formats = Clipboard.GetDataObject().GetFormats();
                _logger.Debug($"Caught {nameof(COMException)}, dumping formats on clipboard...");
	            foreach (var format in formats)
	            {
	                _logger.Debug($"{format}");
	            }
	            throw;
	        }
        }

	    private void SafeAddToClipboardHistory(ClipboardItemViewModel viewModel)
	    {
            if (viewModel == null) return;

            if (_masterList.Any(x => x.DataType == viewModel.DataType &&
                                x.Text.Equals(viewModel.Text, StringComparison.InvariantCultureIgnoreCase)))
            {
                BringItemToTop(viewModel.Text);
                return;
            }
            _masterList.Insert(0, viewModel);
	    }

	    public void SetClipboardBlind(ClipboardItemViewModel viewModel)
        {
            if (ClipboardAlreadySet(viewModel)) return;

	        try
	        {
                ActiveClipboardString = viewModel.Text;
                Clipboard.SetDataObject(viewModel.GetClipboardData());
            }
            catch (COMException e)
            {
                _logger.Error(e);
            }
        }

	    private bool ClipboardAlreadySet(ClipboardItemViewModel viewModel)
	    {
	        var clipboard = BuildViewModelFromClipboard();

	        return clipboard.Text == viewModel.Text
	               && clipboard.DataType == viewModel.DataType;
	    }

	    public string ActiveClipboardString { get; private set; }

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
	        try
	        {
	            Debug.WriteLine("clipboard text: {0}", new object[] {SelectedClipboardItem});
	            SetClipboardBlind(SelectedClipboardItem);
	            SelectedClipboardItem.PasteCount++;
	            ReportPasteCounts();

	            var title = Win32.GetWindowTitle(ShellView.ClientHwnd);
	            Debug.WriteLine("target window title: {0}", new object[] { title });

	            Win32.SendPasteToClient(ShellView.ClientHwnd);
	            BringItemToTop(SelectedClipboardItem.Text);
	            _eventAggregator.Publish(new UserActionEvent(), Execute.OnUIThread);
	        }
	        catch (COMException exception)
	        {
                _logger.WarnFormat("Caught following exception within Paste operation...");
                _logger.Error(exception);
                HandleClipboardException();
	        }
        }

	    private static void HandleClipboardException()
	    {
	        MessageBox.Show("Failed to perform Clipboard operation");
	    }

	    private void ReportPasteCounts()
	    {
            Debug.WriteLine("Paste counts:");
            foreach (var model in _masterList)
	        {
	            Debug.WriteLine("'{0}' --> {1}", new object[]{model.DisplayText, model.PasteCount});
	        }
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
                        clipboardText = (string)clipData.GetData(DataFormats.Text, false);
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
            }

            else if (Clipboard.ContainsFileDropList())
            {
                // we have a file drop list in the clipboard
                var fl = System.Windows.Clipboard.GetFileDropList();
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
                enc.Frames.Add(BitmapFrame.Create(System.Windows.Clipboard.GetImage()));
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
	        SelectedClipboardItem = ClipboardItems.FirstOrDefault();
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
