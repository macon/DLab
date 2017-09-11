using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Caliburn.Micro;

namespace DLab.ViewModels
{
    public class CommandResultViewModel : Screen
    {
        private bool _hasResults;

        public bool HasResults
        {
            get { return _hasResults; }
            set
            {
                _hasResults = value;
                NotifyOfPropertyChange(() => HasResults);
            }
        }
    }
}
