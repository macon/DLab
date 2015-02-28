using System;
using System.Collections.Generic;
using Caliburn.Micro;
using DLab.Domain;

namespace DLab.ViewModels
{
    public class FolderSpecViewModel : Screen
    {
        private readonly FolderSpec _innerFolderSpec;
        private readonly ICatalog _catalog;

        public FolderSpec Instance { get { return _innerFolderSpec; } }

        public FolderSpecViewModel(FolderSpec innerFolderSpec, ICatalog catalog)
        {
            if (innerFolderSpec == null) throw new ArgumentNullException("innerFolderSpec");
            _innerFolderSpec = innerFolderSpec;
            _catalog = catalog;
        }

        public bool Subdirectory
        {
            get { return _innerFolderSpec.Subdirectory; }
            set
            {
                if (_innerFolderSpec.Subdirectory == value) return;
                _innerFolderSpec.Subdirectory = value;
                _catalog.Save(_innerFolderSpec);
                NotifyOfPropertyChange();
            }
        }

        public string Extensions
        {
            get { return _innerFolderSpec.SearchPattern; }
            set
            {
                if (_innerFolderSpec.SearchPattern == value) return;

                _innerFolderSpec.SetExtensions(value);
                _catalog.Save(_innerFolderSpec);
                NotifyOfPropertyChange();
            }
        }

        public string FolderName
        {
            get { return _innerFolderSpec.FolderName; }
            set { _innerFolderSpec.FolderName = value; }
        }

        public int Id
        {
            get { return _innerFolderSpec.Id; }
            set { _innerFolderSpec.Id = value; }
        }
    }
}