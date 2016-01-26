using System.Windows.Media;
using Caliburn.Micro;
using DLab.Domain;

namespace DLab.Infrastructure
{
    public class ImageClass<T> : ViewAware where T: IIconable
    {
        private ImageSource _imageSource;
        public T Item { get; set; }

        public ImageSource ImageSource
        {
            get { return _imageSource; }
            set
            {
                _imageSource = value;
                NotifyOfPropertyChange();
            }
        }
    }
}