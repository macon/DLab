using System;
using System.Collections.Generic;
using System.Linq;
using ProtoBuf;
using Wintellect.Sterling.Serialization;

namespace DLab.Domain
{
    [ProtoContract]
    public class FolderDirectory
    {
        [ProtoMember(1)]
        public List<FolderSpec> Folders { get; set; }
    }

    [ProtoContract]
    public class FolderSpec
    {
        [SterlingIgnore]
        public List<string> DefaultExtensions { get; set; }

        public FolderSpec()
        {
            DefaultExtensions = new List<string> { ".lnk", ".exe", ".bat", ".cmd" };
            Extensions = DefaultExtensions;
        }

        public FolderSpec(string folder) : this()
        {
            FolderName = folder;
            Id = FolderName.GetHashCode();
        }

        [ProtoMember(1)]
        public int Id { get; set; }
        [ProtoMember(2)]
        public string FolderName { get; set; }
        [ProtoMember(3)]
        public List<string> Extensions { get; set; }
        [ProtoMember(4)]
        public bool Subdirectory { get; set; }

        [SterlingIgnore]
        public string SearchPattern
        {
            get { return Extensions.Aggregate((s1, s2) => Combine(s1, ";", s2)); }
        }

        public void SetExtensions(string extensions)
        {
            if (string.IsNullOrEmpty(extensions)) return;
            var parts = extensions.Split(new[]{';'}, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return;

            Extensions.Clear();
            Extensions.AddRange(parts);
        }

        private string Combine(string s1, string separator, string s2)
        {
            return string.IsNullOrEmpty(s2)
                ? s1
                : string.Format("{0}{1}{2}", s1, separator, s2);
        }
    }
}