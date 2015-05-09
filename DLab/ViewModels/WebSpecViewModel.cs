using System;
using DLab.Domain;

namespace DLab.ViewModels
{
    public class WebSpecViewModel
    {
        private readonly WebSpec _webSpec;

        public WebSpecViewModel(WebSpec webSpec)
        {
            _webSpec = webSpec;
        }

        public WebSpec Instance
        {
            get { return _webSpec; }
        }

        public int Id
        {
            get { return _webSpec.Id; }
            set { _webSpec.Id = value; }
        }

        public string Arguments
        {
            get { return _webSpec.Arguments; }
            set
            {
                if (!string.IsNullOrEmpty(_webSpec.Arguments) && _webSpec.Arguments.Equals(value, StringComparison.InvariantCultureIgnoreCase)) return;
                _webSpec.Arguments = value;
                IsDirty = true;
            }
        }

        public string Command
        {
            get { return _webSpec.Command; }
            set
            {
                if (_webSpec.Command.Equals(value, StringComparison.InvariantCultureIgnoreCase)) return;
                _webSpec.Command = value;
                IsDirty = true;
            }
        }

        internal bool IsDirty { get; set; }

        public string Uri
        {
            get { return _webSpec.Uri; }
            set
            {
                if (_webSpec.Uri.Equals(value, StringComparison.InvariantCultureIgnoreCase)) return;
                _webSpec.Uri = value;
                IsDirty = true;
            }
        }

        public bool Unsaved
        {
            get { return Id == default(int); }
        }
    }
}