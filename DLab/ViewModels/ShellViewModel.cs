using System.Collections.Generic;
using System.Linq;
using Caliburn.Micro;
using DLab.Events;
using StructureMap;
using ILog = log4net.ILog;
using LogManager = log4net.LogManager;

namespace DLab.ViewModels
{
    public class ShellViewModel : Conductor<ITabViewModel>.Collection.OneActive, IHandle<UserActionEvent>, IHandle<SystemStatusChangeEvent>
    {
        private readonly IWindowManager _windowManager;
        private readonly IEventAggregator _eventAggregator;
        private readonly IViewModelFactory _viewModelFactory;
        private bool _isHidden;
        private SettingsViewModel _settingViewModel;
        private bool _isBusy;
        private ILog _logger;
//        public Screen ActiveViewModel => ActiveItem as Screen;

        public ShellViewModel(IWindowManager windowManager, IEventAggregator eventAggregator, IViewModelFactory viewModelFactory)
        {
            _windowManager = windowManager;
            _eventAggregator = eventAggregator;
            _viewModelFactory = viewModelFactory;
            _eventAggregator.Subscribe(this);
            _logger = LogManager.GetLogger("ShellView");

            Items.AddRange(new []
            {
                _viewModelFactory.GetViewModel<CommandViewModel>(),
                _viewModelFactory.GetViewModel<ClipboardViewModel>(),
                _viewModelFactory.GetViewModel<ProcessViewModel>(),
                _viewModelFactory.GetViewModel<TestViewModel>() as ITabViewModel
//                _viewModelFactory.GetViewModel<HyperspaceViewModel>() as ITabViewModel
            });
            ActivateCommandModel();
        }

//        protected override async void OnActivationProcessed(ITabViewModel item, bool success)
//        {
//            base.OnActivationProcessed(item, success);
//            if (!success) { return; }
//
//            var processViewModel = item as ProcessViewModel;
//            if (processViewModel == null) { return; }
//
//            await processViewModel.InitialiseProcessListAsync();
//        }

        public void ActivateCommandModel()
        {
            ActivateItem(Items.First(x => x is CommandViewModel));
        }

        public void ActivateProcessModel()
        {
            var processViewModel = (ProcessViewModel) Items.Single(x => x is ProcessViewModel);
            ActivateItem(processViewModel);
#pragma warning disable 4014
            processViewModel.InitialiseProcessListAsync();
#pragma warning restore 4014
            _logger.Debug("ActivateProcessModel: Finished");
        }

        public void ActivateClipboardModel()
        {
            ActivateItem(Items.First(x => x is ClipboardViewModel));
        }

        public void ActivateHyperspaceModel()
        {
            ActivateItem(Items.First(x => x is HyperspaceViewModel));
        }

        public void ActivateTestModel()
        {
            if (ActiveItem is TestViewModel) { return; }
            ActivateItem(Items.First(x => x is TestViewModel));
        }

        public bool IsHidden
        {
            get { return _isHidden; }
            set
            {
                _isHidden = value;
                NotifyOfPropertyChange();
            }
        }

        public bool IsBusy
        {
            get { return _isBusy; }
            set
            {
                _isBusy = value;
                NotifyOfPropertyChange();
            }
        }

        public void Settings()
        {
            if (_settingViewModel == null) { _settingViewModel = _viewModelFactory.GetViewModel<SettingsViewModel>(); }

            _windowManager.ShowDialog(_settingViewModel);
        }

        public void ClipboardChanged()
        {
            _eventAggregator.Publish(new ClipboardChangedEvent(), Execute.OnUIThread);
        }

        public void Handle(UserActionEvent message)
        {
            IsHidden = true;
        }

        public void Handle(SystemStatusChangeEvent message)
        {
            IsBusy = message.State != SystemState.Idle;
        }
    }

    public class UserActionEvent
    {
    }

    public interface IViewModelFactory
    {
        T GetViewModel<T>();
    }

    public class ViewModelFactory : IViewModelFactory
    {
        private readonly IContainer _container;

        public ViewModelFactory(IContainer container)
        {
            _container = container;
        }

        public T GetViewModel<T>()
        {
            return _container.TryGetInstance<T>();
        }
    }
}
