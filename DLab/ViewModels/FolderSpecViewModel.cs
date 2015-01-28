using System;
using System.Collections.Generic;
using Caliburn.Micro;
using DLab.Domain;

namespace DLab.ViewModels
{
    public class FolderSpecViewModel : Screen
    {
        private readonly FolderSpec _innerFolderSpec;
        public bool IsDirty { get; private set; }

        public FolderSpec Instance { get { return _innerFolderSpec; } }

        public FolderSpecViewModel(FolderSpec innerFolderSpec)
        {
            if (innerFolderSpec == null) throw new ArgumentNullException("innerFolderSpec");
            _innerFolderSpec = innerFolderSpec;
        }

        public bool Subdirectory
        {
            get { return _innerFolderSpec.Subdirectory; }
            set
            {
                if (_innerFolderSpec.Subdirectory == value) return;
                _innerFolderSpec.Subdirectory = value;
                IsDirty = true;
                NotifyOfPropertyChange();
            }
        }

        public List<string> Extensions
        {
            get { return _innerFolderSpec.Extensions; }
            set
            {
                if (_innerFolderSpec.Extensions == value) return;
                _innerFolderSpec.Extensions = value;
                IsDirty = true;
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