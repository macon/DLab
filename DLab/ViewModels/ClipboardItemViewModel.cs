using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
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
            if (DataType == Domain.ClipboardDataType.Text)
            {
                return new DataObject(DataFormats.Text, Text);
            }

            if (DataType == Domain.ClipboardDataType.FileDrop)
            {
                var result = new DataObject();
                result.SetFileDropList(StringToCollection(Text));
//	            var result = new DataObject(DataFormats.FileDrop, StringToArray(Text));

                var strm = new System.IO.MemoryStream();
                strm.WriteByte((byte) DragDropEffects.Copy);
                result.SetData("Preferred Dropeffect", strm);
                return result;
            }

            if (DataType == Domain.ClipboardDataType.Image)
            {
                var result = new DataObject();
                result.SetImage(Image);

                var strm = new System.IO.MemoryStream();
                strm.WriteByte((byte) DragDropEffects.Copy);
                result.SetData("Preferred Dropeffect", strm);
                return result;
            }

            return null;
        }

        private ClipboardItemViewModel(string text)
        {
            _clipboardItem = new ClipboardItem {Text = text};
        }

        private ClipboardItemViewModel(BitmapSource bitmapSource)
        {
            _clipboardItem = new ClipboardItem
            {
                Text = "<img>",
                DataType = Domain.ClipboardDataType.Image
            };

            Image = bitmapSource;
        }

        private ClipboardItemViewModel(IEnumerable<string> fileDropList)
        {
            _clipboardItem = new ClipboardItem
            {
                Text = fileDropList.Aggregate((s1, s2) => $"{s1}\n{s2}"),
                DataType = Domain.ClipboardDataType.FileDrop
            };
        }

        private string[] StringToArray(string text)
        {
            var textParts = text.Split(new[] {';'}, StringSplitOptions.RemoveEmptyEntries);
            if (textParts.Length == 0)
            {
                return new string[0];
            }

            var result = textParts.ToArray();
            return result;
        }


        private StringCollection StringToCollection(string text)
        {
            var result = new StringCollection();
            var textParts = text.Split(new[] {'\n'}, StringSplitOptions.RemoveEmptyEntries);
            if (textParts.Length == 0)
            {
                return result;
            }

            result.AddRange(textParts);
            return result;
        }

        private string CollectionToString(ICollection collection)
        {
            if (collection.Count == 0)
            {
                return "";
            }
            var sb = new StringBuilder();

            foreach (var item in collection)
            {
                sb.Append($"{item};");
            }
            return sb.ToString().TrimEnd(';');
        }

        public static ClipboardItemViewModel ByText(string text)
        {
            var result = new ClipboardItemViewModel(text) {DataType = Domain.ClipboardDataType.Text};
            return result;
        }

        public static ClipboardItemViewModel ByFileDropList(IEnumerable<string> fileDropList)
        {
            var result = new ClipboardItemViewModel(fileDropList) {DataType = Domain.ClipboardDataType.FileDrop};
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

        public string DisplayText
        {
            get
            {
                var parts = Text.Split(new[] {'\r', '\n'}, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0)
                {
                    return "";
                }

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
            var result = new ClipboardItemViewModel(text) {DataType = Domain.ClipboardDataType.Text};
            return result;
        }

//	    public static ClipboardItemViewModel MakeFileDropListItem(StringCollection fileDropList)
//	    {
//	        var result = new ClipboardItemViewModel(fileDropList) {DataType = ClipboardDataType.FileDrop};
//	        return result;
//	    }

        public ImageSource Icon
        {
            get
            {
                var uriSource3 = new Uri(@"pack://application:,,,/Dlab;component/Resources/text file1 (1).png");
                var uriSource = new Uri(@"/DLab;component/Resources/text file1 (1).png", UriKind.Relative);
                var uriSource2 = new Uri(@"pack://application:,,,/Dlab;component/Resources/documents7");
                var result = DataType == Domain.ClipboardDataType.Text
                    ? new BitmapImage() {UriSource = uriSource3}
                    : new BitmapImage() {UriSource = uriSource2};
                result.Freeze();
                return result;
            }
        }

        public string ClipboardDataType => DataType.ToString();

        public string ImagePath
        {
            get
            {
                switch (DataType)
                {
                    case Domain.ClipboardDataType.Text:
                        return @"pack://application:,,,/Dlab;component/Resources/text file1 (1).png";
                    case Domain.ClipboardDataType.FileDrop:
                        return @"pack://application:,,,/Dlab;component/Resources/documents7.png";
                    case Domain.ClipboardDataType.Image:
                        return @"pack://application:,,,/Dlab;component/Resources/ancore-26.png";
                    default:
                        return @"pack://application:,,,/Dlab;component/Resources/text file1 (1).png";
                }
                //                var result = DataType == ClipboardDataType.Text
                //                    ? @"pack://application:,,,/Dlab;component/Resources/text file1 (1).png"
                //                    : @"pack://application:,,,/Dlab;component/Resources/documents7.png";
                //	            return result;
            }
        }

        private BitmapImage MakeThumbnailImageFromSource(BitmapSource bitmapSource)
        {
            var encoder = new JpegBitmapEncoder();
            var memoryStream = new MemoryStream();
            var bImg = new BitmapImage();

            encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
            encoder.Save(memoryStream);

            bImg.BeginInit();
            bImg.StreamSource = new MemoryStream(memoryStream.ToArray());

            bImg.DecodePixelWidth = bitmapSource.PixelWidth / 3;
            bImg.DecodePixelHeight = bitmapSource.PixelHeight / 3;
            bImg.EndInit();
            bImg.Freeze();

            memoryStream.Close();

            return bImg;
        }

        public ImageSource ClipboardImage
	    {
	        get
	        {
	            if (DataType != Domain.ClipboardDataType.Image) { return null; }

                // Create a BitmapImage and sets its DecodePixelWidth and DecodePixelHeight
                var bmpImage = MakeThumbnailImageFromSource(Image);
	            return bmpImage;
	        }
	    }

	    public ClipboardItem Instance { get { return _clipboardItem; } }

        public BitmapSource Image { get; set; }

	    public static ClipboardItemViewModel ByImage(BitmapSource bitmapSource)
	    {
	        var result = new ClipboardItemViewModel(bitmapSource) {DataType = Domain.ClipboardDataType.Image};
	        return result;
	    }
	}
}