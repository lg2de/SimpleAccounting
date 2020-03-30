// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Windows;
    using Caliburn.Micro;
    using lg2de.SimpleAccounting.Abstractions;
    using lg2de.SimpleAccounting.Presentation;
    using lg2de.SimpleAccounting.Reports;

    [ExcludeFromCodeCoverage]
    public class AppBootstrapper : BootstrapperBase
    {
        private readonly SimpleContainer container = new SimpleContainer();

        public AppBootstrapper()
        {
            this.Initialize();

            this.container.Singleton<IWindowManager, WindowManager>();
            this.container.Singleton<IReportFactory, ReportFactory>();
            this.container.Singleton<IMessageBox, MessageBoxWrapper>();
            this.container.Singleton<IFileSystem, FileSystem>();
            this.container.PerRequest<ShellViewModel>();
        }

        protected override object GetInstance(Type service, string key)
        {
            return this.container.GetInstance(service, key);
        }

        protected override IEnumerable<object> GetAllInstances(Type service)
        {
            return this.container.GetAllInstances(service);
        }

        protected override void BuildUp(object instance)
        {
            this.container.BuildUp(instance);
        }

        protected override void OnStartup(object sender, StartupEventArgs e)
        {
            var settings = new Dictionary<string, object>
            {
                { "SizeToContent", SizeToContent.Manual }, { "WindowState", WindowState.Maximized }
            };
            this.DisplayRootViewFor<ShellViewModel>(settings);
        }
    }
}
