using Caliburn.Micro;
using DLab.Events;
using StructureMap;

namespace DLab.ViewModels
{
    public class ShellViewModel : Screen, IHandle<UserActionEvent>, IHandle<SystemStatusChangeEvent>
    {
        private readonly IWindowManager _windowManager;
        private readonly IEventAggregator _eventAggregator;
        private readonly IViewModelFactory _viewModelFactory;
        private bool _isHidden;
        private SettingsViewModel _settingViewModel;
        private bool _isBusy;
        public Screen TabViewModel { get; set; }

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

        public ShellViewModel(IWindowManager windowManager, IEventAggregator eventAggregator, IViewModelFactory viewModelFactory)
        {
            _windowManager = windowManager;
            _eventAggregator = eventAggregator;
            _viewModelFactory = viewModelFactory;
            DisplayName = "DLab";
            TabViewModel = _viewModelFactory.GetViewModel<TabViewModel>();
            _eventAggregator.Subscribe(this);
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
