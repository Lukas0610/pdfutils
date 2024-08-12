/*
 * Apache License Version 2.0, January 2004
 * http://www.apache.org/licenses/
 *
 * Copyright (c) 2023 Sandro Hanea
 * Copyright (c) 2024 Lukas Berger <mail@lukasberger.at>
 */
using System;

namespace PDFutils
{

    /// <summary>
    /// Contains text from metadata of the document.
    /// </summary>
    public sealed class PdfInformation
    {

        public string Author { get; set; }

        public string Creator { get; set; }

        public DateTime? CreationDate { get; set; }

        public string Keywords { get; set; }

        public DateTime? ModificationDate { get; set; }

        public string Producer { get; set; }

        public string Subject { get; set; }

        public string Title { get; set; }

    }

}
