using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Caliburn.Micro;
using DLab.Domain;
using DLab.Events;
using Ookii.Dialogs.Wpf;

namespace DLab.ViewModels
{
    public class SettingsFolderViewModel : Screen, ISettingsViewModel
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly FolderSpecRepo _folderSpecRepo;
        private readonly FileCommandsRepo _fileCommandsRepo;
        private FolderSpecViewModel _selectedFolder;
        private bool _isScanning;
        public BindableCollection<FolderSpecViewModel> Folders { get; private set; }
        private List<CatalogEntry> CatalogFiles { get; set; }

        public SettingsFolderViewModel(IEventAggregator eventAggregator, FolderSpecRepo folderSpecRepo, FileCommandsRepo fileCommandsRepo)
        {
            _eventAggregator = eventAggregator;
            _folderSpecRepo = folderSpecRepo;
            _fileCommandsRepo = fileCommandsRepo;
            DisplayName = "Folders";
            Folders = new BindableCollection<FolderSpecViewModel>();

            InitialiseFolders();
            CatalogFiles = _fileCommandsRepo.Files;
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
            var r = _folderSpecRepo.Folders;
            Folders.AddRange(r.Select(x => new FolderSpecViewModel(x, _folderSpecRepo)));
            SelectedFolder = Folders.FirstOrDefault();
        }

        public void Clear()
        {
            _fileCommandsRepo.Clear();
            _fileCommandsRepo.Flush();
        }

        public void AddSpecialFolder()
        {
        }

        public void RemoveFolder()
        {
            if (SelectedFolder == null) return;
            _folderSpecRepo.Delete(SelectedFolder.Instance);
            Folders.Remove(SelectedFolder);
            SelectedFolder = Folders.FirstOrDefault();
        }

        public void AddFolder()
        {
            var s = new VistaFolderBrowserDialog();
            var result = s.ShowDialog();
            if (result.HasValue == false || result.Value == false || string.IsNullOrEmpty(s.SelectedPath)) return;

            var newFolderSpec = new FolderSpec(s.SelectedPath);
            _folderSpecRepo.Save(newFolderSpec);
            var newVm = new FolderSpecViewModel(newFolderSpec, _folderSpecRepo);
            Folders.Add(newVm);
            SelectedFolder = newVm;
        }

        public void DebugCatalog()
        {
            foreach (var entry in CatalogFiles.OrderBy(x => x.Command))
            {
                Debug.WriteLine(entry);
            }
        }

        public async Task<int> ScanAsync(CancellationToken token)
        {
            _folderSpecRepo.Flush();
            var settings = new DLabSettings();
            settings.Folders.AddRange(_folderSpecRepo.Folders);
            int result;

            var cb = new CatalogBuilder(settings);
            try
            {
                result = await Task.Run(() => cb.Build(_scanCancelToken.Token), token);
            }
            finally
            {
                CatalogFiles = cb.Contents;
            }
            return result;
        }

        public bool CanSaveAsync
        {
            get { return !IsScanning; }
        }

        public bool CanCancel
        {
            get { return IsScanning; }
        }

        public void Cancel()
        {
            _scanCancelToken.Cancel();
        }

        private async Task<bool> ScanAndSave(CancellationTokenSource cts)
        {
            try
            {
                await ScanAsync(cts.Token);
                Save();
            }
            catch (TaskCanceledException)
            {
                return false;
            }
            return true;
        }

        public bool IsScanning
        {
            get { return _isScanning; }
            set
            {
                _isScanning = value;
                NotifyOfPropertyChange();
                NotifyOfPropertyChange(() => CanCancel);
                NotifyOfPropertyChange(() => CanSaveAsync);
            }
        }

        public async void ScanIt()
        {
            _eventAggregator.Publish(new SystemStatusChangeEvent(SystemState.Scanning), Execute.BeginOnUIThread);
            await Task.Delay(TimeSpan.FromSeconds(5));
            _eventAggregator.Publish(new SystemStatusChangeEvent(SystemState.Idle), Execute.BeginOnUIThread);
        }

        private CancellationTokenSource _scanCancelToken;
        public async void SaveAsync()
        {
            _scanCancelToken = new CancellationTokenSource();
            try
            {
                IsScanning = true;
                await ScanAndSave(_scanCancelToken);
            }
            finally
            {
                IsScanning = false;
                NotifyOfPropertyChange(() => FileCount);
            }
        }

        public string FileCount
        {
            get { return string.Format("File commands: {0}", _fileCommandsRepo.Files.Count); }
        }

        private bool Save()
        {
            if (CatalogFiles.Count == 0) return false;

            _fileCommandsRepo.ReplaceAll(CatalogFiles);
            _fileCommandsRepo.Flush();
            _folderSpecRepo.Flush();
            return true;
        }
    }
}