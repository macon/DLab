using System;
using System.Collections.Generic;

namespace DLab.ViewModels
{
    public class Folder
    {
        private string _fullPath;

        public Folder()
        {
            Children = new List<Folder>();
            Lineage = new List<int>();
        }

        public List<int> Lineage { get; set; }
        public int Level => Lineage.Count;
        public string Name { get; set; }

        public string FullPath
        {
            get { return _fullPath; }
            set
            {
                _fullPath = value;
                Drive = FullPath.Split(new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries)[0];
            }
        }

        public Folder Parent { get; set; }
        public IList<Folder> Children { get; set; }
        public string QuickCode { get; set; }
        public string Drive { get; private set; }

        public string MakeKey()
        {
            return Parent == null
                ? Name.Substring(0, 1)
                : string.Concat(Parent.Name.Substring(0, 1), Name.Substring(0, 1));
        }
    }
}