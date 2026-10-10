// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Presentation;

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;

/// <summary>
///     Replaces the Mica backdrop of the Fluent theme by a neutral background color.
/// </summary>
/// <remarks>
///     The Fluent theme uses a transparent window background and the Mica backdrop (client area and title bar),
///     which is tinted by the desktop wallpaper. We prefer the neutral background color of the theme.
/// </remarks>
[ExcludeFromCodeCoverage(Justification = "This view function cannot be tested.")]
internal static partial class NeutralWindowBackground
{
    private const string BackgroundResourceKey = "SolidBackgroundFillColorBaseBrush";
    private const int DwmWindowAttributeCaptionColor = 35;
    private const int DwmWindowAttributeSystemBackdropType = 38;
    private const int DwmSystemBackdropTypeNone = 1;

    /// <summary>
    ///     Registers the neutral background for all windows, including derived classes like the shell view.
    /// </summary>
    public static void Register()
    {
        EventManager.RegisterClassHandler(
            typeof(Window), FrameworkElement.LoadedEvent, new RoutedEventHandler((sender, _) => Apply(sender)));
    }

    /// <summary>
    ///     Applies the neutral background to all open windows again, e.g. after the theme has been switched.
    /// </summary>
    public static void ApplyToAllWindows()
    {
        foreach (var window in Application.Current?.Windows ?? new WindowCollection())
        {
            Apply(window);
        }
    }

    private static void Apply(object? sender)
    {
        if (sender is not Window window)
        {
            return;
        }

        window.SetResourceReference(Control.BackgroundProperty, BackgroundResourceKey);

        var handle = new WindowInteropHelper(window).Handle;
        if (handle == IntPtr.Zero)
        {
            return;
        }

        // The title bar is drawn by the window manager.
        // The backdrop must be disabled there and the caption gets the same color as the window.
        int backdropType = DwmSystemBackdropTypeNone;
        _ = DwmSetWindowAttribute(handle, DwmWindowAttributeSystemBackdropType, ref backdropType, sizeof(int));
        if (window.TryFindResource(BackgroundResourceKey) is SolidColorBrush brush)
        {
            int colorRef = brush.Color.R | (brush.Color.G << 8) | (brush.Color.B << 16);
            _ = DwmSetWindowAttribute(handle, DwmWindowAttributeCaptionColor, ref colorRef, sizeof(int));
        }
    }

    [LibraryImport("dwmapi.dll")]
    private static partial int DwmSetWindowAttribute(IntPtr handle, int attribute, ref int value, int size);
}
