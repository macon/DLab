using System;
using Caliburn.Micro;
using ILog = log4net.ILog;

namespace DLab.Infrastructure
{
    public interface IAppServices
    {
        IEventAggregator EventAggregator { get; set; }
        IWindowManager WindowManager { get; set; }
        ILog Log { get; set; }
    }

    public class AppServices : IAppServices
    {
        public AppServices(IEventAggregator eventAggregator, IWindowManager windowManager, ILog log)
        {
            if (eventAggregator == null) throw new ArgumentNullException("eventAggregator");
            if (windowManager == null) throw new ArgumentNullException("windowManager");
            if (log == null) throw new ArgumentNullException("log");
            EventAggregator = eventAggregator;
            WindowManager = windowManager;
            Log = log;
        }

        public IEventAggregator EventAggregator { get; set; }

        public IWindowManager WindowManager { get; set; }

        public ILog Log { get; set; }
    }
}
