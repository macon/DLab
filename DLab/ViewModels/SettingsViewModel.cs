using Caliburn.Micro;
using DLab.Domain;

namespace DLab.ViewModels
{
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
        }

        protected override void OnActivate()
        {
            Items.AddRange(new ISettingsViewModel[] { _settingsFolderViewModel, _settingsWebViewModel });

            base.OnActivate();
        }

        public ISettingsViewModel SelectedItem { get; set; }
    }
}
