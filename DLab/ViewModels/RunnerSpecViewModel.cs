using System;
using DLab.Domain;

namespace DLab.ViewModels
{
    public class RunnerSpecViewModel
    {
        public RunnerSpecViewModel()
        {
            Instance = new RunnerSpec();
        }

        public RunnerSpecViewModel(RunnerSpec runnerSpec)
        {
            Instance = runnerSpec;
        }

        public RunnerSpec Instance { get; }
        public int Id
        {
            get { return Instance.Id; }
            set { Instance.Id = value; }
        }

        public string Arguments
        {
            get { return Instance.Arguments; }
            set
            {
                if (!string.IsNullOrEmpty(Instance.Arguments) && Instance.Arguments.Equals(value, StringComparison.InvariantCultureIgnoreCase)) return;
                Instance.Arguments = value;
                IsDirty = true;
            }
        }

        public string Command
        {
            get { return Instance.Command; }
            set
            {
                if (Instance.Command.Equals(value, StringComparison.InvariantCultureIgnoreCase)) return;
                Instance.Command = value;
                IsDirty = true;
            }
        }

        internal bool IsDirty { get; set; }

        public string Target
        {
            get { return Instance.Target; }
            set
            {
                if (Instance.Target.Equals(value, StringComparison.InvariantCultureIgnoreCase)) return;
                Instance.Target = value;
                IsDirty = true;
            }
        }

        public bool Unsaved => Id == default(int);
    }
}