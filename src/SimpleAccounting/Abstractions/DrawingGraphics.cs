// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Abstractions;

using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Drawing.Printing;

/// <summary>
///     Redirects <see cref="IGraphics"/> into real implementations from .NET framework.
/// </summary>
/// <remarks>
///     The reason for this class is testability.
///     This is why it is excluded from code coverage.
/// </remarks>
[ExcludeFromCodeCoverage]
internal class DrawingGraphics : IGraphics
{
    private readonly PrintPageEventArgs printPageEventArgs;

    public DrawingGraphics(PrintPageEventArgs printPageEventArgs)
    {
        this.printPageEventArgs = printPageEventArgs;
    }

    public bool HasMorePages
    {
        get => this.printPageEventArgs.HasMorePages;
        set => this.printPageEventArgs.HasMorePages = value;
    }

    public SizeF MeasureString(string text, Font font)
    {
        return this.printPageEventArgs.Graphics.MeasureString(text, font);
    }

    public void DrawString(string s, Font font, Brush brush, float x, float y, StringAlignment alignment)
    {
        using var format = new StringFormat { Alignment = alignment };
        this.printPageEventArgs.Graphics.DrawString(s, font, brush, x, y, format);
    }

    public void DrawLine(Pen pen, int x1, int y1, int x2, int y2)
    {
        this.printPageEventArgs.Graphics.DrawLine(pen, x1, y1, x2, y2);
    }
}
