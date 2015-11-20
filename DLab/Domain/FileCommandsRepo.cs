using System.Collections.Generic;
using System.IO;
using System.Linq;
using ProtoBuf;

namespace DLab.Domain
{
    public class FileCommandsRepo
    {
        private FileCommands FileCommands { get; set; }

        public List<CatalogEntry> Files
        {
            get
            {
                if (FileCommands != null) return FileCommands.Entries;

                if (!File.Exists("FileCommands.bin"))
                {
                    FileCommands = new FileCommands();
                    return FileCommands.Entries;
                }

                using (var file = File.OpenRead("FileCommands.bin"))
                {
                    FileCommands = Serializer.Deserialize<FileCommands>(file);
                    FileCommands = RemoveDuplicates(FileCommands);
                }
                return FileCommands.Entries;
            }
        }

        private void RemoveDuplicates(FileCommands fileCommands)
        {
            var d2 = from e in fileCommands.Entries
                     group e by e.FolderPath into g
                     where g.Count() > 1

                     

            var dupes = fileCommands.Entries.GroupBy(x => x.FolderPath)
                            .Where(x => x.Count() > 1)
                            .SelectMany(@group => @group.Skip(1))
                            .ToList();
            foreach (var dupe in dupes)
            {
                fileCommands.Entries.Remove(dupe);
            }
        }

        public void ReplaceAll(List<CatalogEntry> files)
        {
            FileCommands.Entries.Clear();
            FileCommands.Entries.AddRange(files);
        }

        public void Save(CatalogEntry catalogEntry)
        {
            var ids = FileCommands.Entries.Where(x => x.Id == catalogEntry.Id).ToList();
            var existingEntry = FileCommands.Entries.SingleOrDefault(x => x.Id == catalogEntry.Id);
            if (existingEntry != null)
            {
                FileCommands.Entries.Remove(existingEntry);
            }
            FileCommands.Entries.Add(catalogEntry);
        }

        public void Flush()
        {
            using (var file = File.Create("FileCommands.bin"))
            {
                Serializer.Serialize(file, FileCommands);
            }
        }

        public void Clear()
        {
            Files.Clear();
        }
    }
}