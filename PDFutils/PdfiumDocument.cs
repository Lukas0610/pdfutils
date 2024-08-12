/*
 * Apache License Version 2.0, January 2004
 * http://www.apache.org/licenses/
 *
 * Copyright (c) 2023 Sandro Hanea
 * Copyright (c) 2024 Lukas Berger <mail@lukasberger.at>
 */
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

using PDFutils.PDFium;
using PDFutils.PDFium.Platform.Linux;
using PDFutils.PDFium.Platform.MacOS;
using PDFutils.PDFium.Platform.Windows;

using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

using ImageSharp = SixLabors.ImageSharp;

namespace PDFutils
{

    /// <summary>
    /// Provides functionality to render a PDF document.
    /// </summary>
    public sealed class PdfiumDocument : IDisposable
    {

        private static readonly Regex dtRegex = new(@"(?:D:)(?<year>\d\d\d\d)(?<month>\d\d)(?<day>\d\d)(?<hour>\d\d)(?<minute>\d\d)(?<second>\d\d)(?<tz_offset>[+-zZ])?(?<tz_hour>\d\d)?'?(?<tz_minute>\d\d)?'?", RegexOptions.Compiled);
        private static readonly Encoding FPDFEncoding = new UnicodeEncoding(false, false, false);

        private IntPtr _document;
        private IntPtr _form;
        private bool _disposed;
        private NativeMethods.FPDF_FORMFILLINFO _formCallbacks;
        private GCHandle _formCallbacksHandle;
        private Stream _stream;

        private readonly int? _id = null;

        internal readonly IPdfPlatform Platform;

        internal IntPtr DocumentHandle
            => _document;

        internal IntPtr FormHandle
            => _form;

        /// <summary>
        /// Number of pages in the PDF document.
        /// </summary>
        public int PageCount => Pages.Count;

        /// <summary>
        /// Bookmarks stored in this PdfFile
        /// </summary>
        public PdfBookmarkCollection Bookmarks { get; private set; }

        /// <summary>
        /// Size of each page in the PDF document.
        /// </summary>
        public IList<PdfiumPage> Pages { get; private set; }

        /// <summary>
        /// Initializes a new instance of the PdfDocument class with the provided path.
        /// </summary>
        /// <param name="path">Path to the PDF document.</param>
        public static PdfiumDocument Load(string path)
            => Load(path, null);

        /// <summary>
        /// Initializes a new instance of the PdfDocument class with the provided path.
        /// </summary>
        /// <param name="path">Path to the PDF document.</param>
        /// <param name="password">Password for the PDF document.</param>
        public static PdfiumDocument Load(string path, string password)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            return Load(File.OpenRead(path), password);
        }

        /// <summary>
        /// Initializes a new instance of the PdfDocument class with the provided stream.
        /// </summary>
        /// <param name="stream">Stream for the PDF document.</param>
        public static PdfiumDocument Load(Stream stream)
            => Load(stream, null);

        /// <summary>
        /// Initializes a new instance of the PdfDocument class with the provided stream.
        /// </summary>
        /// <param name="stream">Stream for the PDF document.</param>
        /// <param name="password">Password for the PDF document.</param>
        public static PdfiumDocument Load(Stream stream, string password)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            return new PdfiumDocument(stream, password);
        }

        /// <summary>
        /// Initializes a new instance of the PdfDocument class with an empty PDF
        /// </summary>
        //public static PdfiumDocument Create()
        //    => new PdfiumDocument();

        private PdfiumDocument()
        {
            PdfiumLibrary.EnsureLoaded();

            _document = NativeMethods.FPDF_CreateNewDocument();
            if (_document == IntPtr.Zero)
                throw new PdfException((PdfError)NativeMethods.FPDF_GetLastError());

            Bookmarks = new PdfBookmarkCollection();
            Pages = new Collection<PdfiumPage>();

            LoadDocument();

            if (OperatingSystem.IsWindows())
                Platform = new WindowsPdfPlatform();
            else if (OperatingSystem.IsMacOS())
                Platform = new MacOSPdfPlatform();
            else if (OperatingSystem.IsLinux())
                Platform = new LinuxPdfPlatform();
            else
                throw new PlatformNotSupportedException();
        }

        private PdfiumDocument(Stream stream, string password)
        {
            PdfiumLibrary.EnsureLoaded();

            _stream = stream ?? throw new ArgumentNullException(nameof(stream));
            _id = StreamManager.Register(stream);

            _document = NativeMethods.FPDF_LoadCustomDocument(stream, password, _id.Value);
            if (_document == IntPtr.Zero)
                throw new PdfException((PdfError)NativeMethods.FPDF_GetLastError());

            Bookmarks = new PdfBookmarkCollection();
            Pages = new Collection<PdfiumPage>();

            LoadDocument();
            LoadBookmarks();
            LoadPages();

            if (OperatingSystem.IsWindows())
                Platform = new WindowsPdfPlatform();
            else if (OperatingSystem.IsMacOS())
                Platform = new MacOSPdfPlatform();
            else if (OperatingSystem.IsLinux())
                Platform = new LinuxPdfPlatform();
            else
                throw new PlatformNotSupportedException();
        }

        /// <summary>
        /// Creates a <see cref="PrintDocument"/> for the PDF document.
        /// </summary>
        /// <returns></returns>
        public PrintDocument CreatePrintDocument()
            => CreatePrintDocument(PdfPrintMode.CutMargin);

        /// <summary>
        /// Creates a <see cref="PrintDocument"/> for the PDF document.
        /// </summary>
        /// <param name="printMode">Specifies the mode for printing. The default
        /// value for this parameter is CutMargin.</param>
        /// <returns></returns>
        public PrintDocument CreatePrintDocument(PdfPrintMode printMode)
            => CreatePrintDocument(new PdfPrintSettings(printMode, null));

        /// <summary>
        /// Creates a <see cref="PrintDocument"/> for the PDF document.
        /// </summary>
        /// <param name="settings">The settings used to configure the print document.</param>
        /// <returns></returns>
        public PrintDocument CreatePrintDocument(PdfPrintSettings settings)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return new WindowsPdfPrintDocument(this, settings);

            throw new PlatformNotSupportedException();
        }

        /// <summary>
        /// Renders all pages to a single image.
        /// </summary>
        /// <param name="dpiX">Horizontal DPI.</param>
        /// <param name="dpiY">Vertical DPI.</param>
        /// <param name="forPrinting">Render the page for printing.</param>
        /// <returns>The rendered image.</returns>
        public ImageSharp.Image RenderAllPages(float dpiX, float dpiY, bool forPrinting)
            => RenderAllPages(dpiX, dpiY, forPrinting ? PdfRenderFlags.ForPrinting : PdfRenderFlags.None);

        /// <summary>
        /// Renders all pages to a single image.
        /// </summary>
        /// <param name="width">Width of the rendered image.</param>
        /// <param name="height">Height of the rendered image.</param>
        /// <param name="dpiX">Horizontal DPI.</param>
        /// <param name="dpiY">Vertical DPI.</param>
        /// <param name="flags">Flags used to influence the rendering.</param>
        /// <returns>The rendered image.</returns>
        public ImageSharp.Image RenderAllPages(float dpiX, float dpiY, PdfRenderFlags flags)
            => RenderAllPages(dpiX, dpiY, 0, flags);

        /// <summary>
        /// Renders all pages to a single image.
        /// </summary>
        /// <param name="dpiX">Horizontal DPI.</param>
        /// <param name="dpiY">Vertical DPI.</param>
        /// <param name="rotate">Rotation.</param>
        /// <param name="flags">Flags used to influence the rendering.</param>
        /// <returns>The rendered image.</returns>
        public ImageSharp.Image RenderAllPages(float dpiX, float dpiY, PdfRotation rotate, PdfRenderFlags flags)
        {
            if (Pages.Count == 0)
                return null;

            var size = DetermineAllPagesImageSize(dpiX, dpiY);

            var firstWidth = (int)Pages[0].Size.Width;
            if (Pages.Any(x => (int)x.Size.Width != firstWidth))
                return RenderAllPagesIndependentlyAndMerge(size, dpiX, dpiY, rotate, flags);

            return RenderAllPagesOntoSingleImage(size, dpiX, dpiY, rotate, flags);
        }

        /// <summary>
        /// Finds all occurences of text.
        /// </summary>
        /// <param name="text">The text to search for.</param>
        /// <param name="matchCase">Whether to match case.</param>
        /// <param name="wholeWord">Whether to match whole words only.</param>
        /// <returns>All matches.</returns>
        public PdfMatches Search(string text, bool matchCase, bool wholeWord)
            => Search(text, matchCase, wholeWord, 0, PageCount - 1);

        /// <summary>
        /// Finds all occurences of text.
        /// </summary>
        /// <param name="text">The text to search for.</param>
        /// <param name="matchCase">Whether to match case.</param>
        /// <param name="wholeWord">Whether to match whole words only.</param>
        /// <param name="page">The page to search on.</param>
        /// <returns>All matches.</returns>
        public PdfMatches Search(string text, bool matchCase, bool wholeWord, int page)
            => Search(text, matchCase, wholeWord, page, page);

        /// <summary>
        /// Finds all occurences of text.
        /// </summary>
        /// <param name="text">The text to search for.</param>
        /// <param name="matchCase">Whether to match case.</param>
        /// <param name="wholeWord">Whether to match whole words only.</param>
        /// <param name="startPage">The page to start searching.</param>
        /// <param name="endPage">The page to end searching.</param>
        /// <returns>All matches.</returns>
        public PdfMatches Search(string text, bool matchCase, bool wholeWord, int startPage, int endPage)
        {
            var matches = new List<PdfMatch>();

            if (string.IsNullOrEmpty(text))
                return new PdfMatches(startPage, endPage, matches);

            for (int page = startPage; page <= endPage; page++)
            {
                var pageData = Pages[page].Data;

                NativeMethods.FPDF_SEARCH_FLAGS flags = 0;

                if (matchCase)
                    flags |= NativeMethods.FPDF_SEARCH_FLAGS.FPDF_MATCHCASE;

                if (wholeWord)
                    flags |= NativeMethods.FPDF_SEARCH_FLAGS.FPDF_MATCHWHOLEWORD;

                var handle = NativeMethods.FPDFText_FindStart(pageData.TextPageHandle, FPDFEncoding.GetBytes(text), flags, 0);

                try
                {
                    while (NativeMethods.FPDFText_FindNext(handle))
                    {
                        var index = NativeMethods.FPDFText_GetSchResultIndex(handle);
                        var matchLength = NativeMethods.FPDFText_GetSchCount(handle);
                        var result = new byte[(matchLength + 1) * 2];

                        NativeMethods.FPDFText_GetText(pageData.TextPageHandle, index, matchLength, result);

                        var match = FPDFEncoding.GetString(result, 0, matchLength * 2);

                        matches.Add(new PdfMatch(
                            match,
                            new PdfTextSpan(page, index, matchLength),
                            page
                        ));
                    }
                }
                finally
                {
                    NativeMethods.FPDFText_FindClose(handle);
                }
            }

            return new PdfMatches(startPage, endPage, matches);
        }

        /// <summary>
        /// Get metadata information from the PDF document.
        /// </summary>
        /// <returns>The PDF metadata.</returns>
        public PdfInformation GetInformation()
        {
            var pdfInfo = new PdfInformation
            {
                Creator = GetMetaText("Creator"),
                Title = GetMetaText("Title"),
                Author = GetMetaText("Author"),
                Subject = GetMetaText("Subject"),
                Keywords = GetMetaText("Keywords"),
                Producer = GetMetaText("Producer"),
                CreationDate = GetMetaTextAsDate("CreationDate"),
                ModificationDate = GetMetaTextAsDate("ModDate")
            };

            return pdfInfo;
        }

        /// <summary>
        /// Append a new page to the document
        /// </summary>
        /// <param name="size">Dimensions of the new page</param>
        /// <returns>The new page</returns>
        public PdfiumPage AddPage(SizeF size)
            => InsertPage(PageCount, size);

        /// <summary>
        /// Insert a new page at the specified index
        /// </summary>
        /// <param name="page">Index at where to insert the new page</param>
        /// <param name="size">Dimensions of the new page</param>
        /// <returns>The new page</returns>
        public PdfiumPage InsertPage(int page, SizeF size)
        {
            NativeMethods.FPDFPage_New(_document, page, size.Width, size.Height);

            Pages.Insert(page, LoadPage(page));

            return Pages[page];
        }

        /// <summary>
        /// Delete the page from the PDF document.
        /// </summary>
        /// <param name="page">The page to delete.</param>
        public void DeletePage(int page)
        {
            NativeMethods.FPDFPage_Delete(_document, page);
            Pages.RemoveAt(page);
        }

        /// <summary>
        /// Save the PDF document to the specified location.
        /// </summary>
        /// <param name="path">Path to save the PDF document to.</param>
        public void Save(string path)
        {
            if (path == null)
                throw new ArgumentNullException("path");

            using (var stream = File.Create(path))
                Save(stream);
        }

        /// <summary>
        /// Save the PDF document to the specified location.
        /// </summary>
        /// <param name="stream">Stream to save the PDF document to.</param>
        public void Save(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");
            
            NativeMethods.FPDF_SaveAsCopy(_document, stream, NativeMethods.FPDF_SAVE_FLAGS.FPDF_NO_INCREMENTAL);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void LoadDocument()
        {
            NativeMethods.FPDF_GetDocPermissions(_document);

            _formCallbacks = new NativeMethods.FPDF_FORMFILLINFO();
            _formCallbacksHandle = GCHandle.Alloc(_formCallbacks, GCHandleType.Pinned);

            // Depending on whether XFA support is built into the PDFium library, the version
            // needs to be 1 or 2. We don't really care, so we just try one or the other.

            for (int i = 1; i <= 2; i++)
            {
                _formCallbacks.version = i;

                _form = NativeMethods.FPDFDOC_InitFormFillEnvironment(_document, _formCallbacks);
                if (_form != IntPtr.Zero)
                    break;
            }

            NativeMethods.FPDF_SetFormFieldHighlightColor(_form, 0, 0xFFE4DD);
            NativeMethods.FPDF_SetFormFieldHighlightAlpha(_form, 100);
            NativeMethods.FORM_DoDocumentJSAction(_form);
            NativeMethods.FORM_DoDocumentOpenAction(_form);
        }

        private void LoadPages()
        {
            var pageCount = NativeMethods.FPDF_GetPageCount(_document);

            for (int i = 0; i < pageCount; i++)
                Pages.Add(LoadPage(i));
        }

        private PdfiumPage LoadPage(int pageNumber)
        {
            NativeMethods.FPDF_GetPageSizeByIndex(_document, pageNumber, out double width, out double height);

            var pageSize = new SizeF((float)width, (float)height);

            return new PdfiumPage(new PdfPageData(_document, _form, pageNumber), this, pageNumber, pageSize);
        }

        private void LoadBookmarks()
            => LoadBookmarks(Bookmarks, NativeMethods.FPDF_BookmarkGetFirstChild(_document, IntPtr.Zero));

        private void LoadBookmarks(PdfBookmarkCollection collection, IntPtr bookmark)
        {
            if (bookmark == IntPtr.Zero)
                return;

            collection.Add(LoadBookmark(bookmark));

            while ((bookmark = NativeMethods.FPDF_BookmarkGetNextSibling(_document, bookmark)) != IntPtr.Zero)
                collection.Add(LoadBookmark(bookmark));
        }

        private PdfBookmark LoadBookmark(IntPtr bookmark)
        {
            var result = new PdfBookmark
            {
                Title = GetBookmarkTitle(bookmark),
                PageIndex = (int)GetBookmarkPageIndex(bookmark)
            };

            var child = NativeMethods.FPDF_BookmarkGetFirstChild(_document, bookmark);
            if (child != IntPtr.Zero)
                LoadBookmarks(result.Children, child);

            return result;
        }

        private string GetBookmarkTitle(IntPtr bookmark)
        {
            var length = NativeMethods.FPDF_BookmarkGetTitle(bookmark, null, 0);
            var buffer = new byte[length];

            NativeMethods.FPDF_BookmarkGetTitle(bookmark, buffer, length);

            var result = Encoding.Unicode.GetString(buffer);
            if (result.Length > 0 && result[result.Length - 1] == 0)
                result = result.Substring(0, result.Length - 1);

            return result;
        }

        private uint GetBookmarkPageIndex(IntPtr bookmark)
        {
            var dest = NativeMethods.FPDF_BookmarkGetDest(_document, bookmark);
            if (dest != IntPtr.Zero)
                return NativeMethods.FPDFDest_GetDestPageIndex(_document, dest);

            return 0;
        }

        private string GetMetaText(string tag)
        {
            // Length includes a trailing \0.
            var length = NativeMethods.FPDF_GetMetaText(_document, tag, null, 0);
            if (length <= 2)
                return string.Empty;

            var buffer = new byte[length];
            NativeMethods.FPDF_GetMetaText(_document, tag, buffer, length);

            return Encoding.Unicode.GetString(buffer, 0, (int)(length - 2));
        }

        public DateTime? GetMetaTextAsDate(string tag)
        {
            string dt = GetMetaText(tag);

            if (string.IsNullOrEmpty(dt))
            {
                return null;
            }

            Match match = dtRegex.Match(dt);

            if (!match.Success)
            {
                return null;
            }

            var year = match.Groups["year"].Value;
            var month = match.Groups["month"].Value;
            var day = match.Groups["day"].Value;
            var hour = match.Groups["hour"].Value;
            var minute = match.Groups["minute"].Value;
            var second = match.Groups["second"].Value;
            var tzOffset = match.Groups["tz_offset"]?.Value;
            var tzHour = match.Groups["tz_hour"]?.Value;
            var tzMinute = match.Groups["tz_minute"]?.Value;

            string formattedDate = $"{year}-{month}-{day}T{hour}:{minute}:{second}.0000000";

            if (!string.IsNullOrEmpty(tzOffset))
            {
                switch (tzOffset)
                {
                    case "Z":
                    case "z":
                        formattedDate += "+0";
                        break;
                    case "+":
                    case "-":
                        formattedDate += $"{tzOffset}{tzHour}:{tzMinute}";
                        break;
                }
            }

            try
            {
                return DateTime.Parse(formattedDate);
            }
            catch (FormatException)
            {
                return null;
            }

        }

        private Size DetermineAllPagesImageSize(float dpiX, float dpiY)
        {
            var dpiXFactor = dpiX / 72f;
            var dpiYFactor = dpiY / 72f;

            var maxWidth = 0;
            var height = 0;

            foreach (var page in Pages)
            {
                maxWidth = Math.Max(maxWidth, (int)Math.Round(page.Size.Width * dpiXFactor));
                height += (int)Math.Round(page.Size.Height * dpiYFactor);
            }

            return new Size(maxWidth, height);
        }

        private ImageSharp.Image RenderAllPagesOntoSingleImage(Size size, float dpiX, float dpiY, PdfRotation rotate, PdfRenderFlags flags)
        {
            var image = new ImageSharp.Image<Bgra32>(
                new ImageSharp.Configuration()
                {
                    PreferContiguousImageBuffers = true,
                },
                size.Width,
                size.Height);

            if (!image.Frames.RootFrame.DangerousTryGetSinglePixelMemory(out var imageMemory))
                return RenderAllPagesIndependentlyAndMerge(image, size, dpiX, dpiY, rotate, flags);

            var dpiXFactor = dpiX / 72f;
            var dpiYFactor = dpiY / 72f;

            var verticalOffset = 0;

            flags &= ~PdfRenderFlags.CorrectFromDpi;

            foreach (var page in Pages)
            {
                var pageImageWidth = (int)Math.Round(page.Size.Width * dpiXFactor);
                var pageImageHeight = (int)Math.Round(page.Size.Height * dpiYFactor);

                unsafe
                {
                    fixed (Bgra32* pixelDataPtr = imageMemory.Span)
                    {
                        var scan0 = pixelDataPtr + (verticalOffset * size.Width);
                        Platform.Render(page, scan0, pageImageWidth, pageImageHeight, dpiX, dpiY, rotate, flags);
                    }
                }

                verticalOffset += pageImageHeight;
            }

            return image;
        }

        private ImageSharp.Image RenderAllPagesIndependentlyAndMerge(Size size, float dpiX, float dpiY, PdfRotation rotate, PdfRenderFlags flags)
        {
            var image = new ImageSharp.Image<Rgba32>(size.Width, size.Height);
            return RenderAllPagesIndependentlyAndMerge(image, size, dpiX, dpiY, rotate, flags);
        }

        private ImageSharp.Image RenderAllPagesIndependentlyAndMerge(ImageSharp.Image image, Size size, float dpiX, float dpiY, PdfRotation rotate, PdfRenderFlags flags)
        {
            var dpiXFactor = dpiX / 72f;
            var dpiYFactor = dpiY / 72f;

            var maxPageImageWidth = (int)Math.Round(Pages.Max(x => x.Size.Width) * dpiXFactor);
            var maxPageImageHeight = (int)Math.Round(Pages.Max(x => x.Size.Height) * dpiYFactor);

            var pixelData = new Bgra32[maxPageImageWidth * maxPageImageHeight];
            var verticalOffset = 0;

            flags &= ~PdfRenderFlags.CorrectFromDpi;

            foreach (var page in Pages)
            {
                var pageImageWidth = (int)Math.Round(page.Size.Width * dpiXFactor);
                var pageImageHeight = (int)Math.Round(page.Size.Height * dpiYFactor);

                unsafe
                {
                    fixed (Bgra32* pixelDataPtr = pixelData)
                        Platform.Render(page, pixelDataPtr, pageImageWidth, pageImageHeight, dpiX, dpiY, rotate, flags);
                }

                var pixelDataSpan = new ReadOnlySpan<Bgra32>(pixelData, 0, pageImageWidth * pageImageHeight);

                using (var pageImage = ImageSharp.Image.LoadPixelData(pixelDataSpan, pageImageWidth, pageImageHeight))
                    image.Mutate(x => x.DrawImage(pageImage, new ImageSharp.Point((size.Width - pageImageWidth) / 2, verticalOffset), 1f));

                verticalOffset += pageImageHeight;
            }

            return image;
        }

        private void Dispose(bool disposing)
        {
            if (_disposed || !disposing)
                return;

            if (_id.HasValue)
                StreamManager.Unregister(_id.Value);

            if (_form != IntPtr.Zero)
            {
                NativeMethods.FORM_DoDocumentAAction(_form, NativeMethods.FPDFDOC_AACTION.WC);
                NativeMethods.FPDFDOC_ExitFormFillEnvironment(_form);

                _form = IntPtr.Zero;
            }

            if (_document != IntPtr.Zero)
            {
                NativeMethods.FPDF_CloseDocument(_document);
                _document = IntPtr.Zero;
            }

            if (_formCallbacksHandle.IsAllocated)
                _formCallbacksHandle.Free();

            _stream?.Dispose();

            _stream = null;
            _disposed = true;
        }

    }

}
