using System;
using System.Collections.Generic;
using Caliburn.Micro;
using DLab.Domain;

namespace DLab.ViewModels
{
    public class FolderSpecViewModel : Screen
    {
        private readonly ICatalog _catalog;

        public FolderSpec Instance { get; private set; }

        public FolderSpecViewModel(FolderSpec folderSpec, ICatalog catalog)
        {
            if (folderSpec == null) throw new ArgumentNullException("folderSpec");
            Instance = folderSpec;
            _catalog = catalog;
        }

        public bool Subdirectory
        {
            get { return Instance.Subdirectory; }
            set
            {
                if (Instance.Subdirectory == value) return;
                Instance.Subdirectory = value;
                _catalog.Save(Instance);
                NotifyOfPropertyChange();
            }
        }

        public string Extensions
        {
            get { return Instance.SearchPattern; }
            set
            {
                if (Instance.SearchPattern == value) return;

                Instance.SetExtensions(value);
                _catalog.Save(Instance);
                NotifyOfPropertyChange();
            }
        }

        public string FolderName
        {
            get { return Instance.FolderName; }
            set { Instance.FolderName = value; }
        }

        public int Id
        {
            get { return Instance.Id; }
            set { Instance.Id = value; }
        }
    }
}