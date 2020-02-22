// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting
{
    using System.Collections.Specialized;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Windows;
    using System.Windows.Markup;
    using lg2de.SimpleAccounting.Properties;

    [ExcludeFromCodeCoverage]
    public partial class App : Application
    {
        public App()
        {
            Settings.Default.Upgrade();
            if (Settings.Default.RecentProjects == null)
            {
                Settings.Default.RecentProjects = new StringCollection();
            }
        }

        private void ApplicationStartup(object sender, StartupEventArgs e)
        {
            FrameworkElement.LanguageProperty.OverrideMetadata(
                typeof(FrameworkElement),
                new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)));
        }
    }
}
