// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.Abstractions
{
    using System.Drawing;

    internal interface IGraphics
    {
        bool HasMorePages { get; set; }

        void DrawString(string s, Font font, Brush brush, float x, float y, StringAlignment alignment);

        void DrawLine(Pen pen, int x1, int y1, int x2, int y2);
    }
}
