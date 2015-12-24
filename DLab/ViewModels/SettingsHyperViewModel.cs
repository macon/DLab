using System.Linq;
using System.Windows;
using Caliburn.Micro;
using DLab.Domain;

namespace DLab.ViewModels
{
    public sealed class SettingsHyperViewModel : Screen, ISettingsViewModel
    {
        private readonly HyperjumpRepo _hyperjumpRepo;
        public BindableCollection<HyperJumpSettingViewModel> Items { get; set; }
        private HyperJumpSettingViewModel _selectedItem;

        public SettingsHyperViewModel(HyperjumpRepo hyperjumpRepo)
        {
            _hyperjumpRepo = hyperjumpRepo;
            DisplayName = "Hyper";
            Items = new BindableCollection<HyperJumpSettingViewModel>();
            InitialiseItems();
        }

        private void InitialiseItems()
        {
            Items.Clear();
            Items.AddRange(_hyperjumpRepo.Items.Select(x => new HyperJumpSettingViewModel(x)));
        }

        public void Add()
        {
            var viewModel = new HyperJumpSettingViewModel(new HyperjumpSpec());
            Items.Add(viewModel);
        }

        public void Save()
        {
            foreach (var viewModel in Items.Where(x => x.Unsaved || x.IsDirty))
            {
                if (viewModel.Unsaved) { viewModel.Instance.SetId(); }
                _hyperjumpRepo.Save(viewModel.Instance);
                viewModel.IsDirty = false;
            }
            _hyperjumpRepo.Flush();
            MessageBox.Show($"Saved {_hyperjumpRepo.Items.Count}");
            TryClose();
        }

        public void Remove()
        {
            if (SelectedItem == null) return;
            var entity = SelectedItem.Instance;
            _hyperjumpRepo.Delete(entity);
            Items.Remove(SelectedItem);
        }

        public void Clear()
        {
            _hyperjumpRepo.Clear();
            InitialiseItems();
        }

        public HyperJumpSettingViewModel SelectedItem
        {
            get { return _selectedItem; }
            set
            {
                _selectedItem = value;
                NotifyOfPropertyChange();
            }
        }
    }

    public class HyperJumpSettingViewModel
    {
        public HyperJumpSettingViewModel(HyperjumpSpec hyperjumpSpec)
        {
            Instance = hyperjumpSpec;
        }

        public HyperjumpSpec Instance { get; }

        public string Path
        {
            get { return Instance.Path; }
            set
            {
                Instance.Path = value;
                IsDirty = true;
            }
        }

        public bool Exclude
        {
            get { return Instance.Exclude; }
            set
            {
                Instance.Exclude = value;
                IsDirty = true;
            }
        }

        public int Id
        {
            get { return Instance.Id; }
            set { Instance.Id = value; }
        }

        public bool Unsaved => Id == default(int);
        internal bool IsDirty { get; set; }
    }
}