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
            // upgrade settings from older versions
            var settings = Settings.Default;
            settings.Upgrade();
        }

        [SuppressMessage(
            "Minor Code Smell",
            "S2325:Methods and properties that don't access instance data should be static",
            Justification = "FP")]
        private void ApplicationStartup(object sender, StartupEventArgs e)
        {
            if (!string.IsNullOrEmpty(Settings.Default.Culture))
            {
                var culture = CultureInfo.GetCultureInfo(Settings.Default.Culture);
                CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = culture;
            }

            // set control culture according to system culture
            // https://stackoverflow.com/questions/4041197/how-to-set-and-change-the-culture-in-wpf
            FrameworkElement.LanguageProperty.OverrideMetadata(
                typeof(FrameworkElement),
                new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)));
        }
    }
}
