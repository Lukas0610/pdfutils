/*
 * Apache License Version 2.0, January 2004
 * http://www.apache.org/licenses/
 *
 * Copyright (c) 2023 Sandro Hanea
 * Copyright (c) 2024 Lukas Berger <mail@lukasberger.at>
 */
using System;
using System.Drawing;
using System.Runtime.Versioning;

namespace PDFutils.PDFium.Platform.Windows
{

    [SupportedOSPlatform("windows")]
    internal sealed class WindowsPdfPlatform : AbstractImageSharpPdfPlatform
    {

        public override void Render(PdfiumPage page, Graphics graphics, float dpiX, float dpiY, Rectangle bounds, PdfRenderFlags flags)
        {
            if (graphics == null)
                throw new ArgumentNullException("graphics");

            float graphicsDpiX = graphics.DpiX;
            float graphicsDpiY = graphics.DpiY;

            var dc = graphics.GetHdc();

            try
            {
                if ((int)graphicsDpiX != (int)dpiX || (int)graphicsDpiY != (int)dpiY)
                {
                    var transform = new NativeMethods.XFORM
                    {
                        eM11 = graphicsDpiX / dpiX,
                        eM22 = graphicsDpiY / dpiY
                    };

                    NativeMethods.SetGraphicsMode(dc, NativeMethods.GM_ADVANCED);
                    NativeMethods.ModifyWorldTransform(dc, ref transform, NativeMethods.MWT_LEFTMULTIPLY);
                }

                NativeMethods.SetViewportOrgEx(dc, bounds.X, bounds.Y, out var point);
                NativeMethods.FPDF_RenderPage(dc, page.Data.PageHandle, 0, 0, bounds.Width, bounds.Height, 0, FlagsToFPDFFlags(flags));
                NativeMethods.SetViewportOrgEx(dc, point.X, point.Y, out _);
            }
            finally
            {
                graphics.ReleaseHdc(dc);
            }
        }

    }

}
