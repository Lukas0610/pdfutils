/*
 * Apache License Version 2.0, January 2004
 * http://www.apache.org/licenses/
 *
 * Copyright (c) 2024 Lukas Berger <mail@lukasberger.at>
 */
using System.IO;

using PdfSharp.Drawing;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Bmp;

namespace PDFutils.PDFsharp
{

    public static class XImageUtils
    {

        public static XImage FromImageSharpImage(Image image)
        {
            using var memoryStream = new MemoryStream();

            image.Save(memoryStream, new BmpEncoder());
            memoryStream.Seek(0, SeekOrigin.Begin);

            return XImage.FromStream(memoryStream);
        }

    }

}
