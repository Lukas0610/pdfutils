/*
 * Apache License Version 2.0, January 2004
 * http://www.apache.org/licenses/
 *
 * Copyright (c) 2023 Sandro Hanea
 * Copyright (c) 2024 Lukas Berger <mail@lukasberger.at>
 */
using System;
using System.Runtime.InteropServices;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Metadata;
using SixLabors.ImageSharp.PixelFormats;

namespace PDFutils.PDFium.Platform
{

    internal abstract class AbstractImageSharpPdfPlatform : IPdfPlatform
    {

        public virtual void Render(PdfiumPage page, System.Drawing.Graphics graphics, float dpiX, float dpiY, System.Drawing.Rectangle bounds, PdfRenderFlags flags)
            => throw new System.NotImplementedException();

        public Image Render(PdfiumPage page, float fWidth, float fHeight, float dpiX, float dpiY, PdfRotation rotate, PdfRenderFlags flags)
        {
            if ((flags & PdfRenderFlags.CorrectFromDpi) != 0)
            {
                fWidth = fWidth * dpiX / 72;
                fHeight = fHeight * dpiY / 72;
            }

            var width = (int)Math.Round(fWidth);
            var height = (int)Math.Round(fHeight);

            unsafe
            {
                var dataPtr = Marshal.AllocHGlobal(width * height * 4);

                try
                {
                    var handle = NativeMethods.FPDFBitmap_CreateEx(width, height, 4, dataPtr, width * 4);

                    try
                    {
                        var background = (flags & PdfRenderFlags.Transparent) == 0 ? 0xFFFFFFFF : 0x00FFFFFF;
                        var renderFormFill = (flags & PdfRenderFlags.Annotations) != 0;
                        var localFlags = FlagsToFPDFFlags(flags);

                        if (renderFormFill)
                            localFlags &= ~NativeMethods.FPDF.ANNOT;

                        NativeMethods.FPDFBitmap_FillRect(handle, 0, 0, width, height, background);
                        NativeMethods.FPDF_RenderPageBitmap(handle, page.Data.PageHandle, 0, 0, width, height, (int)rotate, localFlags);

                        if (renderFormFill)
                            NativeMethods.FPDF_FFLDraw(page.Document.FormHandle, handle, page.Data.PageHandle, 0, 0, width, height, (int)rotate, localFlags);
                    }
                    finally
                    {
                        NativeMethods.FPDFBitmap_Destroy(handle);
                    }

                    var image = Image.LoadPixelData<Bgra32>(
                        new Span<Bgra32>(dataPtr.ToPointer(), width * height),
                        width,
                        height);

                    image.Metadata.ResolutionUnits = PixelResolutionUnit.PixelsPerInch;
                    image.Metadata.HorizontalResolution = dpiX;
                    image.Metadata.VerticalResolution = dpiY;

                    return image;
                }
                finally
                {
                    Marshal.FreeHGlobal(dataPtr);
                }
            }
        }

        public unsafe void Render(PdfiumPage page, Bgra32* scan0, float fWidth, float fHeight, float dpiX, float dpiY, PdfRotation rotate, PdfRenderFlags flags)
        {
            if ((flags & PdfRenderFlags.CorrectFromDpi) != 0)
            {
                fWidth = fWidth * dpiX / 72;
                fHeight = fHeight * dpiY / 72;
            }

            var width = (int)Math.Round(fWidth);
            var height = (int)Math.Round(fHeight);

            var handle = NativeMethods.FPDFBitmap_CreateEx(width, height, 4, new nint(scan0), width * 4);

            try
            {
                var background = (flags & PdfRenderFlags.Transparent) == 0 ? 0xFFFFFFFF : 0x00FFFFFF;
                var renderFormFill = (flags & PdfRenderFlags.Annotations) != 0;
                var localFlags = FlagsToFPDFFlags(flags);

                if (renderFormFill)
                    localFlags &= ~NativeMethods.FPDF.ANNOT;

                NativeMethods.FPDFBitmap_FillRect(handle, 0, 0, width, height, background);
                NativeMethods.FPDF_RenderPageBitmap(handle, page.Data.PageHandle, 0, 0, width, height, (int)rotate, localFlags);

                if (renderFormFill)
                    NativeMethods.FPDF_FFLDraw(page.Document.FormHandle, handle, page.Data.PageHandle, 0, 0, width, height, (int)rotate, localFlags);
            }
            finally
            {
                NativeMethods.FPDFBitmap_Destroy(handle);
            }
        }

        internal static NativeMethods.FPDF FlagsToFPDFFlags(PdfRenderFlags flags)
            => (NativeMethods.FPDF)(flags & ~(PdfRenderFlags.Transparent | PdfRenderFlags.CorrectFromDpi));

    }

}
