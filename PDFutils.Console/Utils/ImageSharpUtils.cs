/*
 * Apache License Version 2.0, January 2004
 * http://www.apache.org/licenses/
 *
 * Copyright (c) 2024 Lukas Berger <mail@lukasberger.at>
 */
using System.IO;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Pbm;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Qoi;
using SixLabors.ImageSharp.Formats.Tga;
using SixLabors.ImageSharp.Formats.Tiff;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Processing;
using System;

namespace PDFutils.Console.Utils
{

    internal static class ImageSharpUtils
    {

        private const int JpegFormatMaxSize = 65500;

        public static bool TryFindEncoder(string path, out IImageEncoder encoder)
        {
            var extension = Path.GetExtension(path).Trim('.').ToLower();

            encoder = null;

            switch (extension)
            {
                case "bmp":
                case "dib":
                {
                    encoder = new BmpEncoder();
                    return true;
                }
                case "gif":
                {
                    encoder = new GifEncoder();
                    return true;
                }
                case "jfif":
                case "jpe":
                case "jpeg":
                case "jpg":
                {
                    encoder = new JpegEncoder();
                    return true;
                }
                case "pbm":
                {
                    encoder = new PbmEncoder() { ColorType = PbmColorType.Rgb };
                    return true;
                }
                case "pgm":
                {
                    encoder = new PbmEncoder() { ColorType = PbmColorType.Grayscale };
                    return true;
                }
                case "ppm":
                {
                    encoder = new PbmEncoder() { ColorType = PbmColorType.Rgb };
                    return true;
                }
                case "png":
                {
                    encoder = new PngEncoder();
                    return true;
                }
                case "qoi":
                {
                    encoder = new QoiEncoder();
                    return true;
                }
                case "tga":
                {
                    encoder = new TgaEncoder();
                    return true;
                }
                case "tif":
                case "tiff":
                {
                    encoder = new TiffEncoder();
                    return true;
                }
                case "webp":
                {
                    encoder = new WebpEncoder();
                    return true;
                }
            }

            return false;
        }

        public static void MutateImageForSaving(this Image image, IImageEncoder encoder)
        {
            if (encoder is JpegEncoder)
            {
                if (image.Width > JpegFormatMaxSize)
                    image.Mutate(PrivateJpegShrinkWidthToLimit);

                if (image.Height > JpegFormatMaxSize)
                    image.Mutate(PrivateJpegShrinkHeightToLimit);
            }
        }

        public static Image CloneImageForSaving(this Image image, IImageEncoder encoder)
        {
            if (encoder is JpegEncoder)
            {
                if (image.Width > JpegFormatMaxSize)
                    image = image.Clone(PrivateJpegShrinkWidthToLimit);

                if (image.Height > JpegFormatMaxSize)
                    image = image.Clone(PrivateJpegShrinkHeightToLimit);
            }

            return image;
        }

        public static void Save(Image image, string path, IImageEncoder encoder)
        {
            using (var outputStream = new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
                image.Save(outputStream, encoder);
        }

        private static void PrivateJpegShrinkWidthToLimit(IImageProcessingContext context)
        {
            var size = context.GetCurrentSize();
            var shrinkFactor = (double)JpegFormatMaxSize / size.Width;

            context.Resize(JpegFormatMaxSize, (int)Math.Round(size.Height * shrinkFactor));
        }

        private static void PrivateJpegShrinkHeightToLimit(IImageProcessingContext context)
        {
            var size = context.GetCurrentSize();
            var shrinkFactor = (double)JpegFormatMaxSize / size.Height;

            context.Resize((int)Math.Round(size.Width * shrinkFactor), JpegFormatMaxSize);
        }

    }

}
