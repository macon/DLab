using System;
using DLab.Domain;

namespace DLab.ViewModels
{
	public class ClipboardItemViewModel
	{
	    private readonly ClipboardItem _clipboardItem;
	    public const int LineLength = 200;

	    public ClipboardItemViewModel(ClipboardItem clipboardItem)
	    {
	        _clipboardItem = clipboardItem;
	    }

	    public ClipboardItemViewModel(string text)
		{
            _clipboardItem = new ClipboardItem {Text = text};
		}

	    public int Id
	    {
	        get { return _clipboardItem.Id; }
	        set { _clipboardItem.Id = value; }
	    }

	    public string Text
	    {
	        get { return _clipboardItem.Text; }
	        set { _clipboardItem.Text = value; }
	    }

	    public DateTime Clipped
	    {
	        get { return _clipboardItem.Clipped; }
	        set { _clipboardItem.Clipped = value; }
	    }

        public int PasteCount
        {
            get { return _clipboardItem.PasteCount; }
            set { _clipboardItem.PasteCount = value; }
        }

	    public bool Favourite
	    {
	        get { return _clipboardItem.Favourite; }
	        set { _clipboardItem.Favourite = value; }
	    }

	    public string DisplayText
		{
			get
			{
				var parts = Text.Split(new[]{'\r', '\n'}, StringSplitOptions.RemoveEmptyEntries);

				return parts.Length > 1 
					? string.Format("{0}\n{1}{2}", ParseLine(parts[0]), ParseLine(parts[1]), parts.Length>2 ? "..." : "") 
					: ParseLine(parts[0]);
			}
		}

		private string ParseLine(string text)
		{
			return text.Length > LineLength ? string.Format("{0}...", text.Substring(0, LineLength)) : text;
		}

	    public bool IsSaved
	    {
	        get { return _clipboardItem.Id != default(int); }
	    }

	    public ClipboardItem Instance { get { return _clipboardItem; } }
	}
}