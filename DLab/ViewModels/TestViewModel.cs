using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Windows;
using Caliburn.Micro;
using DLab.Domain;
using DLab.HyperJump;
using DLab.Infrastructure;
using ILog = log4net.ILog;

namespace DLab.ViewModels
{
    public class DistinctFolderComparer : IEqualityComparer<Folder>
    {
        public bool Equals(Folder x, Folder y)
        {
            return x.FullPath.Equals(y.FullPath, StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode(Folder obj)
        {
            return obj.FullPath.GetHashCode();
        }
    }

    public class TestViewModel : Screen, ITabViewModel
    {
        private readonly HyperjumpRepo _hyperjumpRepo;
        private readonly IRepository<Domain.Console> _consoleRepo;
        private bool _scanning;
        private string _userCommand;
        public int Order => 9;
        private Scanner _scanner;
        private ObservablePropertyBacking<string> _textInput = new ObservablePropertyBacking<string>();
        private string _rootFolder = "";
        private ILog _log;

        public ObservableCollection<HyperJumpFolderViewModel> Items { get; set; }

        public TestViewModel(HyperjumpRepo hyperjumpRepo, IRepository<Domain.Console> consoleRepo, IAppServices appServices)
        {
            _log = appServices.Log;
            _hyperjumpRepo = hyperjumpRepo;
            _consoleRepo = consoleRepo;
            Items = new ObservableCollection<HyperJumpFolderViewModel>();
            _scanner = new Scanner(hyperjumpRepo, appServices);
            Rescan();
//            RootFolder = "d:\\";

            _textInput
                .Throttle(TimeSpan.FromMilliseconds(300))
                .Do(s => Debug.WriteLine(s))
                .SubscribeOn(NewThreadScheduler.Default)
                .ObserveOn(SynchronizationContext.Current)
                .Subscribe(ParseCommand);
        }

//        public string RootFolder
//        {
//            get { return _rootFolder; }
//            set
//            {
//                if (_rootFolder.ToLower().Equals(value.ToLower())) return;
//                if (!Directory.Exists(value)) return;
//                _rootFolder = value;
////                Scanning = !Scanning;
//                //                Thread.Sleep(500);
//                Rescan();
//                NotifyOfPropertyChange();
//            }
//        }

        public async void Rescan()
        {
            Scanning = true;
            Debug.WriteLine($"Pre-scan {Thread.CurrentThread.ManagedThreadId}");

            await _scanner.ScanAsync();

            Debug.WriteLine($"Post-scan {Thread.CurrentThread.ManagedThreadId}");
            Scanning = false;
        }


        //        public string UserCommand
        //        {
        //            get { return _userCommand; }
        //            set
        //            {
        //                _userCommand = value;
        //                Scanning = !Scanning;
        //                NotifyOfPropertyChange();
        //            }
        //        }
        public string UserCommand
        {
            get { return _textInput.Value; }
            set
            {
                if (value == _textInput.Value) return;
                _textInput.Value = value;
                NotifyOfPropertyChange();
            }
        }

        public bool Scanning
        {
            get { return _scanning; }
            set
            {
                _scanning = value;
                NotifyOfPropertyChange();
            }
        }

        private void ParseCommand(string userCommand)
        {
            if (string.IsNullOrEmpty(userCommand))
            {
                Items.Clear();
                return;
            }

            var parts = userCommand.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length ==0) { return; }

            var masterList = parts
                                .Where(x => x.Length > 1)
                                .Select((x, i) => GetHighestFolderMatches(x, i == 0))
                                .ToList();

            if (!masterList.Any()) { return; }

            var drivingSet = masterList.Last();
            var survivors = new List<FolderMatch>();

            foreach (var folder in drivingSet)
            {
                var orphaned = false;

                for (var parentLevel = masterList.Count - 2; parentLevel >= 0; parentLevel--)
                {
                    if (masterList[parentLevel].Any(pf => IsChild(folder.MatchedFolder, pf.MatchedFolder))) continue;

                    orphaned = true;
                    break;
                }
                if (!orphaned) { survivors.Add(folder); }
            }

            Items.Clear();
            foreach (var item in survivors.OrderBy(f => f.MatchedFolder.Level).ThenBy(f => f.Position).Take(200))
            {
                Items.Add(new HyperJumpFolderViewModel(item.MatchedFolder, parts));
            }
            SelectedItem = Items.FirstOrDefault();
        }

        public HyperJumpFolderViewModel SelectedItem { get; set; }

        private bool IsChild(Folder child, Folder parent)
        {
            if (parent.Lineage.Count >= child.Lineage.Count) { return false; }

            for (var i = 0; i < parent.Lineage.Count; i++)
            {
                if (parent.Lineage[i] != child.Lineage[i])
                {
                    return false;
                }
            }
            return true;
        }

        private List<FolderMatch> GetHighestFolderMatches(string firstPart, bool removeDescendants)
        {
            var initialFolders = _scanner.FolderLookup
                .Where(l => l.Key.IndexOf(firstPart, StringComparison.OrdinalIgnoreCase) >= 0)
                .SelectMany(l => l)
                .Distinct(new DistinctFolderComparer())
                .Select(f => new FolderMatch { MatchedFolder = f, Position = f.Name.IndexOf(firstPart, StringComparison.OrdinalIgnoreCase) })
                .ToList();

            if (!removeDescendants) { return initialFolders; }

            var redundantFolders = new List<FolderMatch>();

            foreach (var folder in initialFolders.OrderBy(f => f.MatchedFolder.Level))
            {
                var childFolders = initialFolders.Where(f => f.MatchedFolder.Drive == folder.MatchedFolder.Drive &&
                                                             f.MatchedFolder.Level > folder.MatchedFolder.Level &&
                                                             f.MatchedFolder.Lineage[folder.MatchedFolder.Level - 1] ==
                                                             folder.MatchedFolder.Lineage[folder.MatchedFolder.Level - 1]).ToList();
                if (childFolders.Count == 0) { continue; }

                foreach (var f in childFolders)
                {
                    initialFolders.Remove(f);
                }

//                redundantFolders.AddRange(childFolders);
            }

//            foreach (var folder in redundantFolders)
//            {
//                initialFolders.Remove(folder);
//            }

            return initialFolders;
        }

        public void DoCommand(char key)
        {
            var console = _consoleRepo.Items.FirstOrDefault(x => x.Hotkey == key);
            if (console == null) { return; }

            var di = new DirectoryInfo(SelectedItem.FullPath);
            var drive = di.Root.Name.TrimEnd('\\');

            var args0 = console.Arguments.Replace("{DRIVE}", drive);
            var args = string.Format(args0, SelectedItem.FullPath);

            var psi = new ProcessStartInfo(string.Format(console.Target, SelectedItem.FullPath))
            {
                Arguments = args,
                UseShellExecute = false,
                Verb = "runas"
            };

            try
            {
                Process.Start(psi);
            }
            catch (Win32Exception e)
            {
                MessageBox.Show(e.Message);
                _log.Error(e);
            }
        }
    }
}
