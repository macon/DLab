using System.Collections.Generic;
using System.IO;
using ProtoBuf;

namespace DLab.Domain
{
    [ProtoContract]
    public class FileCommands
    {
        [ProtoMember(1)]
        public List<CatalogEntry> Entries { get; set; }

        public FileCommands()
        {
            Entries = new List<CatalogEntry>();
        }
    }

    [ProtoContract]
    public class CatalogEntry : EntityBase
    {
        [ProtoMember(1)]
        public string FolderPath { get; set; }

        public override string Target { get { return Fullname(); } }

        public CatalogEntry()
        {}

        public CatalogEntry(FileInfo fi) :this(fi.DirectoryName, fi.Name)
        {}

        public CatalogEntry(string folderPath, string filename)
        {
            FolderPath = folderPath;
            Command = filename;
            Id = Fullname().GetHashCode();
        }

        public string Fullname()
        {
            return Path.Combine(FolderPath, Command);
        }

        public override string ToString()
        {
            return string.Format("{0}, p={1}, path={2}", Command, Priority, FolderPath);
        }
    }
}