using Caliburn.Micro;
using DLab.Domain;

namespace DLab.ViewModels
{
    public class NotesViewModel : Screen, ITabViewModel
    {
        private NoteViewModel _selectedNote;
        public BindableCollection<NoteViewModel> Notes { get; set; }

        public NotesViewModel()
        {
            DisplayName = "Notes";

            Notes = new BindableCollection<NoteViewModel>
            {
                new NoteViewModel(new Note()
                {
                    Title = "Sample note",
                    Text = "The quick brown fox etc",
                    Tags = new[] {"git", "tech"},
                    Clips = new[]
                    {
                        new ClipboardItem() {Text = "jumped over"}
                    }
                })
            };
        }

        public NoteViewModel SelectedNote
        {
            get { return _selectedNote; }
            set
            {
                _selectedNote = value;
                NotifyOfPropertyChange();
            }
        }

        public int Order
        {
            get { return 2; }
        }
    }
}
