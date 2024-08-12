/*
 * Apache License Version 2.0, January 2004
 * http://www.apache.org/licenses/
 *
 * Copyright (c) 2023 Sandro Hanea
 * Copyright (c) 2024 Lukas Berger <mail@lukasberger.at>
 */
using System.Drawing;

using SixLabors.ImageSharp.PixelFormats;

using ImageSharp = SixLabors.ImageSharp;

namespace PDFutils.PDFium
{

    internal interface IPdfPlatform
    {

        void Render(PdfiumPage page, Graphics graphics, float dpiX, float dpiY, Rectangle bounds, PdfRenderFlags flags);

        ImageSharp.Image Render(PdfiumPage page, float width, float height, float dpiX, float dpiY, PdfRotation rotate, PdfRenderFlags flags);

        unsafe void Render(PdfiumPage page, Bgra32* scan0, float width, float height, float dpiX, float dpiY, PdfRotation rotate, PdfRenderFlags flags);

    }

}
