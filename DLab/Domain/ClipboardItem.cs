using System;
using System.Collections.Generic;
using System.Net.Mime;
using ProtoBuf;

namespace DLab.Domain
{
    [ProtoContract]
    public class ClipboardItems
    {
        [ProtoMember(1)]
        public List<ClipboardItem> Items { get; set; }

        public ClipboardItems()
        {
            Items = new List<ClipboardItem>();
        }
    }

    [ProtoContract]
    public class ClipboardItem
    {
        public ClipboardItem()
        {
            Clipped = DateTime.Now;
        }

        [ProtoMember(1)]
        public int Id { get; set; }

        [ProtoMember(2)]
        public string Text { get; set; }

        [ProtoMember(3)]
        public DateTime Clipped { get; set; }

        [ProtoMember(4)]
        public int PasteCount { get; set; }

        [ProtoMember(5)]
        public bool Favourite { get; set; }

        [ProtoMember(6)]
        public ClipboardDataType DataType { get; set; }
    }

    public enum ClipboardDataType
    {
        Text=0,
        FileDrop=1,
        Image=2,
        Unknown = 9
    }
}