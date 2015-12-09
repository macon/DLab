using System.Collections.Generic;
using System.IO;
using System.Linq;
using DLab.Domain;
using ProtoBuf;

namespace DLab.Repositories
{
    public class FileCommandsRepo : BaseRepo<CatalogEntry>
    {
        public FileCommandsRepo()
        {
            StorageName = "FileCommands.bin";
        }

//        private FileCommands FileCommands { get; set; }
        protected override void OnDataLoaded()
        {
            RemoveDuplicates(Specs);
        }

//        public List<CatalogEntry> Specs
//        {
//            get
//            {
//                if (FileCommands != null) return FileCommands.Entries;
//
//                if (!File.Exists("FileCommands.bin"))
//                {
//                    FileCommands = new FileCommands();
//                    return FileCommands.Entries;
//                }
//
//                using (var file = File.OpenRead("FileCommands.bin"))
//                {
//                    FileCommands = Serializer.Deserialize<FileCommands>(file);
//                }
//                return FileCommands.Entries;
//            }
//        }

        private void RemoveDuplicates(List<CatalogEntry> fileCommands)
        {
            var groups = fileCommands.GroupBy(x => x.Fullname())
                                    .Where(x => x.Count() > 1).ToList();

            var dupes = fileCommands.GroupBy(x => x.Fullname())
                            .Where(x => x.Count() > 1)
                            .SelectMany(g => g.Skip(1))
                            .ToList();

            foreach (var dupe in dupes)
            {
                fileCommands.Remove(dupe);
            }

            dupes = fileCommands.GroupBy(x => x.Id)
                                        .Where(x => x.Count() > 1)
                                        .SelectMany(g => g.Skip(1))
                                        .ToList();
            foreach (var dupe in dupes)
            {
                fileCommands.Remove(dupe);
            }
        }

        public void ReplaceAll(List<CatalogEntry> files)
        {
            Specs.Clear();
            Specs.AddRange(files);
        }
    }
}