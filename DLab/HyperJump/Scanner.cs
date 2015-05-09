using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DLab.ViewModels;

namespace DLab.HyperJump
{
    public class Scanner
    {
        public Lookup<char, Folder> LetterIndex;

        private char[] _alpha;
        private IEnumerable<Folder> _allFolders;
        //        private ILookup<string, Folder> _folderLookup;

        public ILookup<string, Folder> FolderLookup { get; set; }
        //        public Dictionary<string, Folder> FlatList { get; set; }
        public Folder God { get; set; }

        public void Scan(DirectoryInfo startingDi)
        {
            _alpha = new[]
            {
                'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u',
                'v', 'w', 'x', 'y', 'z'
            };


            //            God = ScanEx(startingDi);
            _allFolders = ScanEx2(startingDi);

            FolderLookup = _allFolders.ToLookup(x => x.Name.ToLower(), x => x);

            //            LetterIndex = (Lookup<char, Folder>)BuildLetterLookup(God).ToLookup(x => x.Name.ToLower()[0], x => x);

            //            FolderLookup = (Lookup<string, Folder>) 
            //                God.Children.ToLookup(
            //                    c => string.Concat(c.Parent.Name.Substring(0,1),c.Name.Substring(0,1)), c => c
            //                );

            //            FolderLookup = (Lookup<string, Folder>) FlatList.ToLookup(x => x.Key, x => x.Value);

            //            FolderLookup = (Lookup<string, Folder>) Descendants(God).ToLookup(x => x.Key.ToLower(), x => x.Folder);

        }

        private IEnumerable<Folder> BuildLetterLookup(Folder folder)
        {
            var subFolders = new Queue<Folder>(new[] { folder });

            while (subFolders.Any())
            {
                var subFolder = subFolders.Dequeue();

                yield return subFolder;

                foreach (var n in subFolder.Children) subFolders.Enqueue(n);
            }
        }

        public class Relationship
        {
            public string Key { get; set; }
            public Folder Folder { get; set; }
        }

        static IEnumerable<Relationship> Descendants(Folder folder)
        {
            //            var subFolders = new Stack<Folder>(new[] { folder });
            var subFolders = new Queue<Folder>(new[] { folder });

            while (subFolders.Any())
            {
                //                var node = subFolders.Pop();
                var node = subFolders.Dequeue();

                if (node.Parent != null)
                    yield return new Relationship { Key = node.MakeKey(), Folder = node };

                foreach (var n in node.Children) subFolders.Enqueue(n);
            }
        }


        private IEnumerable<Folder> ScanEx2(DirectoryInfo startingDi)
        {
            var folderQueue = new Queue<Folder>(new[] { new Folder { Name = startingDi.Name.ToLower(), FullPath = startingDi.FullName.ToLower() } });

            while (folderQueue.Any())
            {
                var currentFolder = folderQueue.Dequeue();

                var di = new DirectoryInfo(currentFolder.FullPath);

                foreach (var subDi in di.GetDirectories())
                {
                    var subFolder = new Folder { Name = subDi.Name.ToLower(), FullPath = subDi.FullName.ToLower(), Parent = currentFolder };
                    currentFolder.Children.Add(subFolder);
                    folderQueue.Enqueue(subFolder);
                }

                yield return currentFolder;
            }
        }

        private Folder ScanEx(DirectoryInfo startingDi)
        {
            var f = new Folder { Name = startingDi.Name };

            foreach (var subDir in startingDi.GetDirectories())
            {
                var c = ScanEx(subDir);
                c.Name = subDir.Name;
                c.FullPath = subDir.FullName;
                c.Parent = f;
                f.Children.Add(c);
            }

            return f;
        }
    }
}
