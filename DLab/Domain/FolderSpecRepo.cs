using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ProtoBuf;

namespace DLab.Domain
{
    public class FolderSpecRepo
    {
        private FolderDirectory _folderDirectory { get; set; }

        public List<FolderSpec> Folders
        {
            get
            {
                if (_folderDirectory != null) return _folderDirectory.Folders;
                if (!File.Exists("Folders.bin"))
                {
                    _folderDirectory = new FolderDirectory();
                    return _folderDirectory.Folders;
                }

                using (var file = File.OpenRead("Folders.bin"))
                {
                    _folderDirectory = Serializer.Deserialize<FolderDirectory>(file);
                }
                return _folderDirectory.Folders;
            }
        }

        public void Save(FolderSpec folderSpec)
        {
            var existingEntry = _folderDirectory.Folders.SingleOrDefault(x => x.Id == folderSpec.Id);
            if (existingEntry != null)
            {
                _folderDirectory.Folders.Remove(existingEntry);
            }
            _folderDirectory.Folders.Add(folderSpec);
        }

        public void Flush()
        {
            using (var file = File.Create("Folders.bin"))
            {
                Serializer.Serialize(file, _folderDirectory);
            }
        }

        public void Delete(FolderSpec folderSpec)
        {
            var existingSpec = _folderDirectory.Folders.SingleOrDefault(x => x.Id == folderSpec.Id);
            if (existingSpec == null) return;
            _folderDirectory.Folders.Remove(existingSpec);
        }

        public void CreateDefaultFolders()
        {
            var f1 = new FolderSpec(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu));
            var f2 = new FolderSpec(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu));

            Save(f1);
            Save(f2);
            Flush();
        }
    }
}