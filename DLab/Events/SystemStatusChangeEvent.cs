using DLab.ViewModels;

namespace DLab.Events
{
    public class SystemStatusChangeEvent
    {
        public SystemState State { get; private set; }

        public SystemStatusChangeEvent(SystemState state)
        {
            State = state;
        }
    }
}
