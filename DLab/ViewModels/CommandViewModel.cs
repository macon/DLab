using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Caliburn.Micro;
using DLab.Domain;

namespace DLab.ViewModels
{
    public class CommandViewModel : Screen, ITabViewModel
    {
        private readonly ICatalog _catalog;
        private readonly IEventAggregator _eventAggregator;
        private string _title;
        private BindableCollection<MatchResult> _matchedItems;
        private string _userCommand;
        private MatchResult _selectedMatch;

        public CommandViewModel(ICatalog catalog, IEventAggregator eventAggregator)
        {
            _catalog = catalog;
            _eventAggregator = eventAggregator;
            _eventAggregator.Subscribe(this);
            _matchedItems = new BindableCollection<MatchResult>();
            DisplayName = "Command";
        }

        public int Order
        {
            get { return 0; }
        }

        public string Title
        {
            get { return _title; }
            set
            {
                if (value == _title) return;
                _title = value;
                NotifyOfPropertyChange();
            }
        }

        public string UserCommand
        {
            get { return _userCommand; }
            set
            {
                _userCommand = value;
                NotifyOfPropertyChange();

                var r = new List<MatchResult>();

                if (!string.IsNullOrEmpty(_userCommand))
                {
                    r = _catalog.GetMatches(_userCommand);
                }
                MatchedItems.Clear();
                if (!r.Any()) return;
                MatchedItems.AddRange(r);
                SelectedMatchedItem = MatchedItems.First();
                EnhanceWithIconAsync();
            }
        }

        private async void EnhanceWithIconAsync()
        {
            var iconTasks = (from matchedItem in MatchedItems let item = matchedItem 
                             select Task.Run(() => GetIcon(item))).ToList();

            while (iconTasks.Count > 0)
            {
                var task = await Task.WhenAny(iconTasks);
                iconTasks.Remove(task);

                var matchedItem = await task;
                if (matchedItem == null) continue;
                matchedItem.Item.Icon = matchedItem.ImageSource;
            }
        }

        private class ImageClass
        {
            public MatchResult Item { get; set; }
            public ImageSource ImageSource { get; set; }
        }

        private static ImageClass GetIcon(MatchResult matchedItem)
        {
            var result = new ImageClass {Item = matchedItem};
            var icon = Icon.ExtractAssociatedIcon(matchedItem.CommandModel.Target);
            if (icon == null) return null;

            result.ImageSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
                        icon.Handle,
                        new Int32Rect(0,0,icon.Width, icon.Height),
                        BitmapSizeOptions.FromEmptyOptions());
            if (result.ImageSource.CanFreeze)
            {
                result.ImageSource.Freeze();
            }
            return result;
        }

        public MatchResult SelectedMatchedItem
        {
            get { return _selectedMatch; }
            set
            {
                _selectedMatch = value;
                NotifyOfPropertyChange();
            }
        }

        public BindableCollection<MatchResult> MatchedItems
        {
            get
            {
                return _matchedItems;
            }
        }

        public void RunCommand()
        {
            if (SelectedMatchedItem == null) return;

            (SelectedMatchedItem.CommandModel as ISetPriority).Priority++;

            if (SelectedMatchedItem.CommandModel is WebSpec)
            {
                _catalog.Save(SelectedMatchedItem.CommandModel as WebSpec);
            }
            else
            {
                _catalog.Save(SelectedMatchedItem.CommandModel as CatalogEntry);
            }
            _catalog.Flush();

            Process.Start(SelectedMatchedItem.CommandModel.Target);

            _eventAggregator.Publish(new UserActionEvent(), Execute.BeginOnUIThread);
        }
    }
}
