using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Caliburn.Micro;
using DLab.Domain;
using DLab.Infrastructure;
using ILog = log4net.ILog;

namespace DLab.ViewModels
{
    public sealed class CommandViewModel : Screen, ITabViewModel
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly IAppServices _appServices;
        private readonly CommandResolver _commandResolver;
        private string _title;
        private BindableCollection<MatchResult> _matchedItems;
        private string _userCommand;
        private MatchResult _selectedMatch;
        private ILog _log;

        public CommandViewModel(IAppServices appServices, CommandResolver commandResolver, CommandResultViewModel commandResultViewModel)
        {
            _eventAggregator = appServices.EventAggregator;
            _log = appServices.Log;
            _appServices = appServices;
            _commandResolver = commandResolver;
            CommandResult = commandResultViewModel;
            _eventAggregator.Subscribe(this);
            _matchedItems = new BindableCollection<MatchResult>();
            DisplayName = "Command";
        }

        public int Order => 0;

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

                var matchResults = new List<MatchResult>();

                if (!string.IsNullOrEmpty(_userCommand))
                {
                    matchResults = _commandResolver.GetMatches(_userCommand, 10);
                }
                MatchedItems.Clear();
                if (!matchResults.Any()) return;
                MatchedItems.AddRange(matchResults);
                SelectedMatchedItem = MatchedItems.First();
                EnhanceWithIconAsync();
            }
        }

        public CommandResultViewModel CommandResult { get; }

        private async void EnhanceWithIconAsync()
        {
            var iconHelper = new IconHelper();

            var iconTasks = (from matchedItem in MatchedItems let item = matchedItem 
                             select Task.Run(() => iconHelper.GetIcon(item, item.CommandModel.Target))).ToList();

            while (iconTasks.Count > 0)
            {
                var task = await Task.WhenAny(iconTasks);
                iconTasks.Remove(task);

                var matchedItem = await task;
                if (matchedItem == null) continue;
                matchedItem.Item.Icon = matchedItem.ImageSource;
            }
        }

//        private ImageClass<MatchResult> GetIcon(MatchResult matchedItem)
//        {
//            var result = new ImageClass<MatchResult> {Item = matchedItem};
//            var icon = Win32.SafeExtractAssociatedIcon(matchedItem.CommandModel.Target);
//            if (icon == null) return null;
//
//            result.ImageSource = Imaging.CreateBitmapSourceFromHIcon(
//                        icon.Handle,
//                        new Int32Rect(0,0,icon.Width, icon.Height),
//                        BitmapSizeOptions.FromEmptyOptions());
//            if (result.ImageSource.CanFreeze)
//            {
//                result.ImageSource.Freeze();
//            }
//            return result;
//        }

//        private Icon SafeExtractAssociatedIcon(string path)
//        {
//            Icon icon = null;
//            try
//            {
//                icon = Icon.ExtractAssociatedIcon(path);
//            }
//            catch (AccessViolationException e)
//            {
//                _log.Error(e);
//            }
//            catch (FileNotFoundException e)
//            {
//                _log.Error(e);
//            }
//            return icon;
//        }

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

            (SelectedMatchedItem.CommandModel as IWeightedCommand).Priority++;

            if (SelectedMatchedItem.CommandModel is WebSpec)
            {
                _commandResolver.Save((WebSpec)SelectedMatchedItem.CommandModel);
            }
            else if (SelectedMatchedItem.CommandModel is CatalogEntry)
            {
                _commandResolver.Save((CatalogEntry)SelectedMatchedItem.CommandModel);
            }
            else
            {
                _commandResolver.Save(SelectedMatchedItem.CommandModel as RunnerSpec);
            }
            _commandResolver.Flush();

            if (!ValidCommand(SelectedMatchedItem.CommandModel)) return;

            var psi = ParseCommandModel(SelectedMatchedItem.CommandModel);
            Process.Start(psi);

            _eventAggregator.Publish(new UserActionEvent(), Execute.BeginOnUIThread);
        }

        private bool ValidCommand(EntityBase commandModel)
        {
            var fileSpec = commandModel as CatalogEntry;
            if (fileSpec == null) return true;
            if (File.Exists(fileSpec.Target)) return true;

            MessageBox.Show(string.Format("File not found:\n{0}\n\n(consider re-scanning)", fileSpec.Target), "Warning");
            return false;
        }

        private ProcessStartInfo ParseCommandModel(EntityBase target)
        {
            var psi = new ProcessStartInfo();

            if (Clipboard.ContainsText() && target.Target.Contains("{0}"))
            {
                psi.FileName = string.Format(target.Target, Clipboard.GetText());
            }
            else
            {
                psi.FileName = target.Target;
            }

            if (!string.IsNullOrEmpty(target.Arguments))
            {
                psi.Arguments = target.Arguments;
            }

            return psi;
        }
    }
}
