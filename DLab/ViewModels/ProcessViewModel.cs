using System;
using System.Collections.Generic;
using System.Deployment.Internal;
using System.Diagnostics;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Automation;
using System.Windows.Media;
using Caliburn.Micro;
using DLab.Domain;
using DLab.Infrastructure;
using DLab.Views;
using Newtonsoft.Json;
using Console = System.Console;
using ILog = log4net.ILog;
using LogManager = log4net.LogManager;

namespace DLab.ViewModels
{
    public class ProcessViewModel : Screen, ITabViewModel
    {
        private static readonly List<string> WindowTitles = new List<string>();
        private string _userCommand = "";
        private List<ProcessItem> _processList;
        private ProcessItem _selectedProcessName;
        private ILog _logger;
        private int _pid;
        private IList<ProcessItem> _selectedProcessNames;

        public int Order => 8;
        public BindableCollection<ProcessItem> ProcessNames { get; set; }

        public IEnumerable<ProcessItem> SelectedProcessNames => _processList.Where(x => x.IsSelected);

        public ProcessItem SelectedProcessName
        {
            get { return _selectedProcessName; }
            set
            {
                _selectedProcessName = value;
                NotifyOfPropertyChange();
            }
        }

        public ProcessViewModel()
        {
            _logger = LogManager.GetLogger("ShellView");
            ProcessNames = new BindableCollection<ProcessItem>();
            _pid = Process.GetCurrentProcess().Id;

            _chromeTabs = new List<ProcessItem>();
            _processList = new List<ProcessItem>();

            _mainCancellationTokenSource = new CancellationTokenSource();
            Task.Run(() => StartChromeTabListenerAsync(_mainCancellationTokenSource.Token));
        }

        private async Task StartChromeTabListenerAsync(CancellationToken token)
        {
            await InitialiseProcessListAsync();
            while (!token.IsCancellationRequested)
            {
                await Task.Delay(5000, token);
                UpdateChromeTabsAsync(token);
            }
        }

        private void UpdateChromeTabsAsync(CancellationToken token)
        {

            var chromeTabs = GetChromeTabsAsync(token).Result;
            lock (_gate)
            {
                _chromeTabs.Clear();
                _chromeTabs.AddRange(chromeTabs);
            }
        }

        public string UserCommand
        {
            get { return _userCommand; }
            set
            {
                if (value.Equals(_userCommand, StringComparison.OrdinalIgnoreCase)) { return; }
                _userCommand = value;
                NotifyOfPropertyChange();

                if (string.IsNullOrEmpty(_userCommand))
                {
                    ResetProcessNames();
                    return;
                }

                ProcessNames.Clear();
                foreach (var process in _processList.Where(x => x.IsSelected))
                {
                    process.IsSelected = false;
                }

                var s = _processList
                    .Where(x => x.Name.IndexOf(_userCommand, StringComparison.OrdinalIgnoreCase) > -1 || x.Title.IndexOf(_userCommand, StringComparison.OrdinalIgnoreCase) > -1)
                    .ToList();

                if (!s.Any()) { return;}
                if (s.Count == 1 && !s.First().IsEnabled) { return; }

                ProcessNames.AddRange(s.OrderBy(x => x.ZOrder).ToList());
                SelectedProcessName = ProcessNames.First();

                if (!SelectedProcessName.IsEnabled)
                {
                    SelectedProcessName = ProcessNames[1];
                }

                Task.Run(() => EnhanceWithIconAsync(_mainCancellationTokenSource.Token), _mainCancellationTokenSource.Token);
            }
        }

        private async Task EnhanceWithIconAsync(CancellationToken token)
        {
            var iconHelper = new IconHelper();

            var iconTasks = (from processItem in ProcessNames
                             let item = processItem
                             select Task.Run(() => iconHelper.GetIcon(item, item.Path), token)).ToList();

            while (iconTasks.Count > 0)
            {
                token.ThrowIfCancellationRequested();
                var task = await Task.WhenAny(iconTasks);
                iconTasks.Remove(task);

                var matchedItem = await task;
                if (matchedItem == null) continue;
                matchedItem.Item.Icon = matchedItem.ImageSource;
            }
        }

        private void ResetProcessNames()
        {
            ProcessNames.Clear();
            ProcessNames.AddRange(_processList.OrderBy(x => x.ZOrder));
            Task.Run(() => EnhanceWithIconAsync(_mainCancellationTokenSource.Token), _mainCancellationTokenSource.Token);
        }

        public void RunCommand()
        {
            foreach (var processItem in SelectedProcessNames)
            {
                Debug.WriteLine($"Bringing {processItem.Name} to top");
                BringProcessItemToTop(processItem);
                processItem.IsSelected = false;
            }
            UserCommand = "";
        }

        private void BringProcessItemToTop(ProcessItem processItem)
        {
            if (processItem.Category.Equals("chrome", StringComparison.OrdinalIgnoreCase))
            {
//                var chromeProcess = _processList.FirstOrDefault(x => x.Name.Equals("chrome", StringComparison.OrdinalIgnoreCase));
                BringWindowToTop(processItem);
                BringChromeTabToTop(processItem);
            }
            else
            {
                BringWindowToTop(processItem);
            }
        }

        private void BringWindowToTop(ProcessItem processItem)
        {
            var winPlacement = new Win32.WINDOWPLACEMENT();
            Win32.GetWindowPlacement(processItem.WindowHandle, ref winPlacement);

            Debug.WriteLine($"activating pid:{processItem.Id}, hWnd:{processItem.WindowHandle.ToHex()}");

            if (winPlacement.WindowState == Win32.WindowState.Minimized)
            {
                Win32.RestoreWindow(processItem.WindowHandle);
            }
            else
            {
                Win32.ShowWindow(processItem.WindowHandle);
            }

            Win32.BringWindowToTop(processItem.WindowHandle);
            Win32.SetForegroundWindow(processItem.WindowHandle);

            processItem.LastUsed = DateTime.Now;
        }

        private async void BringChromeTabToTop(ProcessItem processItem)
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new Uri("http://localhost:9000");
                httpClient.DefaultRequestHeaders.Accept.Clear();
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                await httpClient.PostAsync($"set-tab/{processItem.InternalId}", null);
            }
        }

        public async Task InitialiseProcessListAsync()
        {
            var winList = Win32.GetWindowsByZOrder();

            var processList = GetProcessList();

            foreach (var item in processList)
            {
                var matched = winList.FirstOrDefault(x => x.Value.ToInt32() == item.WindowHandle.ToInt32());
                var empty = default(KeyValuePair<int, IntPtr>);

                if (!matched.Equals(empty))
                {
                    item.ZOrder = winList.First(x => x.Value.ToInt32() == item.WindowHandle.ToInt32()).Key;
                }
            }

            if (_processList == null)
            {
                _processList = new List<ProcessItem>();
                _processList.AddRange(processList);
            }
            else
            {
                _processList.RemoveAll(x => x.Category == "chrome");
                ResyncProcessState(processList);
            }

            var currentProcessItem = 
                _processList.FirstOrDefault(x => x.WindowHandle.ToInt32() == ShellView.ClientHwnd.ToInt32()) 
                ?? _processList.FirstOrDefault(x => x.WindowHandle.ToInt32() == winList.First().Value.ToInt32());

            if (currentProcessItem != null)
            {
                currentProcessItem.IsEnabled = false;
            }

            //            await AddChromeTabs(_processList).ConfigureAwait(true);
            lock (_gate)
            {
                if (_chromeTabs.Any())
                {
                    _processList.AddRange(_chromeTabs);
                }
            }

            ProcessNames.Clear();
            ProcessNames.AddRange(_processList.OrderBy(x => x.ZOrder));
            await EnhanceWithIconAsync(_mainCancellationTokenSource.Token);
        }

        protected override void OnDeactivate(bool close)
        {
            _mainCancellationTokenSource.Cancel();
            base.OnDeactivate(close);
        }

        private async Task AddChromeTabs(List<ProcessItem> processList)
        {
            var possibleChromes = processList.Where(x => x.Name.Equals("chrome", StringComparison.OrdinalIgnoreCase));

            var chromeProcess = processList.FirstOrDefault(x => x.Name.Equals("chrome", StringComparison.OrdinalIgnoreCase) && x.IsMainWindow);
            if (chromeProcess == null) { return; }

            using (var httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new Uri("http://localhost:9000");
                httpClient.DefaultRequestHeaders.Accept.Clear();
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var response = await httpClient.GetAsync("gettabinfo");

                if (!response.IsSuccessStatusCode) { return; }

                var jsonString = await response.Content.ReadAsStringAsync();
                var jsonObj = JsonConvert.DeserializeObject<ChromeTabs>(jsonString);

                processList.AddRange(
                    jsonObj.Tabs.Select(tabInfo => 
                        ProcessItem.AddChromeTab(chromeProcess.Id, int.Parse(tabInfo.Id), tabInfo.Title, tabInfo.Url, chromeProcess.WindowHandle, chromeProcess.Path, chromeProcess.LastUsed)));
            }
        }

        private readonly object _gate = new object();

        private async Task<IEnumerable<ProcessItem>> GetChromeTabsAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            var chromeProcess = _processList.FirstOrDefault(x => x.Name.Equals("chrome", StringComparison.OrdinalIgnoreCase) && x.IsMainWindow);
            if (chromeProcess == null) { return Enumerable.Empty<ProcessItem>(); }

            using (var httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new Uri("http://localhost:9000");
                httpClient.DefaultRequestHeaders.Accept.Clear();
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                HttpResponseMessage response;
                try
                {
                    response = await httpClient.GetAsync("gettabinfo", new CancellationTokenSource(1000).Token);
                }
                catch (OperationCanceledException e)
                {
                    _logger.Warn("Timeout calling http gettabinfo");
                    return Enumerable.Empty<ProcessItem>();
                }

                if (!response.IsSuccessStatusCode)
                {
                    _logger.Warn($"Failed calling HTTP gettabinfo: {response.StatusCode}");
                    return Enumerable.Empty<ProcessItem>();
                }

                var jsonString = await response.Content.ReadAsStringAsync();
                var jsonObj = JsonConvert.DeserializeObject<ChromeTabs>(jsonString);

                return 
                    jsonObj.Tabs.Select(tabInfo =>
                        ProcessItem.AddChromeTab(chromeProcess.Id, int.Parse(tabInfo.Id), tabInfo.Title, tabInfo.Url, chromeProcess.WindowHandle, chromeProcess.Path, chromeProcess.LastUsed));
            }
        }

        private void ResyncProcessState(IList<ProcessItem> currentProcessList)
        {
            var deadProcessIds = _processList.Where(x => !currentProcessList.Select(y => y.Id).Contains(x.Id)).ToList();
            foreach (var deadProcessItem in deadProcessIds)
            {
                _processList.Remove(deadProcessItem);
            }

            foreach (var processItem in _processList)
            {
                var currentProcessItem = currentProcessList.FirstOrDefault(x => x.Id == processItem.Id && x.WindowHandle == processItem.WindowHandle);

                if (currentProcessItem == null)
                {
                    Debug.WriteLine($"Could not find currentProcessItem for: {processItem}");
                    continue;
                }

                processItem.Title = currentProcessItem.Title;
                processItem.ZOrder = currentProcessItem.ZOrder;
                processItem.IsEnabled = true;
                processItem.IsSelected = false;
            }

            foreach (var processItem in currentProcessList.Where(x => _processList.All(y => y.Id != x.Id && y.WindowHandle != x.WindowHandle)))
            {
                _processList.Add(processItem);
            }
        }

        private IList<ProcessItem> GetProcessList()
        {
            var processlist = Process.GetProcesses().ToList();

            var pl = processlist
                .Where(x => !string.IsNullOrEmpty(x.MainWindowTitle) && x.Id != _pid)
                .Select(x => 
                    new ProcessItem(
                        x.Id,
                        x.MainWindowTitle, 
                        x.ProcessName, 
                        x.MainWindowHandle, 
                        x.MainModule.FileName,
                        x.StartTime))
                .ToList();

            var chrome = pl.FirstOrDefault(x => x.Name == "chrome");
            if (chrome != null)
            {
                var chromeMainHwnd = chrome.WindowHandle;
                _tempItems.Clear();
                Debug.WriteLine($"chrome pid:{chrome.Id}");
                var hWnds = Win32.EnumerateProcessWindowHandles(chrome.Id);

                pl.Remove(chrome);

                foreach (var hWnd in hWnds)
                {
                    var sb = new StringBuilder();
                    var captionLength = Win32.GetWindowTextLength(hWnd);
                    Debug.WriteLine($"captionLength:{captionLength}");

                    if (captionLength > 0)
                    {
                        sb.Append(Win32.GetWindowTextRaw(hWnd));
                    }

                    try
                    {
                        var wStyle = Win32.GetWindowLongPtr(hWnd, (int) Win32.WindowLongFlags.GWL_STYLE);
                        var isVisible = (wStyle.ToInt64() & Win32.WS_VISIBLE.ToInt64()) != 0;
                        Debug.WriteLine($"hWnd:{hWnd.ToHex()}, Caption:{sb}, IsVisible:{isVisible}");

                        if (isVisible)
                        {
                            _tempItems.Add(new ProcessItem(chrome.Id, sb.ToString(), "chrome", hWnd, chrome.Path, DateTime.Now, hWnd==chromeMainHwnd));
                        }

                    }
                    catch (Exception e)
                    {
                        
                    }
                }
//
//                _tempItems.Clear();
//                Debug.WriteLine($"chrome MainWindowHandle:{chrome.WindowHandle.ToHex()}");
////                Win32.EnumChildWindows(chrome.MainWindowHandle, EnumWindowCallback, IntPtr.Zero);
                pl.AddRange(_tempItems);
            }

            return pl;
        }

        private List<ProcessItem> _tempItems = new List<ProcessItem>();
        private CancellationTokenSource _chromeTabCancellationToken;
        private List<ProcessItem> _chromeTabs;
        private CancellationTokenSource _mainCancellationTokenSource;

        public bool EnumWindowCallback(IntPtr windowHandle, IntPtr lParam)
        {
            var parentHwnd = Win32.GetParent(windowHandle);
            uint pid;
            var threadId = Win32.GetWindowThreadProcessId(windowHandle, out pid);
            var sb = new StringBuilder();
            Win32.GetWindowText(windowHandle, sb, Win32.GetWindowTextLength(windowHandle)+1);

//            var sbp = new StringBuilder();
//            Win32.GetWindowText(parentHwnd, sbp, Win32.GetWindowTextLength(parentHwnd)+1);

            Debug.WriteLine($"hWnd:{windowHandle.ToHex()}, parent:{parentHwnd.ToHex()}");
            Debug.WriteLine($"title:{sb}");
//            Debug.WriteLine($"parent title:{sbp}");
            Debug.WriteLine($"creator thread:{threadId}");
            Debug.WriteLine($"pid:{pid}");

            _tempItems.Add(new ProcessItem((int) pid, sb.ToString(), "chrome", windowHandle, "kjheljke", DateTime.Now));
            Debug.WriteLine($"Adding pid={pid}, title={sb}");
            return true;
        }

        public static List<string> GetWindowTitles(bool includeChildren)
        {
            WindowTitles.Clear();
            Win32.EnumWindows(EnumWindowsCallback, includeChildren ? (IntPtr)1 : IntPtr.Zero);
            return WindowTitles;
        }

        private static bool EnumWindowsCallback(IntPtr testWindowHandle, IntPtr includeChildren)
        {
            var title = GetWindowTitle(testWindowHandle);
            if (string.IsNullOrWhiteSpace(title)) { return true; }

            WindowTitles.Add(title);

            if (includeChildren.Equals(IntPtr.Zero) == false)
            {
                Win32.EnumChildWindows(testWindowHandle, EnumWindowsCallback, IntPtr.Zero);
            }
            return true;
        }

        private static string GetWindowTitle(IntPtr windowHandle)
        {
            const int SMTO_ABORTIFHUNG = 0x0002;
            const int WM_GETTEXT = 0xD;
            const int MAX_STRING_SIZE = 32768;
            IntPtr result;
            var title = string.Empty;
            var memoryHandle = Marshal.AllocCoTaskMem(MAX_STRING_SIZE);
            Marshal.Copy(title.ToCharArray(), 0, memoryHandle, title.Length);
            Win32.SendMessageTimeout(windowHandle, WM_GETTEXT, (IntPtr)MAX_STRING_SIZE, memoryHandle, SMTO_ABORTIFHUNG, (uint)1000, out result);
            title = Marshal.PtrToStringAuto(memoryHandle);
            Marshal.FreeCoTaskMem(memoryHandle);
            return title;
        }

        public static void DoIt()
        {
            // there are always multiple chrome processes, so we have to loop through all of them to find the
            // process with a Window Handle and an automation element of name "Address and search bar"
            Process[] procsChrome = Process.GetProcessesByName("chrome");
            foreach (Process chrome in procsChrome)
            {
                // the chrome process must have a window
                if (chrome.MainWindowHandle == IntPtr.Zero)
                {
                    continue;
                }

                // find the automation element
                AutomationElement elm = AutomationElement.FromHandle(chrome.MainWindowHandle);

                // manually walk through the tree, searching using TreeScope.Descendants is too slow (even if it's more reliable)
                AutomationElement elmUrlBar = null;
                try
                {
                    // walking path found using inspect.exe (Windows SDK) for Chrome 31.0.1650.63 m (currently the latest stable)
                    var elm1 = elm.FindFirst(TreeScope.Children, new PropertyCondition(AutomationElement.NameProperty, "Google Chrome"));
                    if (elm1 == null) { continue; } // not the right chrome.exe
                                                    // here, you can optionally check if Incognito is enabled:
                                                    //bool bIncognito = TreeWalker.RawViewWalker.GetFirstChild(TreeWalker.RawViewWalker.GetFirstChild(elm1)) != null;
                    var elm2 = TreeWalker.RawViewWalker.GetLastChild(elm1); // I don't know a Condition for this for finding :(
                    var elm3 = elm2.FindFirst(TreeScope.Children, new PropertyCondition(AutomationElement.NameProperty, ""));
                    var elm4 = elm3.FindFirst(TreeScope.Children, new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Tab));
                    elmUrlBar = elm4.FindFirst(TreeScope.Children, new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Custom));
                }
                catch
                {
                    // Chrome has probably changed something, and above walking needs to be modified. :(
                    // put an assertion here or something to make sure you don't miss it
                    continue;
                }

                // make sure it's valid
                if (elmUrlBar == null)
                {
                    // it's not..
                    continue;
                }

                // elmUrlBar is now the URL bar element. we have to make sure that it's out of keyboard focus if we want to get a valid URL
                if ((bool)elmUrlBar.GetCurrentPropertyValue(AutomationElement.HasKeyboardFocusProperty))
                {
                    continue;
                }

                // there might not be a valid pattern to use, so we have to make sure we have one
                AutomationPattern[] patterns = elmUrlBar.GetSupportedPatterns();
                if (patterns.Length == 1)
                {
                    string ret = "";
                    try
                    {
                        ret = ((ValuePattern)elmUrlBar.GetCurrentPattern(patterns[0])).Current.Value;
                    }
                    catch { }
                    if (ret != "")
                    {
                        // must match a domain name (and possibly "https://" in front)
                        if (Regex.IsMatch(ret, @"^(https:\/\/)?[a-zA-Z0-9\-\.]+(\.[a-zA-Z]{2,4}).*$"))
                        {
                            // prepend http:// to the url, because Chrome hides it if it's not SSL
                            if (!ret.StartsWith("http"))
                            {
                                ret = "http://" + ret;
                            }
                            Debug.WriteLine("Open Chrome URL found: '" + ret + "'");
                        }
                    }
                    continue;
                }
            }
        }
    }

    public class ChromeTabs
    {
        public List<ChromeTabInfo> Tabs { get; set; }
    }

    public class ChromeTabInfo
    {
        public string Id { get; set; }
        public string Url { get; set; }
        public string Title { get; set; }
    }

    internal class ProcessInfo
    {
        public ProcessInfo(int id)
        {
            Id = id;
            LastUsed = DateTime.Now;
        }

        public int Id { get; set; } 
        public DateTime LastUsed { get; set; }
    }

    public class ProcessItem : ViewAware, IIconable
    {
        public ProcessItem(int id, string title, string name, IntPtr windowHandle, string path, DateTime startTime, bool isMainWindow=true)
        {
            Id = id;
            Title = title;
            Name = name;
            WindowHandle = windowHandle;
            Path = path;
            StartTime = startTime;
            LastUsed = StartTime;
            IsEnabled = true;
            Category = "singlewindowprocess";
            IsMainWindow = isMainWindow;
        }

        public static ProcessItem AddChromeTab(int id, int internalId, string title, string name, IntPtr mainWindowHandle, string path, DateTime startTime)
        {
            var pi = new ProcessItem(id, title, name, mainWindowHandle, path, startTime) {Category = "chrome", InternalId = internalId};
            return pi;
        }

        private ImageSource _icon;
        private bool _isSelected;
        private bool _isEnabled;
        public string Title { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public DateTime StartTime { get; set; }
        public IntPtr WindowHandle { get; set; }
        public int Id { get; set; }
        public int ZOrder { get; set; }
        public DateTime LastUsed { get; set; }
        public string Category { get; set; }
        public int InternalId { get; set; }
        public bool IsMainWindow { get; set; }

        public bool IsEnabled
        {
            get { return _isEnabled; }
            set
            {
                _isEnabled = value;
                NotifyOfPropertyChange();
            }
        }

        public override string ToString()
        {
            return $"Title: {Title}, Name: {Name}, WindowHandle: {WindowHandle}, Id: {Id}, Category: {Category}, InternalId: {InternalId}, IsMainWindow: {IsMainWindow}";
        }

        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                _isSelected = value;
                NotifyOfPropertyChange();
            }
        }

        public ImageSource Icon
        {
            get { return _icon; }
            set
            {
                _icon = value;
                NotifyOfPropertyChange();
            }
        }
    }

    internal class DescendingDateComparer : IComparer<ProcessItem>
    {
        public int Compare(ProcessItem x, ProcessItem y)
        {
            var ascendingResult = Comparer<DateTime>.Default.Compare(x.LastUsed, y.LastUsed);
            return 0 - ascendingResult;
        }
    }

    public static class TypeExtensions
    {
        public static string ToHex(this IntPtr intPtr)
        {
            return intPtr.ToInt64().ToString("X8");
        }

        public static string ToHex(this int someInt)
        {
            return someInt.ToString("X8");
        }
    }
}