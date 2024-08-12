/*
 * Apache License Version 2.0, January 2004
 * http://www.apache.org/licenses/
 *
 * Copyright (c) 2023 Sandro Hanea
 * Copyright (c) 2024 Lukas Berger <mail@lukasberger.at>
 */
using System.Drawing;

namespace PDFutils.PDFium
{

    /// <summary>
    /// Describes a link on a page.
    /// </summary>
    public sealed class PdfPageLink
    {

        /// <summary>
        /// The location of the link.
        /// </summary>
        public RectangleF Bounds { get; private set; }

        /// <summary>
        /// The target of the link.
        /// </summary>
        public int? TargetPage { get; private set; }

        /// <summary>
        /// The target URI of the link.
        /// </summary>
        public string Uri { get; private set; }

        /// <summary>
        /// Creates a new instance of the PdfPageLink class.
        /// </summary>
        /// <param name="bounds">The location of the link</param>
        /// <param name="targetPage">The target page of the link</param>
        /// <param name="uri">The target URI of the link</param>
        public PdfPageLink(RectangleF bounds, int? targetPage, string uri)
        {
            Bounds = bounds;
            TargetPage = targetPage;
            Uri = uri;
        }

    }

}
