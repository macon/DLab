using System.Collections.Generic;
using System.IO;
using System.Linq;
using DLab.Domain;
using ProtoBuf;

namespace DLab.Repositories
{
    public class ClipboardRepo
    {
        public const int MaxClipboardHistory = 100;
        public const string Filename = "ClipboardItems.bin";

        private ClipboardItems ClipboardItems { get; set; }

        public List<ClipboardItem> Items
        {
            get
            {
                if (ClipboardItems != null) return ClipboardItems.Items;

                if (!File.Exists(Filename))
                {
                    ClipboardItems = new ClipboardItems();
                    return ClipboardItems.Items;
                }

                using (var file = File.OpenRead(Filename))
                {
                    ClipboardItems = Serializer.Deserialize<ClipboardItems>(file);
                }
                return ClipboardItems.Items;
            }
        }

        public void Flush()
        {
            using (var file = File.Create(Filename))
            {
                Serializer.Serialize(file, ClipboardItems);
            }
        }

        public void Clear()
        {
            ClipboardItems.Items.Clear();
        }

        public void TrySaveClipboardItem(ClipboardItem instance)
        {
            var res = ClipboardItems.Items;
            var currentCount = res.Count;
            if (currentCount < MaxClipboardHistory)
            {
                instance.Id = currentCount + 1;
                Save(instance);
                return;
            }

            var candidates = res
                .Where(x => x.PasteCount <= instance.PasteCount && x.Clipped < instance.Clipped)
                .Select(x => x);

            var victim = candidates.OrderBy(x => x.PasteCount).ThenBy(x => x.Clipped).FirstOrDefault();
            if (victim == null) return;
            instance.Id = victim.Id;
            Save(instance);
        }

        public void Save(ClipboardItem clipboardItem)
        {
            var existingEntry = ClipboardItems.Items.SingleOrDefault(x => x.Id == clipboardItem.Id);
            if (existingEntry != null)
            {
                ClipboardItems.Items.Remove(existingEntry);
            }
            ClipboardItems.Items.Add(clipboardItem);
        }

    }
}