using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Caliburn.Micro;
using DLab.Domain;
using Console = DLab.Domain.Console;

namespace DLab.ViewModels
{
    public class SettingsDirViewModel : Screen, ISettingsViewModel
    {
        private readonly IRepository<Console> _repo;
        private ConsoleViewModel _selectedConsole;
        public BindableCollection<ConsoleViewModel> Consoles { get; set; }

        public SettingsDirViewModel(IRepository<Console> repo)
        {
            _repo = repo;
            DisplayName = "Consoles";
            Consoles = new BindableCollection<ConsoleViewModel>();
            Initialise();
        }

        private void Initialise()
        {
            Consoles.Clear();
            var items = _repo.Items;
            Consoles.AddRange(items.Select(x => new ConsoleViewModel(x)));
        }

        public void Add()
        {
            var vm = new ConsoleViewModel(new Console());
            Consoles.Add(vm);
        }

        public void Save()
        {
            foreach (var viewModel in Consoles.Where(x => x.Unsaved))
            {
                viewModel.Instance.SetId();
            }
            _repo.ReplaceAll(Consoles.Select(x => x.Instance).ToList());
            _repo.Flush();
            TryClose();
        }

        public ConsoleViewModel SelectedConsole
        {
            get { return _selectedConsole; }
            set
            {
                _selectedConsole = value;
                NotifyOfPropertyChange();
            }
        }
    }

    public class ConsoleViewModel : Screen
    {
        private readonly Console _console;

        public ConsoleViewModel(Console console)
        {
            if (console == null) throw new ArgumentNullException("console");
            _console = console;
        }

        public int Id
        {
            get { return _console.Id; }
            set { _console.Id = value; }
        }

        public int Priority
        {
            get { return _console.Priority; }
            set { _console.Priority = value; }
        }

        public string Command
        {
            get { return _console.Command; }
            set
            {
                if (Instance.Command.Equals(value, StringComparison.InvariantCultureIgnoreCase)) return;
                Instance.Command = value;
            }
        }

        public string Target
        {
            get { return _console.Target; }
            set
            {
                if (Instance.Target.Equals(value, StringComparison.InvariantCultureIgnoreCase)) return;
                Instance.Target = value;
            }
        }

        public string Arguments
        {
            get { return _console.Arguments; }
            set
            {
                if (Instance.Arguments.Equals(value, StringComparison.InvariantCultureIgnoreCase)) return;
                Instance.Arguments = value;
            }
        }

        public char Hotkey
        {
            get { return _console.Hotkey; }
            set
            {
                if (char.ToLower(value).Equals(Instance.Hotkey)) return;
                _console.Hotkey = char.ToLower(value);
            }
        }

        public bool Unsaved
        {
            get { return Id == default(int); }
        }

        public Console Instance { get { return _console; } }
    }
}
