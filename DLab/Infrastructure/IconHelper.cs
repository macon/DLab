using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using DLab.Domain;

namespace DLab.Infrastructure
{
    // TODO: create image cache
    public class IconHelper
    {
        public ImageClass<T> GetIcon<T>(T iconable, string path) where T: IIconable
        {
            var result = new ImageClass<T> { Item = iconable };
            var icon = Win32.SafeExtractAssociatedIcon(path);
            if (icon == null) return null;

            result.ImageSource = Imaging.CreateBitmapSourceFromHIcon(
                        icon.Handle,
                        new Int32Rect(0, 0, icon.Width, icon.Height),
                        BitmapSizeOptions.FromEmptyOptions());
            if (result.ImageSource.CanFreeze)
            {
                result.ImageSource.Freeze();
            }
            return result;
        }

    }
}
