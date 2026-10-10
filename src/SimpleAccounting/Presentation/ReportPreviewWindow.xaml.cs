// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Presentation;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Printing;
using System.Drawing.Text;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using AppResources = lg2de.SimpleAccounting.Properties.Resources;
using Bitmap = System.Drawing.Bitmap;
using Color = System.Drawing.Color;
using Graphics = System.Drawing.Graphics;
using PixelFormat = System.Drawing.Imaging.PixelFormat;
using Rectangle = System.Drawing.Rectangle;
using Size = System.Drawing.Size;

/// <summary>
///     Implements the print preview of the reports in the style of the application (Fluent theme, light or dark).
///     It replaces <see cref="System.Windows.Forms.PrintPreviewDialog" /> which is shown in Windows Forms style.
/// </summary>
/// <remarks>
///     The pages are created by the existing <see cref="PrintDocument" /> using <see cref="PreviewPrintController" />.
///     They are converted into bitmaps on demand, when they become visible.
/// </remarks>
[ExcludeFromCodeCoverage(Justification = "This view cannot be tested.")]
public partial class ReportPreviewWindow
{
    // Pages are rendered with the resolution required by zoom and display scaling, within these limits.
    private const double MinimumRenderDpi = 72;
    private const double MaximumRenderDpi = 300;
    private const double DeviceIndependentDpi = 96;
    private const double HundredthInchPerInch = 100;

    // margins around each page, see ItemTemplate
    private const double HorizontalPageMargin = 2 * 16;
    private const double VerticalPageMargin = 2 * 12;

    // special zoom factors calculated from the visible area
    private const double FitPageFactor = -1;
    private const double FitWidthFactor = 0;

    private readonly PrintDocument document;
    private readonly List<PreviewPage> pages;
    private readonly List<ZoomOption> zoomOptions;

    // delays rendering while the zoom or the window size is changing
    private readonly DispatcherTimer renderTimer;

    private ReportPreviewWindow(string documentName, PrintDocument document)
    {
        this.document = document;

        var controller = new PreviewPrintController { UseAntiAlias = true };
        document.PrintController = controller;
        document.Print();
        this.pages = controller.GetPreviewPageInfo().Select(x => new PreviewPage(x)).ToList();

        this.renderTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(150) };
        this.renderTimer.Tick += (_, _) =>
        {
            this.renderTimer.Stop();
            this.pages.ForEach(x => x.Refresh());
        };

        this.InitializeComponent();

        this.Title = documentName;
        this.PagesView.ItemsSource = this.pages;
        this.PageCountText.Text = string.Format(
            CultureInfo.CurrentUICulture, AppResources.ReportPreview_PageCountX, this.pages.Count);

        this.zoomOptions =
        [
            new ZoomOption(AppResources.ReportPreview_FitPage, FitPageFactor),
            new ZoomOption(AppResources.ReportPreview_FitWidth, FitWidthFactor),
            ..new[] { 50, 75, 100, 125, 150, 200 }.Select(
                x => new ZoomOption(string.Format(CultureInfo.CurrentUICulture, "{0} %", x), x / 100.0))
        ];
        this.ZoomSelection.ItemsSource = this.zoomOptions;
        this.ZoomSelection.SelectedIndex = 0; // whole page

        // the display scaling is known after loading only
        this.Loaded += (_, _) => this.ApplyZoom();
    }

    protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
    {
        base.OnDpiChanged(oldDpi, newDpi);
        this.ApplyZoom();
    }

    /// <summary>
    ///     Shows the preview of the document modal to the main window.
    /// </summary>
    public static void Show(string documentName, PrintDocument document)
    {
        ReportPreviewWindow window;
        Mouse.OverrideCursor = Cursors.Wait;
        try
        {
            window = new ReportPreviewWindow(documentName, document);
        }
        finally
        {
            Mouse.OverrideCursor = null;
        }

        if (Application.Current?.MainWindow is { IsVisible: true } mainWindow)
        {
            window.Owner = mainWindow;
        }

        window.ShowDialog();
    }

    private bool IsFitToWindow => this.ZoomSelection.SelectedItem is ZoomOption { Factor: <= 0 };

    private void ApplyZoom()
    {
        if (this.ZoomSelection.SelectedItem is not ZoomOption option || this.pages.Count == 0)
        {
            return;
        }

        double factor = option.Factor;
        if (factor <= FitWidthFactor)
        {
            // fit the widest page into the visible area (without vertical scroll bar)
            double availableWidth = this.PagesView.ActualWidth - HorizontalPageMargin
                                    - SystemParameters.VerticalScrollBarWidth;
            factor = availableWidth / this.pages.Max(x => x.NaturalWidth);
            if (option.Factor <= FitPageFactor)
            {
                // additionally fit the highest page
                double availableHeight = this.PagesView.ActualHeight - VerticalPageMargin;
                factor = Math.Min(factor, availableHeight / this.pages.Max(x => x.NaturalHeight));
            }

            factor = Math.Max(0.1, factor);
        }

        double deviceScale = VisualTreeHelper.GetDpi(this).DpiScaleX;
        this.pages.ForEach(x => x.SetZoom(factor, deviceScale));
        this.renderTimer.Stop();
        this.renderTimer.Start();
    }

    private void ChangeZoom(int direction)
    {
        double current = this.pages.Count > 0 ? this.pages[0].Zoom : 1;
        var fixedOptions = this.zoomOptions.Where(x => x.Factor > 0).ToList();
        var next = direction > 0
            ? fixedOptions.FirstOrDefault(x => x.Factor > current + 0.001) ?? fixedOptions[^1]
            : fixedOptions.LastOrDefault(x => x.Factor < current - 0.001) ?? fixedOptions[0];
        this.ZoomSelection.SelectedItem = next;
    }

    private void OnZoomSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        this.ApplyZoom();
    }

    private void OnPagesSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (this.IsFitToWindow)
        {
            this.ApplyZoom();
        }
    }

    private void OnPagesMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (Keyboard.Modifiers != ModifierKeys.Control)
        {
            return;
        }

        this.ChangeZoom(e.Delta);
        e.Handled = true;
    }

    private void OnIncreaseZoomExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        this.ChangeZoom(+1);
    }

    private void OnDecreaseZoomExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        this.ChangeZoom(-1);
    }

    private void OnCloseExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        this.Close();
    }

    private void OnPrintExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        using var dialog = new System.Windows.Forms.PrintDialog();
        dialog.Document = this.document;
        dialog.UseEXDialog = true;
        if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
        {
            return;
        }

        // restore the default controller (with status dialog) for real printing
        this.document.PrintController =
            new System.Windows.Forms.PrintControllerWithStatusDialog(new StandardPrintController());
        this.document.Print();
    }

    private sealed record ZoomOption(string Text, double Factor);

    /// <summary>
    ///     Represents a single page of the preview.
    /// </summary>
    /// <remarks>
    ///     The bitmap is not cached: it is rendered when the page becomes visible (virtualization)
    ///     and released when the page is scrolled out of view.
    /// </remarks>
    private sealed class PreviewPage : INotifyPropertyChanged
    {
        private readonly PreviewPageInfo pageInfo;
        private double deviceScale = 1;

        public PreviewPage(PreviewPageInfo pageInfo)
        {
            this.pageInfo = pageInfo;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public double NaturalWidth => ToDeviceIndependent(this.pageInfo.PhysicalSize.Width);

        public double Zoom { get; private set; } = 1;

        public double DisplayWidth => this.NaturalWidth * this.Zoom;

        public double NaturalHeight => ToDeviceIndependent(this.pageInfo.PhysicalSize.Height);

        public double DisplayHeight => this.NaturalHeight * this.Zoom;

        public BitmapSource Image => this.Render();

        public void SetZoom(double zoom, double scale)
        {
            this.Zoom = zoom;
            this.deviceScale = scale;
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.DisplayWidth)));
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.DisplayHeight)));
        }

        /// <summary>
        ///     Renders the page again (if visible) with the resolution of the current zoom.
        /// </summary>
        public void Refresh()
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Image)));
        }

        private static double ToDeviceIndependent(int hundredthInch)
        {
            return hundredthInch / HundredthInchPerInch * DeviceIndependentDpi;
        }

        private BitmapSource Render()
        {
            double dpi = Math.Clamp(
                DeviceIndependentDpi * this.Zoom * this.deviceScale, MinimumRenderDpi, MaximumRenderDpi);
            Size physicalSize = this.pageInfo.PhysicalSize;
            int width = (int)Math.Round(physicalSize.Width / HundredthInchPerInch * dpi);
            int height = (int)Math.Round(physicalSize.Height / HundredthInchPerInch * dpi);

            using var bitmap = new Bitmap(width, height, PixelFormat.Format32bppRgb);
            bitmap.SetResolution((float)dpi, (float)dpi);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(Color.White);
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.DrawImage(this.pageInfo.Image, 0, 0, width, height);
            }

            var data = bitmap.LockBits(
                new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format32bppRgb);
            try
            {
                var source = BitmapSource.Create(
                    width, height, dpi, dpi, PixelFormats.Bgr32, null,
                    data.Scan0, data.Stride * height, data.Stride);
                source.Freeze();
                return source;
            }
            finally
            {
                bitmap.UnlockBits(data);
            }
        }
    }
}
