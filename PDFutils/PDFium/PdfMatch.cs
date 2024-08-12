/*
 * Apache License Version 2.0, January 2004
 * http://www.apache.org/licenses/
 *
 * Copyright (c) 2023 Sandro Hanea
 * Copyright (c) 2024 Lukas Berger <mail@lukasberger.at>
 */

namespace PDFutils.PDFium
{

    public sealed class PdfMatch
    {

        public string Text { get; }

        public PdfTextSpan TextSpan { get; }

        public int Page { get; }

        public PdfMatch(string text, PdfTextSpan textSpan, int page)
        {
            Text = text;
            TextSpan = textSpan;
            Page = page;
        }

    }

}
