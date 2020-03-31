// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting
{
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Windows;
    using System.Windows.Markup;
    using lg2de.SimpleAccounting.Properties;

    [ExcludeFromCodeCoverage]
    public partial class App
    {
        public App()
        {
            var settings = Settings.Default;
            settings.Upgrade();
        }

        [SuppressMessage(
            "Minor Code Smell",
            "S2325:Methods and properties that don't access instance data should be static",
            Justification = "FP")]
        private void ApplicationStartup(object sender, StartupEventArgs e)
        {
            FrameworkElement.LanguageProperty.OverrideMetadata(
                typeof(FrameworkElement),
                new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)));
        }
    }
}
