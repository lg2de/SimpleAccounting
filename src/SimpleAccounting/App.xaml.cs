// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting
{
    using System.Configuration;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Windows;
    using System.Windows.Markup;
    using lg2de.SimpleAccounting.Properties;

    [ExcludeFromCodeCoverage]
    public partial class App
    {
        [SuppressMessage(
            "Major Code Smell",
            "S3011:Reflection should not be used to increase accessibility of classes, methods, or fields",
            Justification = "work-around")]
        public App()
        {
            // upgrade settings from older versions
            var settings = Settings.Default;
            settings.Upgrade();

            var provider = Settings.Default.Providers.OfType<LocalFileSettingsProvider>().FirstOrDefault();
            var fileName = provider?.GetType().GetField(
                    "_prevLocalConfigFileName",
                    BindingFlags.Instance | BindingFlags.NonPublic)?
                .GetValue(provider) as string;
            if (File.Exists(fileName))
            {
                // delete configuration of old version
                // ReSharper disable once AssignNullToNotNullAttribute
                Directory.Delete(Path.GetDirectoryName(fileName), recursive: true);
            }
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
