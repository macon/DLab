using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Caliburn.Micro;
using DLab.HyperJump;

namespace DLab.ViewModels
{
    public class FolderMatches
    {
        public string Term { get; private set; }
        public List<FolderMatch> Matches { get; private set; }

        public FolderMatches(string term, List<FolderMatch> matches)
        {
            Term = term;
            Matches = matches;
        }
    }

    public class FolderMatch
    {
        public int Position { get; set; }
        public Folder MatchedFolder { get; set; }
    }

    public class HyperspaceViewModel : Screen//, ITabViewModel
    {
        private string _userCommand = "";
        private Scanner _scanner;
        private string _rootFolder = "";
        private ObservablePropertyBacking<string> _textInput = new ObservablePropertyBacking<string>();
        private bool _scanning;

        public HyperspaceViewModel()
        {
            Items = new ObservableCollection<HyperJumpFolderViewModel>();
            _scanner = new Scanner(null, null);
//            RootFolder = "d:\\";

            //            _textInput = new ObservablePropertyBacking<string>();

            _textInput
                .Throttle(TimeSpan.FromMilliseconds(300))
                .Do(s => Debug.WriteLine(s))
                .SubscribeOn(NewThreadScheduler.Default)
                .ObserveOn(SynchronizationContext.Current)
                .Subscribe(ParseCommand);
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

        public bool Scanning2
        {
            get { return _scanning; }
            set
            {
//                if (_scanning == value) { return; }
                _scanning = value;
                Debug.WriteLine($"IsNotifying:{IsNotifying}");
//                OnPropertyChanged(new PropertyChangedEventArgs(nameof(Scanning)));
                NotifyOfPropertyChange();
                Debug.WriteLine($"Scanning changed to {_scanning}");
            }
        }

        public void Rescan()
        {
            Scanning2 = true;
            Debug.WriteLine($"Pre-scan {Thread.CurrentThread.ManagedThreadId}");

//            await _scanner.ScanAsync(new DirectoryInfo(RootFolder));
            Thread.Sleep(1000);

            Debug.WriteLine($"Post-scan {Thread.CurrentThread.ManagedThreadId}");
//            Scanning = false;
        }

        public string RootFolder
        {
            get { return _rootFolder; }
            set
            {
//                if (_rootFolder.ToLower().Equals(value.ToLower())) return;
                _rootFolder = value;
                Scanning2 = !Scanning2;
//                Thread.Sleep(500);
                //                Rescan();
                NotifyOfPropertyChange();
            }
        }

        public HyperJumpFolderViewModel SelectedItem { get; set; }

        private void ParseCommand(string userCommand)
        {
            if (string.IsNullOrEmpty(userCommand))
            {
                Items.Clear();
                return;
            }

            var parts = userCommand.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            var masterList = parts.Select((x, i) => GetHighestFolderMatches(x, i != 0)).ToList();

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

            //            var matchedFolders = survivors.Select(f => new FolderMatch { MatchedFolder = f });

            Items.Clear();
            foreach (var item in survivors.OrderBy(f => f.MatchedFolder.Level).ThenBy(f => f.Position).Take(200))
            {
                Items.Add(new HyperJumpFolderViewModel(item.MatchedFolder, parts));
            }
            SelectedItem = Items.FirstOrDefault();
        }

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
                .Select(f => new FolderMatch { MatchedFolder = f, Position = f.Name.IndexOf(firstPart, StringComparison.OrdinalIgnoreCase) })
                .ToList();

            if (!removeDescendants) { return initialFolders; }

            var redundantFolders = new List<FolderMatch>();

            foreach (var folder in initialFolders.OrderBy(f => f.MatchedFolder.Level))
            {
                redundantFolders.AddRange(
                    initialFolders.Where(f => f.MatchedFolder.Level > folder.MatchedFolder.Level && f.MatchedFolder.Lineage[folder.MatchedFolder.Level - 1] == folder.MatchedFolder.Lineage[folder.MatchedFolder.Level - 1]));
            }

            foreach (var folder in redundantFolders)
            {
                initialFolders.Remove(folder);
            }
            return initialFolders;
        }

        //        private void ParseCommand(string userCommand)
        //        {
        //            if (string.IsNullOrEmpty(userCommand))
        //            {
        //                Items.Clear();
        //                foreach (var item in _scanner.God.Children)
        //                {
        //                    Items.Add(item);
        //                }
        //            }
        //
        //            var parts = userCommand.Split(new []{' '}, StringSplitOptions.RemoveEmptyEntries);
        //            if (parts.Length < 2) return;
        //
        //            var key = MakeKey(parts);
        //            var initialSet = _scanner.FolderLookup[key].ToList();
        //
        //            var finalSet = ExtendSearch(parts.Skip(2).ToArray(), initialSet);
        //
        //            Items.Clear();
        //            foreach (var item in finalSet)
        //            {
        //                Items.Add(item);
        //            }
        //            SelectedItem = Items.FirstOrDefault();
        //        }

        private IEnumerable<Folder> ExtendSearch(string[] parts, IEnumerable<Folder> initialSet)
        {
            var workingSet = initialSet;
            foreach (var partItem in parts)
            {
                var part = partItem;

                workingSet = workingSet.SelectMany(x => x.Children)
                        .Where(c => c.Name.StartsWith(part, StringComparison.InvariantCultureIgnoreCase));
            }
            return workingSet;
        }

        private static string MakeKey(string[] parts, int sliceLength = 1, int depth = 2)
        {
            var key = parts.Take(depth).Aggregate((s1, s2) => string.Concat(s1.Substring(0, sliceLength), s2.Substring(0, sliceLength)));
            //            var key = string.Concat(parts[0].Substring(0, 1), parts[1].Substring(0, 1)).ToLower();
            return key.ToLower();
        }

        public ObservableCollection<HyperJumpFolderViewModel> Items { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public int Order { get; }
    }


    public class ObservablePropertyBacking<T> : IObservable<T>
    {
        private readonly Subject<T> _innerObservable = new Subject<T>();
        private T _value;

        public T Value
        {
            get { return _value; }
            set
            {
                _value = value;
                _innerObservable.OnNext(value);
            }
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            return _innerObservable
                .DistinctUntilChanged()
                .AsObservable()
                .Subscribe(observer);
        }
    }
}
