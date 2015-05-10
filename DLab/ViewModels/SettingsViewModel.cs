using Caliburn.Micro;
using DLab.Domain;

namespace DLab.ViewModels
{
    public class SettingsViewModel : Conductor<ISettingsViewModel>.Collection.OneActive
    {
        private readonly IViewModelFactory _viewModelFactory;
        private readonly SettingsDirViewModel _settingsDirViewModel;
        private readonly SettingsWebViewModel _settingsWebViewModel;
        private readonly SettingsFolderViewModel _settingsFolderViewModel;

        public SettingsViewModel(IViewModelFactory viewModelFactory)
        {
            DisplayName = "Settings";
            _viewModelFactory = viewModelFactory;
            _settingsFolderViewModel = _viewModelFactory.GetViewModel<SettingsFolderViewModel>();
            _settingsWebViewModel = _viewModelFactory.GetViewModel<SettingsWebViewModel>();
            _settingsDirViewModel = _viewModelFactory.GetViewModel<SettingsDirViewModel>();
        }

        protected override void OnActivate()
        {
            Items.AddRange(new ISettingsViewModel[] { _settingsFolderViewModel, _settingsWebViewModel, _settingsDirViewModel });

            base.OnActivate();
        }

        public ISettingsViewModel SelectedItem { get; set; }
    }
}
