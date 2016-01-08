using System;
using System.Linq;

namespace DLab.ViewModels
{
    public static class FolderExtensions
    {
        public static bool IsChildOf(this Folder child, Folder parent)
        {
            if (!child.Drive.Equals(parent.Drive, StringComparison.OrdinalIgnoreCase)) { return false; }
            if (parent.Lineage.Count >= child.Lineage.Count) { return false; }

            return !parent.Lineage.Where((l, i) => l != child.Lineage[i]).Any();
        }

    }
}