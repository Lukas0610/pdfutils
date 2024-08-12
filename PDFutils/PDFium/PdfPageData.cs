/*
 * Apache License Version 2.0, January 2004
 * http://www.apache.org/licenses/
 *
 * Copyright (c) 2023 Sandro Hanea
 * Copyright (c) 2024 Lukas Berger <mail@lukasberger.at>
 */
using System;

namespace PDFutils.PDFium
{

    internal sealed class PdfPageData : IDisposable
    {

        private readonly IntPtr _form;
        private bool _disposed;

        public IntPtr DocumentHandle { get; }

        public IntPtr PageHandle { get; }

        public IntPtr TextPageHandle { get; }

        public int PageNumber { get; }

        public double Width { get; }

        public double Height { get; }

        public PdfPageData(IntPtr document, IntPtr form, int pageNumber)
        {
            _form = form;

            DocumentHandle = document;
            PageHandle = NativeMethods.FPDF_LoadPage(document, pageNumber);
            TextPageHandle = NativeMethods.FPDFText_LoadPage(PageHandle);

            NativeMethods.FORM_OnAfterLoadPage(PageHandle, form);
            NativeMethods.FORM_DoPageAAction(PageHandle, form, NativeMethods.FPDFPAGE_AACTION.OPEN);

            PageNumber = pageNumber;
            Width = NativeMethods.FPDF_GetPageWidth(PageHandle);
            Height = NativeMethods.FPDF_GetPageHeight(PageHandle);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            NativeMethods.FORM_DoPageAAction(PageHandle, _form, NativeMethods.FPDFPAGE_AACTION.CLOSE);
            NativeMethods.FORM_OnBeforeClosePage(PageHandle, _form);
            NativeMethods.FPDFText_ClosePage(TextPageHandle);
            NativeMethods.FPDF_ClosePage(PageHandle);

            _disposed = true;
        }

    }

}
