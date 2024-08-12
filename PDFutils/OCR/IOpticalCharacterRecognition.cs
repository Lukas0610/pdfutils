/*
 * Apache License Version 2.0, January 2004
 * http://www.apache.org/licenses/
 *
 * Copyright (c) 2024 Lukas Berger <mail@lukasberger.at>
 */
using System;
using System.IO;

using SixLabors.ImageSharp;

namespace PDFutils.OCR
{

    public interface IOpticalCharacterRecognition : IDisposable
    {

        void InitializeFromImage(Image image, string language);

        void InitializeFromStream(Stream stream, string language);

        void InitializeFromBuffer(byte[] buffer, string language);

        string GetTextAsString();

        Text DetectText();

    }

}
