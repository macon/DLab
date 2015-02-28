using System.Data;
using System.IO;

namespace DLab.Domain
{
    public enum CommandType
    {
        File=0,
        Uri=1
    }

    public class MatchResult
    {
        public MatchResult(EntityBase commandModel)
        {
            CommandModel = commandModel;
        }

        public MatchResult()
        {
        }

        public CommandType CommandType { get; set; }
        public EntityBase CommandModel { get; private set; }
    }

    public class CatalogEntry : EntityBase
    {
        public int Id { get; set; }
        public string FolderPath { get; set; }
        public string Filename { get; set; }

        public override string Target { get { return Fullname(); } }
        public override string Command { get { return Filename; } }

        public CatalogEntry()
        {}

        public CatalogEntry(FileInfo fi) :this(fi.DirectoryName, fi.Name)
        {}

        public CatalogEntry(string folderPath, string filename)
        {
            FolderPath = folderPath;
            Filename = filename;
            Id = Fullname().GetHashCode();
        }

        public string Fullname()
        {
            return Path.Combine(FolderPath, Filename);
        }

        public override string ToString()
        {
            return string.Format("{0}, p={1}, path={2}", Filename, Priority, FolderPath);
        }
    }
}