using System.Linq;
using System.Windows;
using Caliburn.Micro;
using DLab.Domain;
using DLab.Repositories;

namespace DLab.ViewModels
{
    public sealed class SettingsWebViewModel : Screen, ISettingsViewModel
    {
        private readonly WebSpecRepo _webSpecRepo;
        private WebSpecViewModel _selectedWebSpec;

        public SettingsWebViewModel(WebSpecRepo webSpecRepo)
        {
            _webSpecRepo = webSpecRepo;
            DisplayName = "Web";
            WebSpecs = new BindableCollection<WebSpecViewModel>();
            InitialiseWebSpecs();
        }

        private void InitialiseWebSpecs()
        {
            WebSpecs.Clear();
            var specs = _webSpecRepo.Specs;
            WebSpecs.AddRange(specs.Select(x => new WebSpecViewModel(x)));
        }

        public void Add()
        {
            var webSpecVieWModel = new WebSpecViewModel(new WebSpec());
            WebSpecs.Add(webSpecVieWModel);
        }

        public bool CanRemove => SelectedWebSpec != null;

        public void Remove()
        {
            if (SelectedWebSpec == null) return;
            var entity = SelectedWebSpec.Instance;
            _webSpecRepo.Delete(SelectedWebSpec.Instance);
            WebSpecs.Remove(SelectedWebSpec);
        }

        public void Clear()
        {
            _webSpecRepo.Clear();
            InitialiseWebSpecs();
        }

        public void Save()
        {
            foreach (var viewModel in WebSpecs.Where(x => x.Unsaved || x.IsDirty))
            {
                if (viewModel.Unsaved) { viewModel.Instance.SetId(); }
                _webSpecRepo.Save(viewModel.Instance);
                viewModel.IsDirty = false;
            }
            _webSpecRepo.Flush();
            MessageBox.Show($"Saved {_webSpecRepo.Specs.Count}");
            TryClose();
        }

        public BindableCollection<WebSpecViewModel> WebSpecs { get; set; }

        public WebSpecViewModel SelectedWebSpec
        {
            get { return _selectedWebSpec; }
            set
            {
                _selectedWebSpec = value;
                NotifyOfPropertyChange();
                NotifyOfPropertyChange(nameof(CanRemove));
            }
        }
    }
}