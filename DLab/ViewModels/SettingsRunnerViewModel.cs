using System.Linq;
using System.Windows;
using Caliburn.Micro;
using DLab.Domain;
using DLab.Repositories;

namespace DLab.ViewModels
{
    public class SettingsRunnerViewModel : Screen, ISettingsViewModel
    {
        private RunnerSpecRepo _runnerSpecRepo;
        private RunnerSpecViewModel _selectedRunnerSpecViewModel;
        public BindableCollection<RunnerSpecViewModel> RunnerSpecs { get; set; }

        public SettingsRunnerViewModel()
        {
            DisplayName = "Runner";
        }

        public SettingsRunnerViewModel(RunnerSpecRepo runnerSpecRepo)
        {
            _runnerSpecRepo = runnerSpecRepo;
            DisplayName = "Runner";
            RunnerSpecs = new BindableCollection<RunnerSpecViewModel>();
            Initialise();
        }

        private void Initialise()
        {
            RunnerSpecs.Clear();
            var specs = _runnerSpecRepo.Specs;
            RunnerSpecs.AddRange(specs.Select(x => new RunnerSpecViewModel(x)));
        }

        public void Add()
        {
            var runnerSpecViewModel = new RunnerSpecViewModel();
            RunnerSpecs.Add(runnerSpecViewModel);
        }

        public bool CanRemove => SelectedRunnerSpec != null;

        public void Remove()
        {
            if (SelectedRunnerSpec == null) return;
            _runnerSpecRepo.Delete(SelectedRunnerSpec.Instance);
            RunnerSpecs.Remove(SelectedRunnerSpec);
        }

        public RunnerSpecViewModel SelectedRunnerSpec
        {
            get { return _selectedRunnerSpecViewModel; }
            set
            {
                _selectedRunnerSpecViewModel = value;
                NotifyOfPropertyChange();
                NotifyOfPropertyChange(nameof(CanRemove));
            }
        }

        public void Clear()
        {
            _runnerSpecRepo.Clear();
            Initialise();
        }

        public void Save()
        {
            foreach (var viewModel in RunnerSpecs.Where(x => x.Unsaved || x.IsDirty))
            {
                if (viewModel.Unsaved) { viewModel.Instance.SetId(); }
                _runnerSpecRepo.Save(viewModel.Instance);
                viewModel.IsDirty = false;
            }
            _runnerSpecRepo.Flush();
            MessageBox.Show($"Saved {_runnerSpecRepo.Specs.Count}");
            TryClose();
        }

    }
}