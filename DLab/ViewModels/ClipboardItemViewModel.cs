using System;

namespace DLab.ViewModels
{
	public class ClipboardItemViewModel
	{
		public const int LineLength = 200;
		public string Text { get; set; }

		public ClipboardItemViewModel(string text)
		{
			Text = text;
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
	}
}