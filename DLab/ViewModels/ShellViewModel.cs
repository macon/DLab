using Caliburn.Micro;
using StructureMap;

namespace DLab.ViewModels
{
    public class ShellViewModel : Screen, IHandle<UserActionEvent>
    {
        private readonly IWindowManager _windowManager;
        private readonly IEventAggregator _eventAggregator;
        private readonly IViewModelFactory _viewModelFactory;
        private bool _isHidden;
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
            var settingViewModel = _viewModelFactory.GetViewModel<SettingsViewModel>();
            var result = _windowManager.ShowDialog(settingViewModel);
        }

        public void ClipboardChanged()
        {
            _eventAggregator.Publish(new ClipboardChangedEvent(), Execute.OnUIThread);
        }

        public void Handle(UserActionEvent message)
        {
            IsHidden = true;
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
