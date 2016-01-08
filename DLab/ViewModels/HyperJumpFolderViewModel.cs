using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace DLab.ViewModels
{
    public class HyperJumpFolderViewModel
    {
        private readonly Folder _folder;
        private readonly IList<string> _parts;

        public HyperJumpFolderViewModel(Folder folder, IList<string> parts)
        {
            if (folder == null) throw new ArgumentNullException("folder");
            _folder = folder;
            _parts = parts;
        }

        public string FullPath => _folder.FullPath;

        public string DisplayText
        {
            get
            {
                var pos = _folder.FullPath.LastIndexOf(@"\", StringComparison.Ordinal);
                if (pos < 0) return _folder.FullPath;

                var folderName = _folder.FullPath.Substring(pos + 1);
                var leftPart = _folder.FullPath.Remove(pos + 1);
                return $"{leftPart}<Bold>{folderName}</Bold>";
            }
        }

        public string QuickCode => _folder.QuickCode;
        public string Name => _folder.Name;

        public string DisplayText3
        {
            get
            {
                var sb = new StringBuilder(_folder.FullPath);
                var position = 0;

                foreach (var part in _parts)
                {
                    var regex = BuildRegex2(part, position);
                    var m = Regex.Match(sb.ToString(), regex, RegexOptions.IgnoreCase);

                    var match = m.Groups[m.Groups.Count - 1];
                    sb.Remove(match.Index, match.Length);
                    sb.Insert(match.Index, $"<Bold>{match.Value}</Bold>");
                    position = match.Index + match.Length + 13;
                }
                return sb.ToString();
            }
        }

        private string BuildRegex2(string part, int position)
        {
            return position > 0
                ? $".{{{position},}}({part})"
                : part;
        }

        public static string BuildRegex(IEnumerable<string> parts)
        {
            var startPointRegex = @"({0})[^\\]*\\+";
            var extraRegex = "({0})";
            var items = parts.ToList();

            var buffer = new StringBuilder();

            buffer.AppendFormat(startPointRegex, items.First());

            foreach (var part in items.Skip(1))
            {
                buffer.AppendFormat(extraRegex, part);
            }

            return buffer.ToString();
        }
    }
}
