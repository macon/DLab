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
                using (var file = File.OpenRead("FileCommands.bin"))
                {
                    FileCommands = Serializer.Deserialize<FileCommands>(file);
                }
                return FileCommands.Entries;
            }
        }

        public void Save(CatalogEntry catalogEntry)
        {
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
    }
}