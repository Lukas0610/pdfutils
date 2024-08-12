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

    public delegate void PdfiumResolveEventHandler(object sender, PdfiumResolveEventArgs e);

    public class PdfiumResolveEventArgs : EventArgs
    {
        public string PdfiumFileName { get; set; }
    }

}
