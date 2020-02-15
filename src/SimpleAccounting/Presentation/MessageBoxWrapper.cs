// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Presentation
{
    using System.Windows;

    /// <summary>
    ///     Default implementation of <see cref="IMessageBox"/> using <see cref="MessageBox"/>.
    /// </summary>
    internal class MessageBoxWrapper : IMessageBox
    {
        public MessageBoxResult Show(
            string messageBoxText,
            string caption,
            MessageBoxButton button = MessageBoxButton.OK,
            MessageBoxImage icon = MessageBoxImage.None,
            MessageBoxResult defaultResult = MessageBoxResult.None,
            MessageBoxOptions options = MessageBoxOptions.None)
        {
            Application.Current.MainWindow.Activate();
            return MessageBox.Show(
                Application.Current.MainWindow,
                messageBoxText,
                caption,
                button,
                icon,
                defaultResult,
                options);
        }
    }
}