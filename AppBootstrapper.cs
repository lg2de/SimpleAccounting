// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Windows;
using Caliburn.Micro;

namespace lg2de.SimpleAccounting
{
    public class AppBootstrapper : BootstrapperBase
    {
        private SimpleContainer container = new SimpleContainer();

        public AppBootstrapper()
        {
            this.Initialize();

            this.container.Singleton<IWindowManager, WindowManager>();
            this.container.PerRequest<ShellViewModel>();
        }

        protected override object GetInstance(Type serviceType, string key)
        {
            return this.container.GetInstance(serviceType, key);
        }

        protected override IEnumerable<object> GetAllInstances(Type serviceType)
        {
            return this.container.GetAllInstances(serviceType);
        }

        protected override void BuildUp(object instance)
        {
            this.container.BuildUp(instance);
        }

        protected override void OnStartup(object sender, StartupEventArgs e)
        {
            var settings = new Dictionary<string, object>();
            settings.Add("SizeToContent", SizeToContent.Manual);
            settings.Add("WindowState", WindowState.Maximized);
            this.DisplayRootViewFor<ShellViewModel>(settings);
        }
    }
}
