using System;
using System.Drawing;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Caliburn.Micro;

namespace DLab.Domain
{
    public class MatchResult : Screen
    {
        private ImageSource _icon;

        public MatchResult(EntityBase commandModel)
        {
            CommandModel = commandModel;
            Icon = new BitmapImage(new Uri("pack://application:,,,/Resources/App.ico"));
        }

        public MatchResult()
        {
        }

        public CommandType CommandType { get; set; }
        public EntityBase CommandModel { get; private set; }
//        public string Icon { get { return @"D:\dev\Scratch\DLab\DLab\Resources\App.ico"; } }
        public ImageSource Icon
        {
            get { return _icon; }
            set
            {
                _icon = value;
                NotifyOfPropertyChange();
            }
        }

        public BitmapImage Icon2 { get; set; }
    }
}