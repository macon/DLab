using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;
using Caliburn.Micro;
using DLab.Domain;
using DLab.ViewModels;
using log4net.Config;
using StructureMap;
using StructureMap.Graph;
using ILog = log4net.ILog;
using LogManager = log4net.LogManager;

namespace DLab
{
    public class CaliburnMicroBootstrapper : BootstrapperBase
    {
        private IContainer _container;

        public CaliburnMicroBootstrapper()
        {
            StartRuntime();
        }

        protected override void OnStartup(object sender, StartupEventArgs e)
        {
            base.OnStartup(sender, e);

            var fi = new FileInfo("log4net.config");
            XmlConfigurator.Configure(fi);

            _container = new Container(x =>
            {
                x.For<IWindowManager>().Use<WindowManager>();
                x.For<IEventAggregator>().Singleton().Use<EventAggregator>();
                x.For<IViewModelFactory>().Use<ViewModelFactory>();
                x.For<SettingsViewModel>().Use<SettingsViewModel>();
                x.For<CommandViewModel>().Use<CommandViewModel>();
                x.For<TabViewModel>().Use<TabViewModel>();
                x.For<ClipboardViewModel>().Use<ClipboardViewModel>();
                x.For<SettingsFolderViewModel>().Use<SettingsFolderViewModel>();
                x.For<SettingsWebViewModel>().Use<SettingsWebViewModel>();

                x.For<FileCommandsRepo>().Use<FileCommandsRepo>().Singleton();
                x.For<FolderSpecRepo>().Use<FolderSpecRepo>().Singleton();
                x.For<WebSpecRepo>().Use<WebSpecRepo>().Singleton();
                x.For<CommandResolver>().Use<CommandResolver>().Singleton();
                x.For<ILog>().Use(ctx => LogManager.GetLogger("DLab"));

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
            var repo = _container.GetInstance<FolderSpecRepo>();

            if (!repo.Folders.Any())
            {
                repo.CreateDefaultFolders();
            }
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
            var logger = LogManager.GetLogger("DLab");
            logger.Error(e.Exception);
        }
    }
}
