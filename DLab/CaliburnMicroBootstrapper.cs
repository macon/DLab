using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;
using Caliburn.Micro;
using DLab.CatalogData;
using DLab.Domain;
using DLab.ViewModels;
using log4net.Config;
using StructureMap;
using StructureMap.Graph;
using Wintellect.Sterling;
using Wintellect.Sterling.Server.FileSystem;
using LogManager = log4net.LogManager;

namespace DLab
{
    public class CaliburnMicroBootstrapper : BootstrapperBase
    {
        private IContainer _container;
        private SterlingEngine _engine;
        private ISterlingDatabaseInstance _catalogDb;

        public CaliburnMicroBootstrapper()
        {
            StartRuntime();
        }

        protected override void OnExit(object sender, EventArgs e)
        {
            base.OnExit(sender, e);
            _engine.Dispose();
            _catalogDb = null;
        }

        protected override void OnStartup(object sender, StartupEventArgs e)
        {
            base.OnStartup(sender, e);

            InitialiseStorage();

            _container = new Container(x =>
            {
                x.For<IWindowManager>().Use<WindowManager>();
                x.For<IEventAggregator>().Singleton().Use<EventAggregator>();
                x.For<ISterlingDatabaseInstance>().Singleton().Use(_catalogDb);
                x.For<IViewModelFactory>().Use<ViewModelFactory>();
                x.For<SettingsViewModel>().Use<SettingsViewModel>();
                x.For<ICatalog>().Singleton().Use<Catalog>();
                x.For<CommandViewModel>().Use<CommandViewModel>();
                x.For<TabViewModel>().Use<TabViewModel>();
                x.For<ClipboardViewModel>().Use<ClipboardViewModel>();
                x.For<SettingsFolderViewModel>().Use<SettingsFolderViewModel>();
                x.For<SettingsWebViewModel>().Use<SettingsWebViewModel>();

                x.Scan(s =>
                {
                    s.TheCallingAssembly();
                    s.WithDefaultConventions();
                    s.RegisterConcreteTypesAgainstTheFirstInterface();
                    s.AddAllTypesOf<ITabViewModel>();
                });
            });

            _container.Configure(x => x.For<IContainer>().Use(_container));

            EnsureDefaultScanFolders();
            DisplayRootViewFor<ShellViewModel>();
        }

        private void EnsureDefaultScanFolders()
        {
            var catalog = _container.GetInstance<ICatalog>();

            if (!catalog.Folders().Any())
            {
                catalog.CreateDefaultFolders();
            }
        }

        private void InitialiseStorage()
        {
            _engine = new SterlingEngine();
            _engine.Activate();
            _catalogDb = _engine.SterlingDatabase.RegisterDatabase<CatalogDatabaseInstance>(new FileSystemDriver());
        }

        protected override object GetInstance(Type serviceType, string key)
        {
            return string.IsNullOrEmpty(key) 
                ? _container.GetInstance(serviceType) 
                : _container.GetInstance(serviceType, key);
        }

        protected override IEnumerable<object> GetAllInstances(Type serviceType)
        {
            return _container.GetAllInstances(serviceType).Cast<object>();
        }

        protected override void BuildUp(object instance)
        {
            _container.BuildUp(instance);
        }

        protected override IEnumerable<Assembly> SelectAssemblies()
        {
            return new[] { Assembly.GetExecutingAssembly() };
        }

        protected override void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            base.OnUnhandledException(sender, e);
            var logger = LogManager.GetLogger(GetType());
            logger.Error(e.Exception);
        }
    }
}
