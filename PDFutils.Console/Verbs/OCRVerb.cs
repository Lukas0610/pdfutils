/*
 * Apache License Version 2.0, January 2004
 * http://www.apache.org/licenses/
 *
 * Copyright (c) 2024 Lukas Berger <mail@lukasberger.at>
 */
using System;
using System.IO;
using System.Reflection;

using CommandLine;

using DeepL;

using PdfSharp.Drawing;
using PdfSharp.Drawing.Layout;
using PdfSharp.Pdf;

using PDFutils.Console.Verbs.API;
using PDFutils.OCR;
using PDFutils.OCR.Tesseract;
using PDFutils.PDFsharp;

using SixLabors.ImageSharp;

namespace PDFutils.Console.Verbs
{

    [Verb("ocr", HelpText = "Perform OCR on a PDF or image file")]
    internal sealed class OCRVerb : IVerb
    {

        private const double OCROutputFontScalingGranularity = .25d;

        [Option('f', "overwrite", HelpText = "Force overwriting existing output-files", Default = false)]
        public bool Overwrite { get; set; }

        [Option('l', "language", HelpText = "The language of the text in the provided file", Required = true)]
        public string Language { get; set; }

        [Option('t', "translate-from", HelpText = "The ISO-formatted language to translate the text from", Default = null)]
        public string TranslateFrom { get; set; }

        [Option('T', "translate-to", HelpText = "The ISO-formatted language to translate the text into", Default = null)]
        public string TranslateTo { get; set; }

        [Option('r', "resolution", HelpText = "Resolution used for rendering PDF documents before performing OCR", Default = 96f)]
        public float Resolution { get; set; }

        [Option("deepl-api-key", HelpText = "The DeepL API key required to perform translations", Default = null)]
        public string DeepLAPIKey { get; set; }

        [Option("tessdata-path", HelpText = "The path to the Tesseract OCR language- and training-data", Default = "tessdata")]
        public string TessDataPath { get; set; }

        [Value(0, HelpText = "The path to the input-file", Required = true)]
        public string InputFile { get; set; }

        [Value(1, HelpText = "The path to the output-file", Required = true)]
        public string OutputFile { get; set; }

        public int Run()
        {
            var inputPath = !Path.IsPathFullyQualified(InputFile)
                ? Path.Combine(Directory.GetCurrentDirectory(), InputFile)
                : InputFile;

            var outputPath = !Path.IsPathFullyQualified(OutputFile)
                ? Path.Combine(Directory.GetCurrentDirectory(), OutputFile)
                : OutputFile;

            if (!File.Exists(inputPath))
            {
                System.Console.WriteLine("Error: Input-File \"{0}\" not found", inputPath);
                return 1;
            }

            if (!Overwrite && File.Exists(outputPath))
            {
                System.Console.WriteLine("Error: Output-File \"{0}\" already exists", outputPath);
                return 1;
            }

            var outputDirectory = Path.GetDirectoryName(outputPath);
            if (!Directory.Exists(outputDirectory))
            {
                System.Console.WriteLine("Error: Output-Directory \"{0}\" not found", outputDirectory);
                return 1;
            }

            var tessDataPath = TessDataPath;

            if (!Path.IsPathFullyQualified(tessDataPath))
            {
                tessDataPath = Path.Combine(Directory.GetCurrentDirectory(), TessDataPath);
                if (!Directory.Exists(tessDataPath))
                    tessDataPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), TessDataPath);
            }

            using var ocr = new TesseractOpticalCharacterRecognition(
                new TesseractOptions()
                {
                    TessDataPath = tessDataPath
                });

            using var outputDocument = new PdfDocument();

            var extension = Path.GetExtension(inputPath).Trim('.').ToLower();

            if (extension == "pdf")
            {
                using var sourceDocument = PdfiumDocument.Load(inputPath);

                foreach (var pdfPage in sourceDocument.Pages)
                {
                    using var pdfPageImage = pdfPage.Render(Resolution, Resolution, false);
                    PrivatePerformOCR(ocr, pdfPageImage, outputDocument);
                }
            }
            else
            {
                using var image = Image.Load(inputPath);
                PrivatePerformOCR(ocr, image, outputDocument);
            }

            outputDocument.Save(outputPath);

            return 0;
        }

        private void PrivatePerformOCR(TesseractOpticalCharacterRecognition ocr, Image image, PdfDocument outputDocument)
        {
            ocr.InitializeFromImage(image, Language);

            var outputPage = outputDocument.AddPage();

            outputPage.Width = new XUnit(image.Width * 0.75d);
            outputPage.Height = new XUnit(image.Height * 0.75d);

            using (var outputPageGraphics = XGraphics.FromPdfPage(outputPage))
            {
                using var pdfPageXImage = XImageUtils.FromImageSharpImage(image);

                outputPageGraphics.DrawImage(pdfPageXImage, 0, 0);

                var text = ocr.DetectText();

                foreach (var paragraph in text)
                {
                    if (!string.IsNullOrEmpty(TranslateFrom) && !string.IsNullOrEmpty(TranslateTo))
                        PrivateTranslateAndWriteOCR(paragraph, image, outputPageGraphics);
                    else
                        PrivateWriteOCR(paragraph, outputPageGraphics);
                }
            }
        }

        private void PrivateWriteOCR(Paragraph paragraph, XGraphics outputPageGraphics)
        {
            foreach (var line in paragraph)
            {
                foreach (var word in line)
                {
                    if (word.Bounds.HasValue)
                    {
                        var bounds = word.Bounds.Value;

                        var xBounds = new XRect(bounds.X * 0.75d, bounds.Y * 0.75d, bounds.Width * 0.75d, bounds.Height * 0.75d);
                        var xStringFormat = new XStringFormat()
                        {
                            Alignment = XStringAlignment.Near,
                            LineAlignment = XLineAlignment.Center,
                        };

                        XFont xFont;
                        XFont xPrevFont = null;
                        XSize xPrevMeasure = default;
                        var xFontSize = 0d;

                        do
                        {
                            xFont = new XFont("Times New Roman", xFontSize);

                            var measure = outputPageGraphics.MeasureString(word, xFont, xStringFormat);

                            if (measure.Width < xBounds.Width)
                            {
                                xFontSize += OCROutputFontScalingGranularity;
                            }
                            else if (measure.Width >= xBounds.Width)
                            {
                                if (xPrevFont != null)
                                {
                                    var delta = Math.Abs(xBounds.Width - measure.Width);
                                    var prevDelta = Math.Abs(xBounds.Width - xPrevMeasure.Width);

                                    if (prevDelta < delta)
                                        xFont = xPrevFont;
                                }
                            }

                            xPrevMeasure = measure;
                            xPrevFont = xFont;
                        } while (xPrevMeasure.Width < xBounds.Width);

                        outputPageGraphics.DrawString(word, xFont, XBrushes.Transparent, xBounds, xStringFormat);
                    }
                }
            }
        }

        private void PrivateTranslateAndWriteOCR(Paragraph paragraph, Image image, XGraphics outputPageGraphics)
        {
            if (string.IsNullOrWhiteSpace(DeepLAPIKey))
                throw new ArgumentException("Invalid DeepL API key provided");

            if (!paragraph.Bounds.HasValue || paragraph.Confidence < 50)
                return;

            var paragraphText = paragraph.ToContinuousString();
            if (string.IsNullOrWhiteSpace(paragraphText))
                return;

            var translator = new Translator(DeepLAPIKey);
            var translateTask = translator.TranslateTextAsync(paragraphText, TranslateFrom, TranslateTo);
            
            translateTask.Wait();

            var bounds = paragraph.Bounds.Value;

            var xFont = new XFont("Arial", 20f);
            var xBounds = new XRect(bounds.X * 0.75d, bounds.Y * 0.75d, bounds.Width * 0.75d, bounds.Height * 0.75d);
            var xFormatter = new XTextFormatter(outputPageGraphics);

            // outputPageGraphics.DrawRectangle(XPens.Magenta, xBounds);

            outputPageGraphics.DrawRectangle(XBrushes.White, xBounds);
            xFormatter.DrawString(translateTask.Result.Text, xFont, XBrushes.Black, xBounds);
        }

    }

}
