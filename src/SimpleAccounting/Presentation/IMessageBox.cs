// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Presentation
{
    using System.Diagnostics.CodeAnalysis;
    using System.Windows;

    /// <summary>
    ///     Defines access to a message box.
    /// </summary>
    public interface IMessageBox
    {
        [SuppressMessage(
            "Critical Code Smell", "S2360:Optional parameters should not be used",
            Justification = "Signature follows wrapped framework API")]
        MessageBoxResult Show(
            string messageBoxText,
            string caption,
            MessageBoxButton button = MessageBoxButton.OK,
            MessageBoxImage icon = MessageBoxImage.None,
            MessageBoxResult defaultResult = MessageBoxResult.None,
            MessageBoxOptions options = MessageBoxOptions.None);
    }
}
