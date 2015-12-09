using System;
using System.Collections.Generic;
using System.Linq;
using Caliburn.Micro;
using DLab.Domain;
using DLab.Repositories;

namespace DLab.ViewModels
{
    public class FolderSpecViewModel : Screen
    {
        private readonly FolderSpecRepo _folderSpecRepo;

        public FolderSpec Instance { get; private set; }

        public FolderSpecViewModel(FolderSpec folderSpec, FolderSpecRepo folderSpecRepo)
        {
            if (folderSpec == null) throw new ArgumentNullException("folderSpec");
            Instance = folderSpec;
            _folderSpecRepo = folderSpecRepo;
        }

        public bool Subdirectory
        {
            get { return Instance.Subdirectory; }
            set
            {
                if (Instance.Subdirectory == value) return;
                Instance.Subdirectory = value;
                _folderSpecRepo.Save(Instance);
                NotifyOfPropertyChange();
            }
        }

//        public void SetExtensions(string extensions)
//        {
//            if (string.IsNullOrEmpty(extensions)) return;
//            var parts = extensions.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
//            if (parts.Length == 0) return;
//        }

//
//        public string SearchPattern
//        {
//            get
//            {
//                if (!Instance.Extensions.Any()) return "";
//                return Extensions.Count == 1
//                    ? Extensions.First()
//                    : Extensions.Aggregate((s1, s2) => Combine(s1, ";", s2));
//            }
//        }

        public string Extensions
        {
            get { return Instance.Extensions; }
            set
            {
                if (Instance.Extensions == value) return;

                Instance.Extensions = value;
                _folderSpecRepo.Save(Instance);
                NotifyOfPropertyChange();
            }
        }

        public string FolderName
        {
            get { return Instance.FolderName; }
            set
            {
                Instance.FolderName = value;
                _folderSpecRepo.Save(Instance);
            }
        }

        public int Id
        {
            get { return Instance.Id; }
            set
            {
                Instance.Id = value;
                _folderSpecRepo.Save(Instance);
            }
        }
    }
}