using System.Linq;
using Caliburn.Micro;
using DLab.Domain;

namespace DLab.ViewModels
{
    public class SettingsWebViewModel : Screen, ISettingsViewModel
    {
        private readonly ICatalog _catalog;
        private WebSpecViewModel _selectedWebSpec;

        public SettingsWebViewModel(ICatalog catalog)
        {
            _catalog = catalog;
            DisplayName = "Web";
            WebSpecs = new BindableCollection<WebSpecViewModel>();
            InitialiseWebSpecs();
        }

        private void InitialiseWebSpecs()
        {
            WebSpecs.Clear();
            var specs = _catalog.WebSpecs();
            WebSpecs.AddRange(specs.Select(x => new WebSpecViewModel(x)));
        }

        public void Add()
        {
            var webSpecVieWModel = new WebSpecViewModel(new WebSpec());
            WebSpecs.Add(webSpecVieWModel);
        }

        public bool CanRemove
        {
            get { return SelectedWebSpec != null; }
        }

        public void Remove()
        {
            if (SelectedWebSpec == null) return;
            var entity = SelectedWebSpec.Instance;
            _catalog.Remove(entity);
            _catalog.Flush();
            WebSpecs.Remove(SelectedWebSpec);
        }

        public void Clear()
        {
            _catalog.Clear<WebSpec>();
            _catalog.Flush();
            InitialiseWebSpecs();
        }

        public void Save()
        {
            foreach (var viewModel in WebSpecs.Where(x => x.Unsaved || x.IsDirty))
            {
                if (viewModel.Unsaved) { viewModel.Instance.SetId(); }
                _catalog.Save(viewModel.Instance);
                viewModel.IsDirty = false;
            }
            _catalog.Flush();
        }

        public BindableCollection<WebSpecViewModel> WebSpecs { get; set; }

        public WebSpecViewModel SelectedWebSpec
        {
            get { return _selectedWebSpec; }
            set
            {
                _selectedWebSpec = value;
                NotifyOfPropertyChange();
                NotifyOfPropertyChange("CanRemove");
            }
        }
    }
}