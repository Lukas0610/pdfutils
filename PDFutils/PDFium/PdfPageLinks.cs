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

namespace PDFutils.PDFium
{

    /// <summary>
    /// Describes all links on a page.
    /// </summary>
    public sealed class PdfPageLinks
    {

        /// <summary>
        /// All links of the page.
        /// </summary>
        public IList<PdfPageLink> Links { get; private set; }

        /// <summary>
        /// Creates a new instance of the PdfPageLinks class.
        /// </summary>
        /// <param name="links">The links on the PDF page.</param>
        public PdfPageLinks(IList<PdfPageLink> links)
        {
            if (links == null)
                throw new ArgumentNullException("links");

            Links = new ReadOnlyCollection<PdfPageLink>(links);
        }

    }

}
