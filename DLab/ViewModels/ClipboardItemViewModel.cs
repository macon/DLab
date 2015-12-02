using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Windows;
using DLab.Domain;
using log4net;
using log4net.Core;

namespace DLab.ViewModels
{
	public class ClipboardItemViewModel
	{
	    private readonly ClipboardItem _clipboardItem;
	    public const int LineLength = 200;
        private StringCollection fileDropList;

        public ClipboardItemViewModel(ClipboardItem clipboardItem)
	    {
	        _clipboardItem = clipboardItem;
	    }

	    public IDataObject GetClipboardData()
	    {
	        if (DataType == ClipboardDataType.Text)
	        {
	            return new DataObject(DataFormats.Text, Text);
	        }

	        if (DataType == ClipboardDataType.FileDrop)
	        {
	            return new DataObject(DataFormats.FileDrop, StringToCollection(Text));
	        }

	        return null;
	    }

	    private ClipboardItemViewModel(string text)
		{
            _clipboardItem = new ClipboardItem {Text = text};
		}

	    private ClipboardItemViewModel(ICollection fileDropList)
		{
            _clipboardItem = new ClipboardItem
            {
                Text = CollectionToString(fileDropList),
                DataType = ClipboardDataType.FileDrop
            };
		}

	    private ICollection StringToCollection(string text)
	    {
	        var result = new StringCollection();
	        var textParts = text.Split(new []{';'}, StringSplitOptions.RemoveEmptyEntries);
            if (textParts.Length == 0) { return result; }

	        result.AddRange(textParts);
	        return result;
	    }

	    private string CollectionToString(ICollection collection)
	    {
	        if (collection.Count == 0) {return "";}
            var sb = new StringBuilder();

	        foreach (var item in collection)
	        {
	            sb.Append($"{item};");
	        }
	        return sb.ToString().TrimEnd(';');
	    }

	    public static ClipboardItemViewModel ByText(string text)
	    {
	        var result = new ClipboardItemViewModel(text) {DataType = ClipboardDataType.Text};
	        return result;
	    }

	    public static ClipboardItemViewModel ByFileDropList(StringCollection fileDropList)
	    {
	        var result = new ClipboardItemViewModel(fileDropList) {DataType = ClipboardDataType.FileDrop};
	        return result;
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

	    public ClipboardDataType DataType
	    {
	        get { return _clipboardItem.DataType; }
	        set { _clipboardItem.DataType = value; }
	    }

	    public ClipboardDataType DataType
	    {
	        get { return _clipboardItem.DataType; }
	        set { _clipboardItem.DataType = value; }
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

	    public static ClipboardItemViewModel MakeTextItem(string text)
	    {
	        var result = new ClipboardItemViewModel(text) {DataType = ClipboardDataType.Text};
	        return result;
	    }

	    public static ClipboardItemViewModel MakeFileDropListItem(StringCollection fileDropList)
	    {
	        var result = new ClipboardItemViewModel(fileDropList) {DataType = ClipboardDataType.FileDropList};
	        return result;
	    }

	    public ClipboardItem Instance { get { return _clipboardItem; } }
	}
}