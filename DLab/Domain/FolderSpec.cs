using System;
using System.Collections.Generic;
using System.Linq;
using ProtoBuf;

namespace DLab.Domain
{
    [ProtoContract]
    public class FolderDirectory
    {
        [ProtoMember(1)]
        public List<FolderSpec> Folders { get; set; }

        public FolderDirectory()
        {
            Folders = new List<FolderSpec>();
        }
    }

    [ProtoContract]
    public class FolderSpec
    {
        public FolderSpec()
        {
        }

        public FolderSpec(string folder)
        {
            FolderName = folder;
            Id = FolderName.GetHashCode();
            Extensions = ".exe;.lnk;.bat;.cmd";
        }

        [ProtoMember(1)]
        public int Id { get; set; }

        [ProtoMember(2)]
        public string FolderName { get; set; }

        [ProtoMember(3)]
        public string Extensions { get; set; }

        [ProtoMember(4)]
        public bool Subdirectory { get; set; }

        public List<string> ExtensionList
        {
            get
            {
                if (string.IsNullOrEmpty(Extensions)) return new List<string>();
                var parts = Extensions.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                
                return (parts.Length == 0) ? new List<string>() : parts.ToList();
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