using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Shapes;
using DLab.Domain;
using DLab.Infrastructure;
using DLab.ViewModels;
using log4net;
using Path = System.IO.Path;

namespace DLab.HyperJump
{
    public class Scanner
    {
        private readonly HyperjumpRepo _hyperjumpRepo;
        public Lookup<char, Folder> LetterIndex;

        private char[] _alpha;
        private ILog _log;

        public Scanner(HyperjumpRepo hyperjumpRepo, IAppServices appServices)
        {
            _log = appServices.Log;
            _hyperjumpRepo = hyperjumpRepo;
        }

        //        private ILookup<string, Folder> _folderLookup;

        public ILookup<string, Folder> FolderLookup { get; set; }
        //        public Dictionary<string, Folder> FlatList { get; set; }
        public Folder God { get; set; }

        public Task<bool> ScanAsync()
        {
            return Task.Run(() =>
            {
                Scan();
                return true;
            });
        }

        public void Scan()
        {
            var sw = Stopwatch.StartNew();

            var parents = _hyperjumpRepo.GetParents().ToList();

            var folders = parents.Where(x => !x.Exclude).Select(folderSpec =>
            {
                _log.Debug($"scanning {folderSpec.Path}");

                return ScanEx2(new DirectoryInfo(folderSpec.Path));
            }).ToList();

            var allFolders = folders.SelectMany(x => x as Folder[] ?? x.ToArray()).ToList();

            _log.Debug($"{allFolders.Count} folders");

            FolderLookup = allFolders
                                .Select(x => new { Key = x.QuickCode, Thing = x })
                                .Concat(allFolders.Select(x => new { Key = x.Name, Thing = x }))
                                .ToLookup(x => x.Key, x => x.Thing);

            _log.Debug($"{FolderLookup.Count} lookup keys");

            var items = FolderLookup.Where(l => l.Key.IndexOf("te", StringComparison.InvariantCultureIgnoreCase) >= 0).ToList();
            _log.Debug($"found {items.Count} matching 'te'");

//            var items = FolderLookup.Where(l => l.Key == "templates").ToList();
            foreach (var item in items.SelectMany(l => l))
            {
                _log.Debug(item.FullPath);
            }
        }

        private IEnumerable<Folder> ScanEx2(DirectoryInfo startingDi)
        {
            var rootFolder = new Folder
            {
                Name = startingDi.Name,
                FullPath = startingDi.FullName
            };
            rootFolder.Lineage.Add(0);

            var folderQueue = new Queue<Folder>(new[] { rootFolder });

            while (folderQueue.Any())
            {
                var currentFolder = folderQueue.Dequeue();

                var code = ExtractCode(currentFolder.Name);
                currentFolder.QuickCode = code;

                var di = new DirectoryInfo(currentFolder.FullPath);

                var subDirIndex = 0;

                try
                {
                    foreach (var subDi in di.GetDirectories())
                    {
                        var subFolder = new Folder
                        {
                            Name = subDi.Name,
                            FullPath = subDi.FullName,
                            Parent = currentFolder,
                            Lineage = new List<int>(currentFolder.Lineage) { subDirIndex++ }
                        };

                        currentFolder.Children.Add(subFolder);
                        folderQueue.Enqueue(subFolder);
                    }
                }
                catch (UnauthorizedAccessException e)
                {
                    Debug.WriteLine($"{e.Message}");
                    continue;
                }
                yield return currentFolder;
            }
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

        public static string SplitUpperCaseToString(string source)
        {
            return string.Join(" ", ExtractCode(source));
        }

        public static string ExtractCode(string source)
        {
            if (string.IsNullOrEmpty(source))
            {
                return "";
            }

            var code = new StringBuilder();

            var letters = source.ToCharArray();
            var previousChar = char.MinValue;

            code.Append(letters[0].ToString());

            for (var i = 1; i < letters.Length; i++)
            {
                var thisChar = letters[i];

                if ((char.IsLetterOrDigit(thisChar) && IsDelimiter(previousChar)) || char.IsUpper(thisChar))
                {
                    code.Append(thisChar.ToString());
                }

                previousChar = thisChar;
            }

            return code.ToString();
        }

        private static bool IsDelimiter(char previousChar)
        {
            return char.IsWhiteSpace(previousChar) ||
                previousChar == '-' ||
                previousChar == '.' ||
                previousChar == '_';
        }
    }
}
