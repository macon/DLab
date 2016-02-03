using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Media;
using Caliburn.Micro;
using DLab.Domain;
using DLab.Infrastructure;
using DLab.Views;
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

                var s = _processList.Where(x => x.Name.IndexOf(_userCommand, StringComparison.OrdinalIgnoreCase) > -1).ToList();
                if (!s.Any()) { return;}
                if (s.Count == 1 && !s.First().IsEnabled) { return; }

                ProcessNames.AddRange(s.OrderBy(x => x.ZOrder).ToList());
                SelectedProcessName = ProcessNames.First();

                if (!SelectedProcessName.IsEnabled)
                {
                    SelectedProcessName = ProcessNames[1];
                }
                EnhanceWithIconAsync();
            }
        }

        private async Task EnhanceWithIconAsync()
        {
            var i = new IconHelper();

            var iconTasks = (from processItem in ProcessNames
                             let item = processItem
                             select Task.Run(() => i.GetIcon(item, item.Path))).ToList();

            while (iconTasks.Count > 0)
            {
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
            EnhanceWithIconAsync();
        }

        public void RunCommand()
        {
            foreach (var processItem in SelectedProcessNames)
            {
                BringWindowToTop(processItem);
            }
        }

        private void BringWindowToTop(ProcessItem processItem)
        {
            var winPlacement = new Win32.WINDOWPLACEMENT();
            Win32.GetWindowPlacement(processItem.MainWindowHandle, ref winPlacement);

            if (winPlacement.WindowState == Win32.WindowState.Minimized)
            {
                Win32.RestoreWindow(processItem.MainWindowHandle);
            }
            else
            {
                Win32.ShowWindow(processItem.MainWindowHandle);
            }

            Win32.BringWindowToTop(processItem.MainWindowHandle);
            Win32.SetForegroundWindow(processItem.MainWindowHandle);

            processItem.LastUsed = DateTime.Now;
        }

        public async void InitialiseProcessListAsync()
        {
            var tmpList = Win32.GetWindowsByZOrder();

            var processList = GetProcessList();

            foreach (var item in processList)
            {
                item.ZOrder = tmpList.First(x => x.Value.ToInt32() == item.MainWindowHandle.ToInt32()).Key;
            }

            if (_processList == null)
            {
                _processList = new List<ProcessItem>();
                _processList.AddRange(processList);
            }
            else
            {
                ResyncProcessState(processList);
            }

            var currentProcessItem = _processList.FirstOrDefault(x => x.MainWindowHandle.ToInt32() == ShellView.ClientHwnd.ToInt32());
            if (currentProcessItem != null)
            {
                currentProcessItem.IsEnabled = false;
            }
            else
            {
                currentProcessItem =
                    _processList.FirstOrDefault(x => x.MainWindowHandle.ToInt32() == tmpList.First().Value.ToInt32());
                if (currentProcessItem != null)
                {
                    currentProcessItem.IsEnabled = false;
                }
            }

            ProcessNames.Clear();
            ProcessNames.AddRange(_processList.OrderBy(x => x.ZOrder));
            await EnhanceWithIconAsync();
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
                var currentProcessItem = currentProcessList.First(x => x.Id == processItem.Id);
                processItem.Title = currentProcessItem.Title;
                processItem.ZOrder = currentProcessItem.ZOrder;
                processItem.IsEnabled = true;
                processItem.IsSelected = false;
            }

            foreach (var processItem in currentProcessList.Where(x => _processList.All(y => y.Id != x.Id)))
            {
                _processList.Add(processItem);
            }
        }

        private IList<ProcessItem> GetProcessList()
        {
            var processlist = Process.GetProcesses().ToList();

            return processlist
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
        public ProcessItem(int id, string title, string name, IntPtr mainWindowHandle, string path, DateTime startTime)
        {
            Id = id;
            Title = title;
            Name = name;
            MainWindowHandle = mainWindowHandle;
            Path = path;
            StartTime = startTime;
            LastUsed = StartTime;
            IsEnabled = true;
        }

        private ImageSource _icon;
        private bool _isSelected;
        private bool _isEnabled;
        public string Title { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public DateTime StartTime { get; set; }
        public IntPtr MainWindowHandle { get; set; }
        public int Id { get; set; }
        public int ZOrder { get; set; }
        public DateTime LastUsed { get; set; }

        public bool IsEnabled
        {
            get { return _isEnabled; }
            set
            {
                _isEnabled = value;
                NotifyOfPropertyChange();
            }
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
}