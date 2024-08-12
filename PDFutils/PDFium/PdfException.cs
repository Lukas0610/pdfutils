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

    public sealed class PdfException : Exception
    {

        public PdfError Error { get; private set; }

        public PdfException()
            : base()
        { }

        public PdfException(PdfError error)
            : this(GetMessage(error))
        {
            Error = error;
        }

        public PdfException(string message)
            : base(message)
        { }

        public PdfException(string message, Exception innerException)
            : base(message, innerException)
        { }

        private static string GetMessage(PdfError error)
            => error switch
            {
                PdfError.Success => "No error",
                PdfError.CannotOpenFile => "File not found or could not be opened",
                PdfError.InvalidFormat => "File not in PDF format or corrupted",
                PdfError.PasswordProtected => "Password required or incorrect password",
                PdfError.UnsupportedSecurityScheme => "Unsupported security scheme",
                PdfError.PageNotFound => "Page not found or content error",
                _ => "Unknown error",
            };

    }

}
