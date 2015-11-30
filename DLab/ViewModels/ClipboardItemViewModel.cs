using System;
using DLab.Domain;
using log4net;
using log4net.Core;

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
			    if (parts.Length == 0) { return ""; }

			    try
			    {
			        return parts.Length > 1 
			            ? $"{TrimLine(parts[0])}\n{TrimLine(parts[1])}{(parts.Length > 2 ? "..." : "")}"
			            : TrimLine(parts[0]);
			    }
                catch (IndexOutOfRangeException e)
                {
                    var logger = LogManager.GetLogger("Default");
                    logger.ErrorFormat(Text);
                    logger.Error(e);
                    return Text.Substring(0, 100);
                }
			}
		}

		private string TrimLine(string text)
		{
			return text.Length > LineLength ? $"{text.Substring(0, LineLength)}..." : text;
		}

	    public bool IsSaved
	    {
	        get { return _clipboardItem.Id != default(int); }
	    }

	    public ClipboardItem Instance { get { return _clipboardItem; } }
	}
}