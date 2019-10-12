﻿// <copyright>
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
            MessageBoxButton button,
            MessageBoxImage icon,
            MessageBoxResult defaultResult,
            MessageBoxOptions options)
        {
            return MessageBox.Show(messageBoxText, caption, button, icon, defaultResult, options);
        }
    }
}