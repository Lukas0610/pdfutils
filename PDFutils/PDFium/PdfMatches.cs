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

    public sealed class PdfMatches
    {

        public int StartPage { get; private set; }

        public int EndPage { get; private set; }

        public IList<PdfMatch> Items { get; private set; }

        public PdfMatches(int startPage, int endPage, IList<PdfMatch> matches)
        {
            if (matches == null)
                throw new ArgumentNullException("matches");

            StartPage = startPage;
            EndPage = endPage;
            Items = new ReadOnlyCollection<PdfMatch>(matches);
        }

    }

}
