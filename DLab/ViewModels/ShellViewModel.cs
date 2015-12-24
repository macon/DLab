using System.Collections.Generic;
using System.Linq;
using Caliburn.Micro;
using DLab.Events;
using StructureMap;

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
//        public Screen ActiveViewModel => ActiveItem as Screen;

        public ShellViewModel(IWindowManager windowManager, IEventAggregator eventAggregator, IViewModelFactory viewModelFactory)
        {
            _windowManager = windowManager;
            _eventAggregator = eventAggregator;
            _viewModelFactory = viewModelFactory;
            _eventAggregator.Subscribe(this);

            Items.AddRange(new []
            {
                _viewModelFactory.GetViewModel<CommandViewModel>(),
                _viewModelFactory.GetViewModel<ClipboardViewModel>(),
                _viewModelFactory.GetViewModel<TestViewModel>() as ITabViewModel
//                _viewModelFactory.GetViewModel<HyperspaceViewModel>() as ITabViewModel
            });
            ActivateCommandModel();
        }

        public void ActivateCommandModel()
        {
            ActivateItem(Items.First(x => x is CommandViewModel));
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
