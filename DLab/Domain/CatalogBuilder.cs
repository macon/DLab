using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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

        public void Build()
        {
            foreach (var folder in _settings.Folders)
            {
                var folder1 = folder;

                CatalogMatchingFiles(folder1, 
                    filename => {
                        var fi = new FileInfo(filename);
                        var entry = new CatalogEntry(fi);
                        Contents.Add(entry);
                    });
            }
        }

        private void CatalogMatchingFiles(FolderSpec folderSpec, Action<string> fileAction)
        {
            foreach (var file in System.IO.Directory.EnumerateFiles(folderSpec.FolderName, "*.*").Where(x => ExtensionMatch(x, folderSpec.Extensions)))
            {
                fileAction(file);
            }

            foreach (var subDir in FileCommands.GetDirectories(folderSpec.FolderName))
            {
                try
                {
                    var subFolderSpec = new FolderSpec()
                    {
                        FolderName = subDir,
                        Extensions = folderSpec.Extensions,
                        Subdirectory = folderSpec.Subdirectory
                    };

                    CatalogMatchingFiles(subFolderSpec, fileAction);
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