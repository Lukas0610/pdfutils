/*
 * Apache License Version 2.0, January 2004
 * http://www.apache.org/licenses/
 *
 * Copyright (c) 2024 Lukas Berger <mail@lukasberger.at>
 */
using System;
using System.Drawing.Printing;
using System.IO;
using System.Linq;

using CommandLine;

using PDFutils.Console.Verbs.API;

namespace PDFutils.Console.Verbs
{

    [Verb("print", HelpText = "Print a PDF")]
    internal sealed class PrintVerb : IVerb
    {

        [Option('r', "resolution", HelpText = "Resolution to print the PDF at", Default = 96f)]
        public float Resolution { get; set; }

        [Option('s', "paper-size", HelpText = "The name of the paper-size to print the PDF at", Default = "A4")]
        public string PaperSize { get; set; }

        [Value(0, HelpText = "The path to the input-file", Required = true)]
        public string InputFile { get; set; }

        [Value(1, HelpText = "The name of the printer", Required = true)]
        public string PrinterName { get; set; }

        public int Run()
        {
            if (!OperatingSystem.IsWindowsVersionAtLeast(6, 1))
                return -1;

#pragma warning disable CA1416 // Validate platform compatibility

            var inputPath = !Path.IsPathFullyQualified(InputFile)
                ? Path.Combine(Directory.GetCurrentDirectory(), InputFile)
                : InputFile;

            if (!File.Exists(inputPath))
            {
                System.Console.WriteLine("Error: Input-File \"{0}\" not found", inputPath);
                return 1;
            }

            using var pdfDocument = PdfiumDocument.Load(inputPath);
            using var pdfPrintDocument = pdfDocument.CreatePrintDocument();

            pdfPrintDocument.PrintController = new StandardPrintController();
            pdfPrintDocument.PrinterSettings.PrinterName = PrinterName;

            if (!pdfPrintDocument.PrinterSettings.IsValid)
            {
                System.Console.WriteLine("Error: Printer \"{0}\" not found", PrinterName);
                return 1;
            }

            var pdfPageSize = pdfDocument.Pages[0].Size;

            var paperSize = pdfPrintDocument.PrinterSettings.PaperSizes
                .Cast<PaperSize>()
                .First(x => x.PaperName.Equals(PaperSize, StringComparison.OrdinalIgnoreCase));

            if (paperSize != null)
            {
                pdfPrintDocument.DefaultPageSettings.PaperSize = paperSize;
            }
            else
            {
                var pdfPageMaxWidth = 0f;
                var pdfPageMaxHeight = 0f;

                foreach (var page in pdfDocument.Pages)
                {
                    pdfPageMaxWidth = Math.Max(pdfPageMaxWidth, page.Size.Width);
                    pdfPageMaxHeight = Math.Max(pdfPageMaxHeight, page.Size.Height);
                }

                pdfPrintDocument.DefaultPageSettings.PaperSize = new PaperSize("Custom", (int)(pdfPageSize.Width / 72f * 100), (int)(pdfPageSize.Width / 72f * 100));
            }

            var closestPrinterResolution = pdfPrintDocument.PrinterSettings.PrinterResolutions
                .Cast<PrinterResolution>()
                .MinBy(x => Math.Abs(Resolution - x.X));

            if (closestPrinterResolution != null)
                pdfPrintDocument.PrinterSettings.DefaultPageSettings.PrinterResolution = closestPrinterResolution;

            pdfPrintDocument.Print();

            return 0;

#pragma warning restore CA1416 // Validate platform compatibility
        }

    }

}
