using System.Collections.Generic;

namespace DLab.Domain
{
    public class DLabSettings
    {
        public List<FolderSpec> Folders { get; set; }

        public DLabSettings()
        {
            Folders = new List<FolderSpec>();
        }
    }
}