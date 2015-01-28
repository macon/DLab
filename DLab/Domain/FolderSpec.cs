using System.Collections.Generic;
using System.Linq;
using Wintellect.Sterling.Serialization;

namespace DLab.Domain
{
    public class FolderSpec
    {
        [SterlingIgnore]
        public static List<string> DefaultExtensions = new List<string>();

        public FolderSpec()
        {}

        static FolderSpec()
        {
            DefaultExtensions = new List<string> { ".lnk", ".exe", ".bat", ".cmd" };
        }

        public FolderSpec(string folder)
        {
            Extensions = DefaultExtensions;
            FolderName = folder;
            Id = FolderName.GetHashCode();
        }

        public int Id { get; set; }
        public string FolderName { get; set; }
        public List<string> Extensions { get; set; }
        public bool Subdirectory { get; set; }

        public string SearchPattern()
        {
            return Extensions.Aggregate((s1, s2) => Combine(s1, ";", s2));
        }

        [SterlingIgnore]
        public string DisplayPattern
        {
            get
            {
                return Extensions.Aggregate((s1, s2) => Combine(s1, "\n", s2));
            }
        }

        private string Combine(string s1, string separator, string s2)
        {
            return string.IsNullOrEmpty(s2)
                ? s1
                : string.Format("{0}{1}{2}", s1, separator, s2);
        }
    }
}