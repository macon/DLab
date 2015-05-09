using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DLab.ViewModels
{
    public class HyperJumpFolderViewModel
    {
        private readonly Folder _folder;

        public HyperJumpFolderViewModel(Folder folder)
        {
            if (folder == null) throw new ArgumentNullException("folder");
            _folder = folder;
        }

        public string DisplayText
        {
            get { return _folder.FullPath; }
            //            var regex = BuildRegex(parts);
            //
            //            return "Hello <Bold>there</Bold>";

        }

        //        private string BuildRegex(IEnumerable<string> parts)
        //        {
        //            var startPointRegex = "({0})[^\\]*\\+";
        //            var extraRegex = "({1})";
        //            var items = parts.ToList();
        //
        //            var buffer = new StringBuilder();
        //
        //            buffer.AppendFormat(startPointRegex, items.First());
        //
        //            foreach (var part in items.Skip(1))
        //            {
        //                buffer.AppendFormat(extraRegex, part);
        //            }
        //
        //        }
    }

    public class Folder
    {
        public Folder()
        {
            Children = new List<Folder>();
        }

        public string Name { get; set; }
        public string FullPath { get; set; }
        public Folder Parent { get; set; }
        public IList<Folder> Children { get; set; }

        public string MakeKey()
        {
            return Parent == null
                ? Name.Substring(0, 1)
                : string.Concat(Parent.Name.Substring(0, 1), Name.Substring(0, 1));
        }
    }
}
