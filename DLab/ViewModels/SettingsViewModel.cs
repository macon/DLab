using Caliburn.Micro;

namespace DLab.ViewModels
{
    public enum State
    {
        Idle=0,
        Saving=1,
        Scanning=2
    }

    public class SettingsViewModel : Conductor<ISettingsViewModel>.Collection.OneActive
    {
        private readonly IViewModelFactory _viewModelFactory;
        private SettingsWebViewModel _settingsWebViewModel { get; set; }
        private SettingsFolderViewModel _settingsFolderViewModel { get; set; }

        public SettingsViewModel(IViewModelFactory viewModelFactory)
        {
            DisplayName = "Settings";
            _viewModelFactory = viewModelFactory;
            _settingsFolderViewModel = _viewModelFactory.GetViewModel<SettingsFolderViewModel>();
            _settingsWebViewModel = _viewModelFactory.GetViewModel<SettingsWebViewModel>();
            Items.AddRange(new ISettingsViewModel[] { _settingsFolderViewModel, _settingsWebViewModel });
        }

        public ISettingsViewModel SelectedItem { get; set; }
    }
}
