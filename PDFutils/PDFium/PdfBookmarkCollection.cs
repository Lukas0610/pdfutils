/*
 * Apache License Version 2.0, January 2004
 * http://www.apache.org/licenses/
 *
 * Copyright (c) 2023 Sandro Hanea
 * Copyright (c) 2024 Lukas Berger <mail@lukasberger.at>
 */
using System.Collections.ObjectModel;

namespace PDFutils.PDFium
{

    public sealed class PdfBookmark
    {

        public string Title { get; set; }

        public int PageIndex { get; set; }

        public PdfBookmarkCollection Children { get; }

        public PdfBookmark()
        {
            Children = new PdfBookmarkCollection();
        }

        public override string ToString()
        {
            return Title;
        }

    }

    public sealed class PdfBookmarkCollection : Collection<PdfBookmark> { }

}
