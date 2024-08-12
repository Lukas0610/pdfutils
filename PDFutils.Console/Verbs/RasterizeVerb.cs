/*
 * Apache License Version 2.0, January 2004
 * http://www.apache.org/licenses/
 *
 * Copyright (c) 2024 Lukas Berger <mail@lukasberger.at>
 */
using System.IO;

using CommandLine;

using PDFutils.Console.Utils;
using PDFutils.Console.Verbs.API;

namespace PDFutils.Console.Verbs
{

    [Verb("rasterize", HelpText = "Convert PDF to raster-image")]
    internal sealed class RasterizeVerb : IVerb
    {

        [Option('r', "resolution", HelpText = "Resolution of the generated image", Default = 96f)]
        public float Resolution { get; set; }

        [Option('f', "overwrite", HelpText = "Force overwriting existing output-files", Default = false)]
        public bool Overwrite { get; set; }

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

            using var pdfDocument = PdfiumDocument.Load(inputPath);
            using var pdfImage = pdfDocument.RenderAllPages(Resolution, Resolution, false);

            if (!ImageSharpUtils.TryFindEncoder(outputPath, out var imageEncoder))
            {
                System.Console.WriteLine("Error: No encoder found for \"{0}\"", Path.GetExtension(outputPath));
                return 1;
            }

            ImageSharpUtils.MutateImageForSaving(pdfImage, imageEncoder);
            ImageSharpUtils.Save(pdfImage, outputPath, imageEncoder);

            return 0;
        }

    }

}
