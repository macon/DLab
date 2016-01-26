using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Caliburn.Micro;
using DLab.Domain;
using DLab.Infrastructure;

namespace DLab.ViewModels
{
    public class ProcessViewModel : Screen, ITabViewModel
    {
        public int Order => 8;

        public BindableCollection<ProcessItem> ProcessNames { get; set; }
        private ProcessItem _selectedProcessName;

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
            ProcessNames = new BindableCollection<ProcessItem>();
        }

        protected override void OnActivate()
        {
            base.OnActivate();
            InitialiseProcessNames();
        }

        public string UserCommand
        {
            get { return _userCommand; }
            set
            {
                if (value.Equals(_userCommand, StringComparison.OrdinalIgnoreCase)) { return; }
                _userCommand = value;
                NotifyOfPropertyChange();

                ProcessNames.Clear();

                if (string.IsNullOrEmpty(_userCommand))
                {
                    ResetProcessNames();
                    return;
                }

                var s = _processList.Where(x => x.Name.IndexOf(_userCommand, StringComparison.OrdinalIgnoreCase) > -1).ToList();
                if (!s.Any()) { return;}

                ProcessNames.AddRange(s.ToList());
                SelectedProcessName = ProcessNames.First();
                EnhanceWithIconAsync();
            }
        }

        private async void EnhanceWithIconAsync()
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
            ProcessNames.AddRange(_processList);
            EnhanceWithIconAsync();
        }

        public void RunCommand()
        {
            Win32.BringWindowToTop(SelectedProcessName.MainWindowHandle);
            Win32.SetForegroundWindow(SelectedProcessName.MainWindowHandle);
            Win32.RestoreWindow(SelectedProcessName.MainWindowHandle);
        }

        private async void InitialiseProcessNames()
        {
            var t = Task.Run(() => InitialiseProcessNamesAsync());
            await t;
            _processList = t.Result;
            ProcessNames.Clear();
            ProcessNames.AddRange(_processList);
            EnhanceWithIconAsync();
        }

        private IEnumerable<ProcessItem> InitialiseProcessNamesAsync()
        {
            var processlist = Process.GetProcesses().ToList();

            return processlist
                .Where(x => !string.IsNullOrEmpty(x.MainWindowTitle))
                .Select(x => 
                    new ProcessItem(
                        x.MainWindowTitle, 
                        x.ProcessName, 
                        x.MainWindowHandle, 
                        x.MainModule.FileName));
        }

        private static readonly List<string> WindowTitles = new List<string>();
        private string _userCommand = "";
        private IEnumerable<ProcessItem> _processList;

        public static List<string> GetWindowTitles(bool includeChildren)
        {
            Win32.EnumWindows(EnumWindowsCallback, includeChildren ? (IntPtr)1 : IntPtr.Zero);
            return WindowTitles;
        }

        private static bool EnumWindowsCallback(IntPtr testWindowHandle, IntPtr includeChildren)
        {
            var title = GetWindowTitle(testWindowHandle);
            WindowTitles.Add(title);

            if (includeChildren.Equals(IntPtr.Zero) == false)
            {
                Win32.EnumChildWindows(testWindowHandle, EnumWindowsCallback, IntPtr.Zero);
            }
            return true;
        }

        private static string GetWindowTitle(IntPtr windowHandle)
        {
            uint SMTO_ABORTIFHUNG = 0x0002;
            uint WM_GETTEXT = 0xD;
            int MAX_STRING_SIZE = 32768;
            IntPtr result;
            string title = string.Empty;
            IntPtr memoryHandle = Marshal.AllocCoTaskMem(MAX_STRING_SIZE);
            Marshal.Copy(title.ToCharArray(), 0, memoryHandle, title.Length);
            Win32.SendMessageTimeout(windowHandle, WM_GETTEXT, (IntPtr)MAX_STRING_SIZE, memoryHandle, SMTO_ABORTIFHUNG, (uint)1000, out result);
            title = Marshal.PtrToStringAuto(memoryHandle);
            Marshal.FreeCoTaskMem(memoryHandle);
            return title;
        }
    }

    public class ProcessItem : ViewAware, IIconable
    {
        public ProcessItem(string title, string name, IntPtr mainWindowHandle, string path)
        {
            Title = title;
            Name = name;
            MainWindowHandle = mainWindowHandle;
            Path = path;
        }

        private ImageSource _icon;
        public string Title { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public IntPtr MainWindowHandle { get; set; }
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
}