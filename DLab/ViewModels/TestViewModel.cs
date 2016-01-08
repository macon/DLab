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
using System.Threading.Tasks;
using System.Windows;
using Caliburn.Micro;
using DLab.Domain;
using DLab.HyperJump;
using DLab.Infrastructure;
using Console = System.Console;
using ILog = log4net.ILog;
using LogManager = log4net.LogManager;

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
        private CancellationTokenSource _cts;

//        public ObservableCollection<HyperJumpFolderViewModel> MatchedItems { get; set; }
        public BindableCollection<HyperJumpFolderViewModel> MatchedItems { get; set; }

        public HyperJumpFolderViewModel SelItem
        {
            get
            {
                return _selItem;
            }
            set
            {
                _selItem = value;
            }
        }

        public TestViewModel(HyperjumpRepo hyperjumpRepo, IRepository<Domain.Console> consoleRepo, IAppServices appServices)
        {
            _log = appServices.Log;
            _hyperjumpRepo = hyperjumpRepo;
            _consoleRepo = consoleRepo;
            _previousResults = new List<FolderMatches>();
            MatchedItems = new BindableCollection<HyperJumpFolderViewModel>();
//            MatchedItems = new ObservableCollection<HyperJumpFolderViewModel>();
            _scanner = new Scanner(hyperjumpRepo, appServices);
            Rescan();

            _textInput
                .Throttle(TimeSpan.FromMilliseconds(300))
                .Do(s => Debug.WriteLine(s))
                .Do(s => CancelCurrentScan())
                .SubscribeOn(NewThreadScheduler.Default)
                .ObserveOn(SynchronizationContext.Current)
                .Subscribe(ParseCommand);
        }

        private void CancelCurrentScan()
        {
            if (_cts == null) return;
            _cts.Cancel();
            _cts = null;
            _log.Debug("request cancellation");
        }

        public async void Rescan()
        {
            Scanning = true;
            Debug.WriteLine($"Pre-scan {Thread.CurrentThread.ManagedThreadId}");

            await _scanner.ScanAsync();

            Debug.WriteLine($"Post-scan {Thread.CurrentThread.ManagedThreadId}");
            Scanning = false;
        }

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

        private async void ParseCommand(string userCommand)
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();

            MatchedItems.Clear();

            if (string.IsNullOrEmpty(userCommand))
            {
                return;
            }

            _log.Debug($"ParseCommand('{userCommand}')");

            var parts = userCommand.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Where(p => p.Length > 1).ToArray();

            //            Task.Run(() => ParseCommandAsync(userCommand, _cts.Token), _cts.Token);

            List<FolderMatch> survivors;
            try
            {
                survivors = await ParseCommandAsync(parts, _cts.Token);
            }
            catch (OperationCanceledException)
            {
                _log.Debug($"operation cancelled. search args:{userCommand}");
                return;
            }
            if (survivors == null) { return;}

            foreach (var item in survivors.OrderBy(f => f.MatchedFolder.Level).ThenBy(f => f.Position).Take(200))
            {
                MatchedItems.Add(new HyperJumpFolderViewModel(item.MatchedFolder, parts));
            }
            SelectedMatchedItem = MatchedItems.FirstOrDefault();
        }

        private List<FolderMatches> _previousResults;
        private HyperJumpFolderViewModel _selectedMatchedItem;
        private HyperJumpFolderViewModel _selItem;

        private async Task<List<FolderMatch>> ParseCommandAsync(string[] parts, CancellationToken token)
        {
            if (parts.Length ==0) { return null; }

            var searchTasks = parts.Where(p => !_previousResults.Any(x => x.Term.Equals(p, StringComparison.OrdinalIgnoreCase)))
                                   .Select(part => Task.Run(() => GetHighestFolderMatches(part, false, token), token))
                                   .ToList();

            var res = await Task.WhenAll(searchTasks);

            var newMatches = res.ToList();

            var finalSet = parts.Select(p =>
            {
                var found = _previousResults.FirstOrDefault(x => x.Term.Equals(p, StringComparison.OrdinalIgnoreCase)) ??
                            newMatches.FirstOrDefault(x => x.Term.Equals(p, StringComparison.OrdinalIgnoreCase));
                return found;
            }).ToList();

            if (!finalSet.Any()) { return null; }

            var drivingSet = finalSet.Last();
            var survivors = new List<FolderMatch>();

            foreach (var folder in drivingSet.Matches)
            {
                var orphaned = false;

                for (var parentLevel = newMatches.Count - 2; parentLevel >= 0; parentLevel--)
                {
                    token.ThrowIfCancellationRequested();
                    var possibleParents = finalSet[parentLevel].Matches;

                    if (possibleParents.Any(pf => folder.MatchedFolder.IsChildOf(pf.MatchedFolder))) continue;

                    orphaned = true;
                    break;
                }
                if (!orphaned) { survivors.Add(folder); }
            }

            _previousResults = finalSet;

            return survivors;
        }

        public HyperJumpFolderViewModel SelectedMatchedItem
        {
            get { return _selectedMatchedItem; }
            set
            {
                _selectedMatchedItem = value;
                NotifyOfPropertyChange();
            }
        }

        private FolderMatches GetHighestFolderMatches(string searchTerm, bool removeDescendants, CancellationToken token)
        {
            var sw = Stopwatch.StartNew();
            var initialFolders = _scanner.FolderLookup
                .WithCancellation(token)
                .Where(l =>
                {
//                    _log.Debug("[Where]");
//                    Thread.Sleep(300);
//                    await Task.Delay(200, token);
                    return l.Key.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0;
                })
                .SelectMany(l => l)
                .Distinct(new DistinctFolderComparer())
                .Select(f => new FolderMatch { MatchedFolder = f, Position = f.Name.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) })
                .ToList();

            _log.Debug($"seeking {searchTerm} took {sw.ElapsedMilliseconds}");

//            if (!removeDescendants) { return new FolderMatches(searchTerm, initialFolders); }
            return new FolderMatches(searchTerm, initialFolders);
            //            var redundantFolders = new List<FolderMatch>();
            //
            //            foreach (var folder in initialFolders.OrderBy(f => f.MatchedFolder.Level))
            //            {
            //                var childFolders = initialFolders.Where(f => f.MatchedFolder.Drive == folder.MatchedFolder.Drive &&
            //                                                             f.MatchedFolder.Level > folder.MatchedFolder.Level &&
            //                                                             f.MatchedFolder.Lineage[folder.MatchedFolder.Level - 1] ==
            //                                                             folder.MatchedFolder.Lineage[folder.MatchedFolder.Level - 1]).ToList();
            //                if (childFolders.Count == 0) { continue; }
            //
            //                foreach (var f in childFolders)
            //                {
            //                    initialFolders.Remove(f);
            //                }
            //
            ////                redundantFolders.AddRange(childFolders);
            //            }
            //
            ////            foreach (var folder in redundantFolders)
            ////            {
            ////                initialFolders.Remove(folder);
            ////            }
            //
            //            return initialFolders;
        }

        public void DoCommand(char key)
        {
            var console = _consoleRepo.Items.FirstOrDefault(x => char.ToUpper(x.Hotkey) == key || char.ToLower(x.Hotkey) == key);
            if (console == null) { return; }

            var di = new DirectoryInfo(SelectedMatchedItem.FullPath);
            var drive = di.Root.Name.TrimEnd('\\');

            var args0 = console.Arguments.Replace("{DRIVE}", drive);
            var args = string.Format(args0, SelectedMatchedItem.FullPath);

            var psi = new ProcessStartInfo(string.Format(console.Target, SelectedMatchedItem.FullPath))
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

    static class CancelExtension
    {
        public static IEnumerable<T> WithCancellation<T>(this IEnumerable<T> en, CancellationToken token)
        {
            var logger = LogManager.GetLogger("Default");

            foreach (var item in en)
            {
//                logger.Debug("[WithCancellation]");

                token.ThrowIfCancellationRequested();
                yield return item;
            }
        }
    }
}
