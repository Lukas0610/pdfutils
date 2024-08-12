/*
 * Apache License Version 2.0, January 2004
 * http://www.apache.org/licenses/
 *
 * Copyright (c) 2023 Sandro Hanea
 * Copyright (c) 2024 Lukas Berger <mail@lukasberger.at>
 */
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

using ImageSharp = SixLabors.ImageSharp;

namespace PDFutils.PDFium
{

    public sealed class PdfiumPage : IDisposable
    {

        private static readonly Encoding FPDFEncoding = new UnicodeEncoding(false, false, false);

        private bool _disposed;

        internal PdfPageData Data;

        public PdfiumDocument Document { get; }

        public int PageNumber { get; }

        public SizeF Size { get; }

        internal PdfiumPage(PdfPageData data, PdfiumDocument document, int pageNumber, SizeF size)
        {
            Data = data;

            Document = document;
            PageNumber = pageNumber;
            Size = size;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            Data?.Dispose();
            Data = null;

            GC.SuppressFinalize(this);

            _disposed = true;
        }

        /// <summary>
        /// Renders the page to the provided graphics instance.
        /// </summary>
        /// <param name="graphics">Graphics instance to render the page on.</param>
        /// <param name="dpiX">Horizontal DPI.</param>
        /// <param name="dpiY">Vertical DPI.</param>
        /// <param name="bounds">Bounds to render the page in.</param>
        /// <param name="forPrinting">Render the page for printing.</param>
        public void Render(Graphics graphics, float dpiX, float dpiY, Rectangle bounds, bool forPrinting)
            => Render(graphics, dpiX, dpiY, bounds, forPrinting ? PdfRenderFlags.ForPrinting : PdfRenderFlags.None);

        /// <summary>
        /// Renders the page to the provided graphics instance.
        /// </summary>
        /// <param name="graphics">Graphics instance to render the page on.</param>
        /// <param name="dpiX">Horizontal DPI.</param>
        /// <param name="dpiY">Vertical DPI.</param>
        /// <param name="bounds">Bounds to render the page in.</param>
        /// <param name="flags">Flags used to influence the rendering.</param>
        public void Render(Graphics graphics, float dpiX, float dpiY, Rectangle bounds, PdfRenderFlags flags)
            => Document.Platform.Render(this, graphics, dpiX, dpiY, bounds, flags);

        /// <summary>
        /// Renders the page to an image.
        /// </summary>
        /// <param name="dpiX">Horizontal DPI.</param>
        /// <param name="dpiY">Vertical DPI.</param>
        /// <param name="forPrinting">Render the page for printing.</param>
        /// <returns>The rendered image.</returns>
        public ImageSharp.Image Render(float dpiX, float dpiY, bool forPrinting)
            => Render(Size.Width, Size.Height, dpiX, dpiY, forPrinting);

        /// <summary>
        /// Renders the page to an image.
        /// </summary>
        /// <param name="dpiX">Horizontal DPI.</param>
        /// <param name="dpiY">Vertical DPI.</param>
        /// <param name="flags">Flags used to influence the rendering.</param>
        /// <returns>The rendered image.</returns>
        public ImageSharp.Image Render(float dpiX, float dpiY, PdfRenderFlags flags)
            => Render(Size.Width, Size.Height, dpiX, dpiY, flags);

        /// <summary>
        /// Renders the page to an image.
        /// </summary>
        /// <param name="page">Number of the page to render.</param>
        /// <param name="width">Width of the rendered image.</param>
        /// <param name="height">Height of the rendered image.</param>
        /// <param name="dpiX">Horizontal DPI.</param>
        /// <param name="dpiY">Vertical DPI.</param>
        /// <param name="forPrinting">Render the page for printing.</param>
        /// <returns>The rendered image.</returns>
        public ImageSharp.Image Render(float width, float height, float dpiX, float dpiY, bool forPrinting)
            => Render(width, height, dpiX, dpiY, forPrinting ? PdfRenderFlags.ForPrinting : PdfRenderFlags.None);

        /// <summary>
        /// Renders the page to an image.
        /// </summary>
        /// <param name="width">Width of the rendered image.</param>
        /// <param name="height">Height of the rendered image.</param>
        /// <param name="dpiX">Horizontal DPI.</param>
        /// <param name="dpiY">Vertical DPI.</param>
        /// <param name="flags">Flags used to influence the rendering.</param>
        /// <returns>The rendered image.</returns>
        public ImageSharp.Image Render(float width, float height, float dpiX, float dpiY, PdfRenderFlags flags)
            => Document.Platform.Render(this, width, height, dpiX, dpiY, 0, flags);

        /// <summary>
        /// Renders the page to an image.
        /// </summary>
        /// <param name="width">Width of the rendered image.</param>
        /// <param name="height">Height of the rendered image.</param>
        /// <param name="dpiX">Horizontal DPI.</param>
        /// <param name="dpiY">Vertical DPI.</param>
        /// <param name="rotate">Rotation.</param>
        /// <param name="flags">Flags used to influence the rendering.</param>
        /// <returns>The rendered image.</returns>
        public ImageSharp.Image Render(float width, float height, float dpiX, float dpiY, PdfRotation rotate, PdfRenderFlags flags)
            => Document.Platform.Render(this, width, height, dpiX, dpiY, rotate, flags);

        /// <summary>
        /// Get all text on the page.
        /// </summary>
        /// <returns>The text on the page.</returns>
        public string GetPdfText()
        {
            var length = NativeMethods.FPDFText_CountChars(Data.TextPageHandle);
            return GetPdfText(0, length);
        }

        /// <summary>
        /// Get text on the page within the specified bounds.
        /// </summary>
        /// <param name="offset">The offset from where to start to get the text from</param>
        /// <param name="length">Number of characters to get</param>
        /// <returns>The text on the page within the specified bounds.</returns>
        public string GetPdfText(int offset, int length)
        {
            var result = new byte[(length + 1) * 2];
            NativeMethods.FPDFText_GetText(Data.TextPageHandle, offset, length, result);
            return FPDFEncoding.GetString(result, 0, length * 2);
        }

        /// <summary>
        /// Returns all links on the page.
        /// </summary>
        /// <param name="size">The size of the page.</param>
        /// <returns>A collection with the links on the page.</returns>
        public PdfPageLinks GetPageLinks()
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().Name);

            var links = new List<PdfPageLink>();

            int link = 0;

            while (NativeMethods.FPDFLink_Enumerate(Data.PageHandle, ref link, out IntPtr annotation))
            {
                var destination = NativeMethods.FPDFLink_GetDest(Data.DocumentHandle, annotation);
                int? target = null;
                string uri = null;

                if (destination != IntPtr.Zero)
                    target = (int)NativeMethods.FPDFDest_GetDestPageIndex(Data.DocumentHandle, destination);

                var action = NativeMethods.FPDFLink_GetAction(annotation);
                if (action != IntPtr.Zero)
                {
                    const uint length = 1024;
                    var sb = new StringBuilder(1024);

                    NativeMethods.FPDFAction_GetURIPath(Data.DocumentHandle, action, sb, length);

                    uri = sb.ToString();
                }

                var rect = new NativeMethods.FS_RECTF();

                if (NativeMethods.FPDFLink_GetAnnotRect(annotation, rect) && (target.HasValue || uri != null))
                {
                    links.Add(new PdfPageLink(
                        new RectangleF(rect.left, rect.top, rect.right - rect.left, rect.bottom - rect.top),
                        target,
                        uri
                    ));
                }
            }

            return new PdfPageLinks(links);
        }


        /// <summary>
        /// Get all bounding rectangles for the text span.
        /// </summary>
        /// <description>
        /// The algorithm used to get the bounding rectangles tries to join
        /// adjacent character bounds into larger rectangles.
        /// </description>
        /// <param name="textSpan">The span to get the bounding rectangles for.</param>
        /// <returns>The bounding rectangles.</returns>
        public IList<PdfRectangle> GetTextBounds(int index, int matchLength)
        {
            var result = new List<PdfRectangle>();
            RectangleF? lastBounds = null;

            for (int i = 0; i < matchLength; i++)
            {
                var bounds = GetBounds(index + i);
                if (bounds.Width == 0 || bounds.Height == 0)
                    continue;

                if (
                    lastBounds.HasValue &&
                    AreClose(lastBounds.Value.Right, bounds.Left) &&
                    AreClose(lastBounds.Value.Top, bounds.Top) &&
                    AreClose(lastBounds.Value.Bottom, bounds.Bottom)
                )
                {
                    var top = Math.Max(lastBounds.Value.Top, bounds.Top);
                    var bottom = Math.Min(lastBounds.Value.Bottom, bounds.Bottom);

                    lastBounds = new RectangleF(
                        lastBounds.Value.Left,
                        top,
                        bounds.Right - lastBounds.Value.Left,
                        bottom - top
                    );

                    result[result.Count - 1] = new PdfRectangle(PageNumber, lastBounds.Value);
                }
                else
                {
                    lastBounds = bounds;
                    result.Add(new PdfRectangle(PageNumber, bounds));
                }
            }

            return result;
        }

        /// <summary>
        /// Convert a point from page coordinates to device coordinates.
        /// </summary>
        /// <param name="point">The point to convert.</param>
        /// <returns>The converted point.</returns>
        public Point PointFromPdf(PointF point)
        {
            NativeMethods.FPDF_PageToDevice(
                Data.PageHandle,
                0,
                0,
                (int)Data.Width,
                (int)Data.Height,
                0,
                point.X,
                point.Y,
                out var deviceX,
                out var deviceY
            );

            return new Point(deviceX, deviceY);
        }

        /// <summary>
        /// Convert a point from device coordinates to page coordinates.
        /// </summary>
        /// <param name="point">The point to convert.</param>
        /// <returns>The converted point.</returns>
        public PointF PointToPdf(Point point)
        {
            NativeMethods.FPDF_DeviceToPage(
                Data.PageHandle,
                0,
                0,
                (int)Data.Width,
                (int)Data.Height,
                0,
                point.X,
                point.Y,
                out var deviceX,
                out var deviceY
            );

            return new PointF((float)deviceX, (float)deviceY);
        }

        /// <summary>
        /// Convert a rectangle from page coordinates to device coordinates.
        /// </summary>
        /// <param name="rect">The rectangle to convert.</param>
        /// <returns>The converted rectangle.</returns>
        public Rectangle RectangleFromPdf(RectangleF rect)
        {
            NativeMethods.FPDF_PageToDevice(
                Data.PageHandle,
                0,
                0,
                (int)Data.Width,
                (int)Data.Height,
                0,
                rect.Left,
                rect.Top,
                out var deviceX1,
                out var deviceY1
            );

            NativeMethods.FPDF_PageToDevice(
                Data.PageHandle,
                0,
                0,
                (int)Data.Width,
                (int)Data.Height,
                0,
                rect.Right,
                rect.Bottom,
                out var deviceX2,
                out var deviceY2
            );

            return new Rectangle(
                deviceX1,
                deviceY1,
                deviceX2 - deviceX1,
                deviceY2 - deviceY1
            );
        }

        /// <summary>
        /// Convert a rectangle from device coordinates to page coordinates.
        /// </summary>
        /// <param name="rect">The rectangle to convert.</param>
        /// <returns>The converted rectangle.</returns>
        public RectangleF RectangleToPdf(Rectangle rect)
        {
            NativeMethods.FPDF_DeviceToPage(
                Data.PageHandle,
                0,
                0,
                (int)Data.Width,
                (int)Data.Height,
                0,
                rect.Left,
                rect.Top,
                out var deviceX1,
                out var deviceY1
            );

            NativeMethods.FPDF_DeviceToPage(
                Data.PageHandle,
                0,
                0,
                (int)Data.Width,
                (int)Data.Height,
                0,
                rect.Right,
                rect.Bottom,
                out var deviceX2,
                out var deviceY2
            );

            return new RectangleF(
                (float)deviceX1,
                (float)deviceY1,
                (float)(deviceX2 - deviceX1),
                (float)(deviceY2 - deviceY1)
            );
        }

        /// <summary>
        /// Rotate the page.
        /// </summary>
        /// <param name="page">The page to rotate.</param>
        /// <param name="rotation">How to rotate the page.</param>
        public void RotatePage(int page, PdfRotation rotation)
            => NativeMethods.FPDFPage_SetRotation(Data.PageHandle, rotation);

        private RectangleF GetBounds(int index)
        {
            NativeMethods.FPDFText_GetCharBox(
                Data.TextPageHandle,
                index,
                out var left,
                out var right,
                out var bottom,
                out var top
            );

            return new RectangleF(
                (float)left,
                (float)top,
                (float)(right - left),
                (float)(bottom - top)
            );
        }

        private static bool AreClose(float p1, float p2)
            => Math.Abs(p1 - p2) < 4f;

    }

}
