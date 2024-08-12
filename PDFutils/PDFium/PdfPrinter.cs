/*
 * Apache License Version 2.0, January 2004
 * http://www.apache.org/licenses/
 *
 * Copyright (c) 2023 Sandro Hanea
 * Copyright (c) 2024 Lukas Berger <mail@lukasberger.at>
 */
using System.Drawing.Printing;
using System.IO;
using System.Runtime.Versioning;

namespace PDFutils.PDFium
{

    [SupportedOSPlatform("windows")]
    public class PdfPrinter
    {

        public PageSettings PageSettings { get; }

        public PrinterSettings Settings { get; }

        /// <summary>
        /// Initializes a new instance of the <seealso cref="PdfPrinter"/>, which can be used to print pdf files to the specified printer.
        /// </summary>
        /// <param name="printerName">The name of the printer which will be used.</param>
        public PdfPrinter(string printerName = null)
        {
            Settings = new PrinterSettings()
            {
                PrinterName = printerName
            };

            PageSettings = new PageSettings(Settings)
            {
                Margins = new Margins(0, 0, 0, 0)
            };
        }

        /// <summary>
        /// Will print a file read from the stream using the specified printer previously defined.
        /// </summary>
        /// <param name="document">The PDF document to be printed</param>
        /// <param name="copies">The number of copies to be printed, default 1.</param>
        public void Print(PdfiumDocument document, short copies = 1)
        {
            using var printDocument = document.CreatePrintDocument();

            DoPrint(printDocument, copies, null);
        }

        /// <summary>
        /// Will print a file read from the stream using the specified printer previously defined.
        /// </summary>
        /// <param name="fileStream">The file stream to be printed.</param>
        /// <param name="copies">The number of copies to be printed, default 1.</param>
        /// <param name="documentName">The name of the document in the print queue, if null, default name "document" will be used.</param>
        public void Print(Stream fileStream, short copies = 1, string documentName = null)
        {
            using var document = PdfiumDocument.Load(fileStream);
            using var printDocument = document.CreatePrintDocument();

            DoPrint(printDocument, copies, documentName);
        }

        /// <summary>
        /// Will print the file specified by a path using the printer priviously defined.
        /// </summary>
        /// <param name="fileName">The PDF file path to be printed.</param>
        /// <param name="copies">The number of copies to be printed, default 1.</param>
        /// <param name="documentName">The name of the document in the print queue, if null, default name "document" will be used.</param>
        public void Print(string fileName, short copies = 1, string documentName = null)
        {
            using var document = PdfiumDocument.Load(fileName);
            using var printDocument = document.CreatePrintDocument();

            DoPrint(printDocument, copies, documentName);
        }

        private void DoPrint(PrintDocument printDocument, short copies, string documentName)
        {
            var localSettings = (PrinterSettings)Settings.Clone();
            localSettings.Copies = copies;

            if (documentName != null)
                printDocument.DocumentName = documentName;

            printDocument.PrinterSettings = localSettings;
            printDocument.DefaultPageSettings = PageSettings;
            printDocument.PrintController = new StandardPrintController();
            printDocument.Print();
        }

    }

}
