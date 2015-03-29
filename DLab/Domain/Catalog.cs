using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DLab.CatalogData;
using DLab.Infrastructure;
using ProtoBuf;
using Wintellect.Sterling;

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
    }

    public class WebSpecRepo
    {
        private WebSpecs _webSpecs { get; set; }

        public List<WebSpec> Specs
        {
            get
            {
                if (_webSpecs != null) return _webSpecs.Specs;

                using (var file = File.OpenRead("WebSpecs.bin"))
                {
                    _webSpecs = Serializer.Deserialize<WebSpecs>(file);
                }
                return _webSpecs.Specs;
            }
        }

        public void Save(WebSpec webSpec)
        {
            var existingSpec = _webSpecs.Specs.SingleOrDefault(x => x.Id == webSpec.Id);
            if (existingSpec != null)
            {
                _webSpecs.Specs.Remove(existingSpec);
            }
            _webSpecs.Specs.Add(webSpec);
        }

        public void Flush()
        {
            using (var file = File.Create("WebSpecs.bin"))
            {
                Serializer.Serialize(file, _webSpecs);
            }
        }
    }



    public class ProtoCat
    {
        private WebSpecs _webSpecs { get; set; }
        private FolderDirectory _folderDirectory { get; set; }


        public List<FolderSpec> Folders {
            get
            {
                if (_folderDirectory != null) return _folderDirectory.Folders;
                using (var file = File.OpenRead("FolderDirectory.bin"))
                {
                    _folderDirectory = Serializer.Deserialize<FolderDirectory>(file);
                }
                return _folderDirectory.Folders;
            }
        }

        public List<WebSpec> WebSpecs()
        {
            if (_webSpecs != null) return _webSpecs.Specs;
            using (var file = File.OpenRead("WebSpecs.bin"))
            {
                _webSpecs = Serializer.Deserialize<WebSpecs>(file);
            }
            return _webSpecs.Specs;
        }

        public List<ClipboardItem> ClipboardItems()
        {
            return new List<ClipboardItem>();
        }

        public void Save<T>(T entity) where T : class, new()
        {
            
        }

        public void SaveNoFlush<T>(T entity) where T : class, new()
        {
            throw new NotImplementedException();
        }

        public ISterlingDatabaseInstance Instance
        {
            get { throw new NotImplementedException(); }
        }

        public void Flush()
        {
            throw new NotImplementedException();
        }

        public void Clear<T>() where T : class, new()
        {
            throw new NotImplementedException();
        }

        public List<MatchResult> GetMatches(string text, int pageSize = 5)
        {
            throw new NotImplementedException();
        }

        public void CreateDefaultFolders()
        {
            throw new NotImplementedException();
        }

        public void Remove<T>(T entity) where T : class
        {
            throw new NotImplementedException();
        }

        public void TrySaveClipboardItem(ClipboardItem instance)
        {
            throw new NotImplementedException();
        }
    }

    public class Catalog : ICatalog
    {
        private readonly ISterlingDatabaseInstance _catalogDatabaseInstance;

        public Catalog(ISterlingDatabaseInstance catalogDatabaseInstance)
        {
            _catalogDatabaseInstance = catalogDatabaseInstance;
        }

        public List<CatalogEntry> Files()
        {
            return _catalogDatabaseInstance.Query<CatalogEntry, int>().Select(x => x.LazyValue.Value).ToList();
        }

        public List<FolderSpec> Folders()
        {
            return _catalogDatabaseInstance.Query<FolderSpec, int>().Select(x => x.LazyValue.Value).ToList();
        }

        public List<WebSpec> WebSpecs()
        {
            return _catalogDatabaseInstance.Query<WebSpec, int>().Select(x => x.LazyValue.Value).ToList();
        }

        public List<ClipboardItem> ClipboardItems()
        {
            var res = _catalogDatabaseInstance.Query<ClipboardItem, int>();
            return res.Select(x => x.LazyValue.Value).ToList();
        }

        public void Save<T>(T entity) where T : class, new()
        {
            _catalogDatabaseInstance.Save(entity);
            _catalogDatabaseInstance.Flush();
        }

        public void SaveNoFlush<T>(T entity) where T : class, new()
        {
            _catalogDatabaseInstance.Save(entity);
        }

        public void Clear<T>() where T : class, new()
        {
            _catalogDatabaseInstance.Truncate(typeof(T));
        }

        public void Flush()
        {
            _catalogDatabaseInstance.Flush();
        }

        public ISterlingDatabaseInstance Instance { get { return _catalogDatabaseInstance; } }

        public List<MatchResult> GetMatches(string text, int pageSize = 5)
        {
            var webCommands = _catalogDatabaseInstance.Query<WebSpec, string, int>(CatalogDatabaseInstance.IdxWebCommand)
                .Where(x => x.Index.StartsWith(text, StringComparison.InvariantCultureIgnoreCase))
                .Take(pageSize)
                .Select(x => x.LazyValue.Value)
                .OrderByDescending(x => x.Priority);

            var fileCommands = _catalogDatabaseInstance.Query<CatalogEntry, string, int>(CatalogDatabaseInstance.IdxFilename)
                .Where(x => x.Index.StartsWith(text, StringComparison.InvariantCultureIgnoreCase))
                .Take(pageSize)
                .Select(x => x.LazyValue.Value)
                .OrderByDescending(x => x.Priority);

            var r1 = webCommands.Select(x => new MatchResult(x) {CommandType = CommandType.Uri, Icon = DefaultSystemBrowser.IconImage});
            var r2 = r1.Concat(fileCommands.Select(x => new MatchResult(x) { CommandType = CommandType.File}));

            return r2.Take(pageSize).ToList();
        }

        public void CreateDefaultFolders()
        {
            var f1 = new FolderSpec(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu));
            var f2 = new FolderSpec(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu));

            Save(f1);
            Save(f2);
            Flush();
        }

        public void Remove<T>(T entity) where T : class
        {
            _catalogDatabaseInstance.Delete(entity);
        }

        public void TrySaveClipboardItem(ClipboardItem instance)
        {
            var res = _catalogDatabaseInstance.Query<ClipboardItem, int>();
            var currentCount = res.Count;
            if (currentCount < MaxClipboardHistory)
            {
                instance.Id = currentCount + 1;
                Save(instance);
                return;
            }

            var candidates = _catalogDatabaseInstance.Query<ClipboardItem, int>()
                .Where(x => x.LazyValue.Value.PasteCount <= instance.PasteCount && x.LazyValue.Value.Clipped < instance.Clipped)
                .Select(x => x.LazyValue.Value);

            var victim = candidates.OrderBy(x => x.PasteCount).ThenBy(x => x.Clipped).FirstOrDefault();
            if (victim == null) return;
            instance.Id = victim.Id;
            Save(instance);
        }

        public const int MaxClipboardHistory = 3;
    }
}