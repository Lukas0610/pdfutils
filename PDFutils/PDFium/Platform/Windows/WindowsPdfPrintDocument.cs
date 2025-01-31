﻿/*
 * Apache License Version 2.0, January 2004
 * http://www.apache.org/licenses/
 *
 * Copyright (c) 2023 Sandro Hanea
 * Copyright (c) 2024 Lukas Berger <mail@lukasberger.at>
 */
using System;
using System.Drawing;
using System.Drawing.Printing;
using System.Runtime.Versioning;

namespace PDFutils.PDFium.Platform.Windows
{

    [SupportedOSPlatform("windows")]
    public class WindowsPdfPrintDocument : PrintDocument
    {

        private readonly PdfiumDocument _document;
        private readonly PdfPrintSettings _settings;
        private int _currentPage;

        public event QueryPageSettingsEventHandler BeforeQueryPageSettings;

        protected virtual void OnBeforeQueryPageSettings(QueryPageSettingsEventArgs e)
        {
            BeforeQueryPageSettings?.Invoke(this, e);
        }

        public event PrintPageEventHandler BeforePrintPage;

        protected virtual void OnBeforePrintPage(PrintPageEventArgs e)
        {
            BeforePrintPage?.Invoke(this, e);
        }

        public WindowsPdfPrintDocument(PdfiumDocument document, PdfPrintSettings settings)
        {
            _document = document ?? throw new ArgumentNullException("document");
            _settings = settings;
        }

        protected override void OnBeginPrint(PrintEventArgs e)
        {
            _currentPage = (PrinterSettings.FromPage != 0)
                ? (PrinterSettings.FromPage - 1)
                : 0;

            base.OnBeginPrint(e);
        }

        protected override void OnQueryPageSettings(QueryPageSettingsEventArgs e)
        {
            OnBeforeQueryPageSettings(e);

            // Some printers misreport landscape. The below check verifies
            // whether the page rotation matches the landscape setting.
            bool inverseLandscape = (e.PageSettings.Bounds.Width > e.PageSettings.Bounds.Height) != e.PageSettings.Landscape;

            if (_settings.MultiplePages == null && _currentPage < _document.PageCount)
            {
                bool landscape = GetOrientation(_document.Pages[_currentPage].Size) == Orientation.Landscape;

                if (inverseLandscape)
                    landscape = !landscape;

                e.PageSettings.Landscape = landscape;
            }

            base.OnQueryPageSettings(e);
        }

        protected override void OnPrintPage(PrintPageEventArgs e)
        {
            OnBeforePrintPage(e);

            if (_settings.MultiplePages != null)
                PrintMultiplePages(e);
            else
                PrintSinglePage(e);

            base.OnPrintPage(e);
        }

        private void PrintMultiplePages(PrintPageEventArgs e)
        {
            var settings = _settings.MultiplePages;

            var pagesPerPage = settings.Horizontal * settings.Vertical;
            var pageCount = ((_document.PageCount - 1) / pagesPerPage) + 1;

            if (_currentPage < pageCount)
            {
                var width = e.PageBounds.Width - (e.PageSettings.HardMarginX * 2d);
                var height = e.PageBounds.Height - (e.PageSettings.HardMarginY * 2d);

                var widthPerPage = (width - (((double)settings.Horizontal - 1) * settings.Margin)) / settings.Horizontal;
                var heightPerPage = (height - (((double)settings.Vertical - 1) * settings.Margin)) / settings.Vertical;

                for (var horizontal = 0; horizontal < settings.Horizontal; horizontal++)
                {
                    for (var vertical = 0; vertical < settings.Vertical; vertical++)
                    {
                        var page = _currentPage * pagesPerPage;

                        if (settings.Orientation == PdfOrientation.Horizontal)
                            page += (vertical * settings.Vertical) + horizontal;
                        else
                            page += (horizontal * settings.Horizontal) + vertical;

                        if (page >= _document.PageCount)
                            continue;

                        var pageLeft = (double)(widthPerPage + settings.Margin) * horizontal;
                        var pageTop = (double)(heightPerPage + settings.Margin) * vertical;

                        RenderPage(e, page, pageLeft, pageTop, widthPerPage, heightPerPage);
                    }
                }

                _currentPage++;
            }

            if (PrinterSettings.ToPage > 0)
                pageCount = Math.Min(PrinterSettings.ToPage, pageCount);

            e.HasMorePages = _currentPage < pageCount;
        }

        private void PrintSinglePage(PrintPageEventArgs e)
        {
            if (_currentPage < _document.PageCount)
            {
                var pageOrientation = GetOrientation(_document.Pages[_currentPage].Size);
                var printOrientation = GetOrientation(e.PageBounds.Size);

                e.PageSettings.Landscape = pageOrientation == Orientation.Landscape;

                double left;
                double top;
                double width;
                double height;

                if (_settings.Mode == PdfPrintMode.ShrinkToMargin)
                {
                    left = 0;
                    top = 0;
                    width = e.PageBounds.Width - (e.PageSettings.HardMarginX * 2);
                    height = e.PageBounds.Height - (e.PageSettings.HardMarginY * 2);
                }
                else
                {
                    left = -e.PageSettings.HardMarginX;
                    top = -e.PageSettings.HardMarginY;
                    width = e.PageBounds.Width;
                    height = e.PageBounds.Height;
                }

                if (pageOrientation != printOrientation)
                {
                    (height, width) = (width, height);
                    (left, top) = (top, left);
                }

                RenderPage(e, _currentPage, left, top, width, height);
                _currentPage++;
            }

            int pageCount = PrinterSettings.ToPage == 0
                ? _document.PageCount
                : Math.Min(PrinterSettings.ToPage, _document.PageCount);

            e.HasMorePages = _currentPage < pageCount;
        }

        private void RenderPage(PrintPageEventArgs e, int page, double left, double top, double width, double height)
        {
            var size = _document.Pages[page].Size;

            double pageScale = size.Height / size.Width;
            double printScale = height / width;

            double scaledWidth = width;
            double scaledHeight = height;

            if (pageScale > printScale)
                scaledWidth = width * (printScale / pageScale);
            else
                scaledHeight = height * (pageScale / printScale);

            left += (width - scaledWidth) / 2;
            top += (height - scaledHeight) / 2;

            _document.Pages[page].Render(
                e.Graphics,
                e.Graphics.DpiX,
                e.Graphics.DpiY,
                new Rectangle(
                    AdjustDpi(e.Graphics.DpiX, left),
                    AdjustDpi(e.Graphics.DpiY, top),
                    AdjustDpi(e.Graphics.DpiX, scaledWidth),
                    AdjustDpi(e.Graphics.DpiY, scaledHeight)
                ),
                PdfRenderFlags.ForPrinting | PdfRenderFlags.Annotations
            );
        }

        private static int AdjustDpi(double value, double dpi)
        {
            return (int)(value / 100.0 * dpi);
        }

        private Orientation GetOrientation(SizeF pageSize)
        {
            if (pageSize.Height > pageSize.Width)
                return Orientation.Portrait;
            return Orientation.Landscape;
        }

        private enum Orientation
        {
            Portrait,
            Landscape
        }

    }

}
