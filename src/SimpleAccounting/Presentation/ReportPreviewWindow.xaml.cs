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

    // limits the zoom factor fitting the page into a tiny window
    private const double MinimumFitFactor = 0.1;

    // tolerance to compare the current zoom factor with the fixed ones
    private const double ZoomTolerance = 0.001;

    // fixed zoom factors offered in addition, in percent
    private const double PercentPerFactor = 100;
    private static readonly int[] ZoomPercentages = [50, 75, 100, 125, 150, 200, 300];

    private readonly PrintDocument document;
    private readonly List<ZoomOption> zoomOptions;

    // delays rendering while the zoom or the window size is changing
    private readonly DispatcherTimer renderTimer;

    private List<PreviewPage> pages = [];

    // suppresses the handling of selection changes while the selection is updated by code
    private bool isUpdatingPaperSelection;

    private ReportPreviewWindow(string documentName, PrintDocument document)
    {
        this.document = document;

        this.renderTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(150) };
        this.renderTimer.Tick += (_, _) =>
        {
            this.renderTimer.Stop();
            this.pages.ForEach(x => x.Refresh());
        };

        this.InitializeComponent();

        this.Title = documentName;
        this.UpdatePaperSelection();
        this.CreatePages();

        this.zoomOptions =
        [
            new ZoomOption(AppResources.ReportPreview_FitPage, FitPageFactor),
            new ZoomOption(AppResources.ReportPreview_FitWidth, FitWidthFactor),
            ..ZoomPercentages.Select(
                x => new ZoomOption(
                    string.Format(CultureInfo.CurrentUICulture, "{0} %", x), x / PercentPerFactor))
        ];
        this.ZoomSelection.ItemsSource = this.zoomOptions;
        this.ZoomSelection.SelectedIndex = 0; // whole page

        // The visible area and the display scaling are known after loading only.
        this.Loaded += (_, _) => this.ApplyZoomAndRender();
    }

    protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
    {
        base.OnDpiChanged(oldDpi, newDpi);
        this.ApplyZoom();
    }

    protected override void OnClosed(EventArgs e)
    {
        this.renderTimer.Stop();
        this.pages.ForEach(x => x.Dispose());
        base.OnClosed(e);
    }

    /// <summary>
    ///     Shows the preview of the document modal to the main window.
    /// </summary>
    public static void Show(string documentName, PrintDocument document)
    {
        var owner = Application.Current?.MainWindow is { IsVisible: true } mainWindow ? mainWindow : null;

        ReportPreviewWindow window;
        Mouse.OverrideCursor = Cursors.Wait;
        try
        {
            window = new ReportPreviewWindow(documentName, document);
        }
        catch (InvalidPrinterException exception)
        {
            // the preview requires the (default) printer to be available
            Mouse.OverrideCursor = null;
            ShowPrinterError(owner, exception);
            return;
        }
        finally
        {
            Mouse.OverrideCursor = null;
        }

        window.Owner = owner;
        window.ShowDialog();
    }

    private bool IsFitToWindow => this.ZoomSelection.SelectedItem is ZoomOption { Factor: <= 0 };

    private void ApplyZoom()
    {
        // before loading, the size of the visible area is unknown
        if (!this.IsLoaded || this.ZoomSelection.SelectedItem is not ZoomOption option || this.pages.Count == 0)
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

            factor = Math.Max(MinimumFitFactor, factor);
        }

        double deviceScale = VisualTreeHelper.GetDpi(this).DpiScaleX;
        this.pages.ForEach(x => x.SetZoom(factor, deviceScale));
        this.renderTimer.Stop();
        this.renderTimer.Start();
    }

    /// <summary>
    ///     Applies the zoom and renders the visible pages without delay.
    /// </summary>
    private void ApplyZoomAndRender()
    {
        this.ApplyZoom();
        this.renderTimer.Stop();
        this.pages.ForEach(x => x.Refresh());
    }

    /// <summary>
    ///     Creates the pages of the preview using the current printer and page settings.
    /// </summary>
    private void CreatePages()
    {
        var oldPages = this.pages;

        var controller = new PreviewPrintController { UseAntiAlias = true };
        this.document.PrintController = controller;
        this.document.Print();
        this.pages = controller.GetPreviewPageInfo().Select(x => new PreviewPage(x)).ToList();

        this.PagesView.ItemsSource = this.pages;
        this.PageCountText.Text = string.Format(
            CultureInfo.CurrentUICulture, AppResources.ReportPreview_PageCountX, this.pages.Count);
        oldPages.ForEach(x => x.Dispose());

        if (this.IsLoaded)
        {
            this.ApplyZoomAndRender();
        }
    }

    private void RecreatePages()
    {
        Mouse.OverrideCursor = Cursors.Wait;
        try
        {
            this.CreatePages();
        }
        finally
        {
            Mouse.OverrideCursor = null;
        }
    }

    /// <summary>
    ///     Updates the selection of the paper according to the settings of the document.
    /// </summary>
    /// <remarks>
    ///     The paper sizes are offered by the current printer (selected in the print dialog).
    ///     The paper defined by the report is offered additionally if the printer does not support it.
    /// </remarks>
    private void UpdatePaperSelection()
    {
        this.isUpdatingPaperSelection = true;
        try
        {
            var currentPaper = this.document.DefaultPageSettings.PaperSize;
            var paperSizes = this.document.PrinterSettings.PaperSizes.Cast<PaperSize>().ToList();
            var selectedPaper =
                paperSizes.Find(x => x.PaperName == currentPaper.PaperName)
                ?? paperSizes.Find(x => x.Width == currentPaper.Width && x.Height == currentPaper.Height);
            if (selectedPaper == null)
            {
                selectedPaper = currentPaper;
                paperSizes.Insert(0, currentPaper);
            }

            this.PaperSelection.ItemsSource = paperSizes;
            this.PaperSelection.SelectedItem = selectedPaper;
        }
        finally
        {
            this.isUpdatingPaperSelection = false;
        }
    }

    private static void ShowPrinterError(Window? owner, Exception exception)
    {
        MessageDialog.Show(
            owner, exception.Message, AppResources.Action_Print,
            MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.None);
    }

    private void OnPaperSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (this.isUpdatingPaperSelection || this.PaperSelection.SelectedItem is not PaperSize paperSize)
        {
            return;
        }

        this.document.DefaultPageSettings.PaperSize = paperSize;
        this.RecreatePages();
    }

    private void ChangeZoom(int direction)
    {
        double current = this.pages.Count > 0 ? this.pages[0].Zoom : 1;
        var fixedOptions = this.zoomOptions.Where(x => x.Factor > 0).ToList();
        var next = direction > 0
            ? fixedOptions.Find(x => x.Factor > current + ZoomTolerance) ?? fixedOptions[^1]
            : fixedOptions.FindLast(x => x.Factor < current - ZoomTolerance) ?? fixedOptions[0];
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
        string printerName = this.document.PrinterSettings.PrinterName;
        string paperName = this.document.DefaultPageSettings.PaperSize.PaperName;
        bool landscape = this.document.DefaultPageSettings.Landscape;

        using var dialog = new System.Windows.Forms.PrintDialog();
        dialog.Document = this.document;
        dialog.UseEXDialog = true;
        if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
        {
            return;
        }

        try
        {
            // the printer or the paper may be changed in the dialog, the preview shows the printed result
            if (printerName != this.document.PrinterSettings.PrinterName
                || paperName != this.document.DefaultPageSettings.PaperSize.PaperName
                || landscape != this.document.DefaultPageSettings.Landscape)
            {
                this.UpdatePaperSelection();
                this.RecreatePages();
            }

            // restore the default controller (with status dialog) for real printing
            this.document.PrintController =
                new System.Windows.Forms.PrintControllerWithStatusDialog(new StandardPrintController());
            this.document.Print();
        }
        catch (InvalidPrinterException exception)
        {
            ShowPrinterError(this, exception);
        }
    }

    private sealed record ZoomOption(string Text, double Factor);

    /// <summary>
    ///     Represents a single page of the preview.
    /// </summary>
    /// <remarks>
    ///     The bitmap is not cached: it is rendered when the page becomes visible (virtualization)
    ///     and released when the page is scrolled out of view.
    /// </remarks>
    private sealed class PreviewPage : INotifyPropertyChanged, IDisposable
    {
        private readonly PreviewPageInfo pageInfo;
        private double deviceScale = 1;

        // the page is not rendered before the zoom fitting the visible area has been applied
        private bool isZoomApplied;

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

        public BitmapSource? Image => this.isZoomApplied ? this.Render() : null;

        public void Dispose()
        {
            // the page is stored as metafile which holds a GDI handle
            this.pageInfo.Image.Dispose();
        }

        public void SetZoom(double zoom, double scale)
        {
            this.Zoom = zoom;
            this.deviceScale = scale;
            this.isZoomApplied = true;
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
