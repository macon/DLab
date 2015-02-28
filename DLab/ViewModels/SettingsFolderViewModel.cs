using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Caliburn.Micro;
using DLab.Domain;
using DLab.Events;
using Ookii.Dialogs.Wpf;

namespace DLab.ViewModels
{
    public class SettingsFolderViewModel : Screen, ISettingsViewModel
    {
        private readonly ICatalog _catalog;
        private readonly IEventAggregator _eventAggregator;
        private bool _includeSubfolders;
        private FolderSpecViewModel _selectedFolder;
        private SystemState _systemState;
        private bool _isScanning;
        private List<FolderSpec> MasterFolders { get; set; }
        public BindableCollection<string> ScannedItems { get; set; }
        public BindableCollection<FolderSpecViewModel> Folders { get; private set; }
        private List<CatalogEntry> CatalogFiles { get; set; }

        public SettingsFolderViewModel(ICatalog catalog, IEventAggregator eventAggregator)
        {
            MasterFolders = new List<FolderSpec>();
            _catalog = catalog;
            _eventAggregator = eventAggregator;
            DisplayName = "Folders";
            ScannedItems = new BindableCollection<string>();
            Folders = new BindableCollection<FolderSpecViewModel>();

            InitialiseFolders();

            CatalogFiles = _catalog.Files();
        }

        public FolderSpecViewModel SelectedFolder
        {
            get { return _selectedFolder; }
            set
            {
                _selectedFolder = value;
                NotifyOfPropertyChange();
            }
        }

        private void InitialiseFolders()
        {
            Folders.Clear();
            MasterFolders.Clear();

            var r = _catalog.Folders();
            MasterFolders.AddRange(r);
            Folders.AddRange(MasterFolders.Select(x => new FolderSpecViewModel(x, _catalog)));
            SelectedFolder = Folders.FirstOrDefault();
        }

        public void Clear()
        {
            _catalog.Clear<CatalogEntry>();
        }

        public void AddSpecialFolder()
        {

        }

        public void RemoveFolder()
        {
            if (SelectedFolder == null) return;
            var entity = MasterFolders.FirstOrDefault(x => x.Id == SelectedFolder.Id);
            if (entity == null) return;

            _catalog.Remove(entity);
            _catalog.Flush();
            InitialiseFolders();
        }

        public void AddFolder()
        {
            var s = new VistaFolderBrowserDialog();
            var result = s.ShowDialog();
            if (result.HasValue == false || result.Value == false || string.IsNullOrEmpty(s.SelectedPath)) return;

            var newFolderSpec = new FolderSpec(s.SelectedPath);
            MasterFolders.Add(newFolderSpec);
            Folders.Add(new FolderSpecViewModel(newFolderSpec, _catalog));

            _catalog.Save(newFolderSpec);
            _catalog.Flush();
        }

        public void DebugCatalog()
        {
            foreach (var entry in CatalogFiles.OrderBy(x => x.Filename))
            {
                Debug.WriteLine(entry);
            }
        }

        public Task<int> Scan()
        {
            var settings = new DLabSettings();
            settings.Folders.AddRange(MasterFolders);

            var cb = new CatalogBuilder(settings);
            cb.Build();
            CatalogFiles = cb.Contents;
            ScannedItems.Clear();
            ScannedItems.AddRange(cb.Contents.Select(c => c.Filename));
            return Task.FromResult(1);
        }

        public bool CanSave()
        {
            return _systemState == SystemState.Idle;
        }

        private Task<int> ScanAndSave()
        {
            return new Task<int>(() =>
            {
//                UpdateFolders();

                Scan();

                SaveAsync();
                return 1;
            });
        }

        public bool IsScanning
        {
            get { return _isScanning; }
            set
            {
                _isScanning = value;
                NotifyOfPropertyChange();
            }
        }

        public async void ScanIt()
        {
            _eventAggregator.Publish(new SystemStatusChangeEvent(SystemState.Scanning), Execute.BeginOnUIThread);
            await Task.Delay(TimeSpan.FromSeconds(5));
            _eventAggregator.Publish(new SystemStatusChangeEvent(SystemState.Idle), Execute.BeginOnUIThread);
        }

        public async void Save()
        {
            try
            {
                _systemState = SystemState.Saving;
                var t = ScanAndSave();
                t.Start();
                await t;
            }
            finally
            {
                _systemState = SystemState.Idle;
            }
        }

        private Task<int> SaveAsync()
        {
            if (CatalogFiles.Count == 0) return Task.FromResult(1);

            _catalog.Clear<CatalogEntry>();

            foreach (var entry in CatalogFiles)
            {
                _catalog.Save(entry);
            }
            _catalog.Flush();
            return Task.FromResult(1);
        }

//        private void UpdateFolders()
//        {
//            var needFlush = false;
//            foreach (var folder in Folders.Where(x => x.IsDirty))
//            {
//                _catalog.SaveNoFlush(folder.Instance);
//                needFlush = true;
//            }
//            if (needFlush) _catalog.Flush();
//        }

//        public bool IsDirty
//        {
//            get { return Folders.Any(x => x.IsDirty); }
//        }
    }
}