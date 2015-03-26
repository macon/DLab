using System.Collections.Generic;
using Wintellect.Sterling;

namespace DLab.Domain
{
    public interface ICatalog
    {
        List<CatalogEntry> Files();
        List<FolderSpec> Folders();
        List<WebSpec> WebSpecs();
        List<ClipboardItem> ClipboardItems();
        void Save<T>(T entity) where T : class, new();
        void SaveNoFlush<T>(T entity) where T : class, new();
        ISterlingDatabaseInstance Instance { get; }
        void Flush();
        void Clear<T>() where T : class, new();
        List<MatchResult> GetMatches(string text, int pageSize = 5);
        void CreateDefaultFolders();
        void Remove<T>(T entity) where T : class;
        void TrySaveClipboardItem(ClipboardItem instance);
    }
}