using System;

namespace DLab.Views
{
    public class ClipboardChangedEventArgs : EventArgs
    {
        public readonly System.Windows.IDataObject DataObject;

        public ClipboardChangedEventArgs(System.Windows.IDataObject dataObject)
        {
            DataObject = dataObject;
        }
    }
}