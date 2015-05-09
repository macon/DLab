using System.Collections.Generic;
using ProtoBuf;

namespace DLab.Domain
{
    [ProtoContract]
    public class Note
    {
        [ProtoMember(1)]
        public string Title { get; set; }

        [ProtoMember(2)]
        public string Text { get; set; }

        public IList<CatalogEntry> Commands { get; set; }
        public IList<ClipboardItem> Clips { get; set; }
        public IList<string> Tags { get; set; }
    }
}