﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Caliburn.Micro;
using DLab.Domain;
using DLab.Infrastructure;
using ILog = log4net.ILog;

namespace DLab.ViewModels
{
    public class CommandViewModel : Screen, ITabViewModel
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly CommandResolver _commandResolver;
        private string _title;
        private BindableCollection<MatchResult> _matchedItems;
        private string _userCommand;
        private MatchResult _selectedMatch;
        private ILog _log;

        public CommandViewModel(IAppServices appServices, CommandResolver commandResolver)
        {
            _eventAggregator = appServices.EventAggregator;
            _log = appServices.Log;
            _commandResolver = commandResolver;
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
                    r = _commandResolver.GetMatches(_userCommand);
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

        private ImageClass GetIcon(MatchResult matchedItem)
        {
            var result = new ImageClass {Item = matchedItem};
            var icon = SafeExtractAssociatedIcon(matchedItem);
            if (icon == null) return null;

            result.ImageSource = Imaging.CreateBitmapSourceFromHIcon(
                        icon.Handle,
                        new Int32Rect(0,0,icon.Width, icon.Height),
                        BitmapSizeOptions.FromEmptyOptions());
            if (result.ImageSource.CanFreeze)
            {
                result.ImageSource.Freeze();
            }
            return result;
        }

        private Icon SafeExtractAssociatedIcon(MatchResult matchedItem)
        {
            Icon icon = null;
            try
            {
                icon = Icon.ExtractAssociatedIcon(matchedItem.CommandModel.Target);
            }
            catch (AccessViolationException e)
            {
                _log.Error(e);
            }
            catch (FileNotFoundException e)
            {
                _log.Error(e);
            }
            return icon;
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

            (SelectedMatchedItem.CommandModel as IWeightedCommand).Priority++;

            if (SelectedMatchedItem.CommandModel is WebSpec)
            {
                _commandResolver.Save(SelectedMatchedItem.CommandModel as WebSpec);
            }
            else
            {
                _commandResolver.Save(SelectedMatchedItem.CommandModel as CatalogEntry);
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
