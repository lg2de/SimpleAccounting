using System.Collections.Generic;
using System.Windows;
using Caliburn.Micro;

namespace lg2de.SimpleAccounting
{
    public class AppBootstrapper : BootstrapperBase
    {
        public AppBootstrapper()
        {
            this.Initialize();
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
