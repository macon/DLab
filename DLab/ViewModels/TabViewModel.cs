using System.Collections.Generic;
using System.Linq;
using Caliburn.Micro;

namespace DLab.ViewModels
{
    public class TabViewModel : Conductor<ITabViewModel>.Collection.OneActive
    {
        public TabViewModel(IEnumerable<ITabViewModel> tabViewModels)
        {
            Items.AddRange(tabViewModels.OrderBy(x => x.Order));
        }

        public CommandViewModel CommandViewModel
        {
            get { return Items.Single(x => x is CommandViewModel) as CommandViewModel; }
        }

        public ClipboardViewModel ClipboardViewModel
        {
            get { return Items.Single(x => x is ClipboardViewModel) as ClipboardViewModel; }
        }
    }
}
