using System;
using System.Collections.Generic;
using System.Linq;
using DLab.CatalogData;
using Wintellect.Sterling;

namespace DLab.Domain
{
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

            var r1 = webCommands.Select(x => new MatchResult(x));
            var r2 = r1.Concat(fileCommands.Select(x => new MatchResult(x)));

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
    }
}