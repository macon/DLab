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
        public MatchResult(ISetPriority commandModel)
        {
            CommandModel = commandModel;
        }

        public MatchResult()
        {
        }

        public CommandType CommandType { get; set; }
        public ISetPriority CommandModel { get; private set; }
    }

    public class CatalogEntry : ISetPriority
    {
        public int Id { get; set; }
        public string FolderPath { get; set; }
        public string Filename { get; set; }
        public int Priority { get; set; }

        public string Command
        {
            get { return Fullname(); }
        }

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