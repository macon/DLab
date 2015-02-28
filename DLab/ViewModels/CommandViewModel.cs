using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
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
            }
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
