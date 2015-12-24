using Caliburn.Micro;

namespace DLab.ViewModels
{
    public sealed class SettingsViewModel : Conductor<ISettingsViewModel>.Collection.OneActive
    {
        private readonly SettingsDirViewModel _settingsDirViewModel;
        private readonly SettingsWebViewModel _settingsWebViewModel;
        private readonly SettingsFolderViewModel _settingsFolderViewModel;
        private readonly SettingsRunnerViewModel _settingsRunnerViewModel;
        private readonly SettingsHyperViewModel _settingsHyperViewModel;

        public SettingsViewModel(IViewModelFactory viewModelFactory)
        {
            DisplayName = "Settings";
            var viewModelFactory1 = viewModelFactory;
            _settingsFolderViewModel = viewModelFactory1.GetViewModel<SettingsFolderViewModel>();
            _settingsWebViewModel = viewModelFactory1.GetViewModel<SettingsWebViewModel>();
            _settingsDirViewModel = viewModelFactory1.GetViewModel<SettingsDirViewModel>();
            _settingsRunnerViewModel = viewModelFactory1.GetViewModel<SettingsRunnerViewModel>();
            _settingsHyperViewModel = viewModelFactory1.GetViewModel<SettingsHyperViewModel>();
        }

        protected override void OnActivate()
        {
            Items.AddRange(new ISettingsViewModel[]
            {
                _settingsFolderViewModel,
                _settingsWebViewModel,
                _settingsDirViewModel,
                _settingsRunnerViewModel,
                _settingsHyperViewModel
            });

            base.OnActivate();
        }

        public ISettingsViewModel SelectedItem { get; set; }
    }
}
