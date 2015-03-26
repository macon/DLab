using System;

namespace DLab.Domain
{
    public class ClipboardItem
    {
        public ClipboardItem()
        {
            Clipped = DateTime.Now;
        }
        public int Id { get; set; }
        public string Text { get; set; }
        public DateTime Clipped { get; set; }
        public int PasteCount { get; set; }
        public bool Favourite { get; set; }
    }
}