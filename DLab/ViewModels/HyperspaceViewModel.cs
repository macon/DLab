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
using System.Windows;
using System.Windows.Input;
using Caliburn.Micro;
using DLab.HyperJump;

namespace DLab.ViewModels
{
    public class HyperspaceViewModel : Screen, ITabViewModel
    {
        private string _userCommand = "";
        private Scanner _scanner;
        private string _rootFolder = "";
        private ObservablePropertyBacking<string> _textInput = new ObservablePropertyBacking<string>();

        public HyperspaceViewModel()
        {
            Items = new ObservableCollection<Folder>();
            _scanner = new Scanner();
            RootFolder = "d:\\dev";
            DisplayName = "Dir";

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

        public string RootFolder
        {
            get { return _rootFolder; }
            set
            {
                if (_rootFolder.ToLower().Equals(value.ToLower())) return;
                _rootFolder = value;
                _scanner.Scan(new DirectoryInfo(RootFolder));
                //                ParseCommand(UserCommand);
                NotifyOfPropertyChange();
            }
        }

        //        public string UserCommand
        //        {
        //            get { return _userCommand; }
        //            set
        //            {
        //                _userCommand = value;
        //
        //                ParseCommand(_userCommand);
        //
        //                OnPropertyChanged();
        //            }
        //        }

        public Folder SelectedItem { get; set; }

        private void ParseCommand(string userCommand)
        {
            if (string.IsNullOrEmpty(userCommand))
            {
                Items.Clear();
                return;
            }

            var parts = userCommand.ToLower().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            var firstPart = parts[0];

            var matchedFolders =
                _scanner.FolderLookup.Where(
                    l => l.Key.Contains(firstPart)).SelectMany(l => l);

            for (var i = 1; i < parts.Length; i++)
            {
                var part = parts[i];
                var isLast = i == parts.Length - 1;

                if (part == ".")
                {
                    matchedFolders = isLast 
                        ? GetDescendants(matchedFolders) 
                        : GetSpecificDescendants(matchedFolders, parts[++i]);
                    continue;
                }

                matchedFolders = matchedFolders
                    .SelectMany(f => f.Children)
                    .Where(c => c.Name.Contains(part));
            }

            Items.Clear();
            foreach (var item in matchedFolders)
            {
                Items.Add(item);
            }
            SelectedItem = Items.FirstOrDefault();
        }

        private IEnumerable<Folder> GetDescendants(IEnumerable<Folder> baseFolders)
        {
            var folderQueue = new Queue<Folder>(baseFolders.SelectMany(x => x.Children));

            while (folderQueue.Any())
            {
                var subFolder = folderQueue.Dequeue();
                yield return subFolder;
                foreach (var n in subFolder.Children) folderQueue.Enqueue(n);
            }
        }

        private IEnumerable<Folder> GetSpecificDescendants(IEnumerable<Folder> baseFolders, string folderNamePattern)
        {
            var folderQueue = new Queue<Folder>(baseFolders.SelectMany(x => x.Children));

            while (folderQueue.Any())
            {
                var subFolder = folderQueue.Dequeue();
                if (subFolder.Name.Contains(folderNamePattern))
                {
                    yield return subFolder;
                    continue;
                }

                foreach (var n in subFolder.Children) folderQueue.Enqueue(n);
            }
            
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

        public ObservableCollection<Folder> Items { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public int Order
        {
            get { return 9; }
        }

        public void DoCommand(string key)
        {
            var commandPrompt = @"C:\Windows\system32\cmd.exe";
            var powershell = @"C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe";

            if (key.ToLower() == "c")
            {
                if (!File.Exists(commandPrompt))
                {
                    MessageBox.Show("Could not find {0}", commandPrompt);
                    return;
                }
                var args = string.Format(@"/K ""cd /d {0}""", SelectedItem.FullPath);
                var psi = new ProcessStartInfo(commandPrompt) {Arguments = args};
                psi.UseShellExecute = false;
                Process.Start(psi);
                return;
            }

            if (key.ToLower() == "p")
            {
                if (!File.Exists(powershell))
                {
                    MessageBox.Show("Could not find {0}", powershell);
                    return;
                }
                var args = string.Format(@"-NoExit -Command ""& {{Set-Location {0}}}""", SelectedItem.FullPath);
                var psi = new ProcessStartInfo(powershell) { Arguments = args };
                psi.UseShellExecute = false;
                Process.Start(psi);
            }

            if (key.ToLower() == "e")
            {
                var psi = new ProcessStartInfo(SelectedItem.FullPath);
                Process.Start(psi);
            }
        }
    }


    public class ObservablePropertyBacking<T> : IObservable<T>
    {
        private Subject<T> _innerObservable = new Subject<T>();

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
