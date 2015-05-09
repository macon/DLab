using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DLab.Domain
{
    public class CatalogBuilder
    {
        private readonly DLabSettings _settings;
        public List<CatalogEntry> Contents { get; set; }

        public CatalogBuilder(DLabSettings settings)
        {
            _settings = settings;
            Contents = new List<CatalogEntry>();
        }

        public int Build(CancellationToken token)
        {
            Contents = new List<CatalogEntry>();
            foreach (var folder in _settings.Folders)
            {
                token.ThrowIfCancellationRequested();
                var folder1 = folder;

                CatalogMatchingFiles(folder1, 
                    filename => {
                        var fi = new FileInfo(filename);
                        var entry = new CatalogEntry(fi);
                        Contents.Add(entry);
                    }, token);
            }
            return Contents.Count;
        }

        private void CatalogMatchingFiles(FolderSpec folderSpec, Action<string> fileAction, CancellationToken token)
        {
            foreach (var file in Directory.EnumerateFiles(folderSpec.FolderName, "*.*").Where(x => ExtensionMatch(x, folderSpec.ExtensionList)))
            {
                token.ThrowIfCancellationRequested();
                fileAction(file);
            }

            foreach (var subDir in Directory.GetDirectories(folderSpec.FolderName))
            {
                token.ThrowIfCancellationRequested();
                try
                {
                    var subFolderSpec = new FolderSpec()
                    {
                        FolderName = subDir,
                        Extensions = folderSpec.Extensions,
                        Subdirectory = folderSpec.Subdirectory
                    };

                    CatalogMatchingFiles(subFolderSpec, fileAction, token);
                }
                catch (UnauthorizedAccessException)
                {
                    // swallow, log, whatever
                }
            }
        }

        private bool ExtensionMatch(string filename, IEnumerable<string> extensions)
        {
            return extensions.Any(extension => filename.EndsWith(extension, StringComparison.InvariantCultureIgnoreCase));
        }
    }
}