using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Caliburn.Micro;
using DLab.Domain;

namespace DLab.ViewModels
{
    public class NoteViewModel : Screen
    {
        private readonly Note _note;
        private bool _isSelected;

        public NoteViewModel(Note note)
        {
            if (note == null) throw new ArgumentNullException("note");
            _note = note;
        }

        public string Title
        {
            get { return _note.Title; }
            set { _note.Title = value; }
        }

        public string Text
        {
            get { return _note.Text; }
            set { _note.Text = value; }
        }

        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                _isSelected = value;
            }
        }
    }
}
