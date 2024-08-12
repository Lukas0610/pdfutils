/*
 * Apache License Version 2.0, January 2004
 * http://www.apache.org/licenses/
 *
 * Copyright (c) 2024 Lukas Berger <mail@lukasberger.at>
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Tiff;

using Tesseract;

namespace PDFutils.OCR.Tesseract
{

    public sealed class TesseractOpticalCharacterRecognition : IOpticalCharacterRecognition
    {

        private readonly TesseractOptions mOptions;

        private TesseractEngine mEngine;
        private Pix mPix;
        private Page mPage;
        private ResultIterator mIterator;

        private bool mDisposed;

        public Page Page
            => mPage;

        public TesseractOpticalCharacterRecognition(TesseractOptions options)
        {
            mOptions = options;
        }

        public void InitializeFromImage(Image image, string language)
        {
            if (mDisposed)
                throw new ObjectDisposedException(nameof(TesseractOpticalCharacterRecognition));

            using var memoryStream = new MemoryStream();
            image.Save(memoryStream, new TiffEncoder());

            Dispose(false);
            PrivateInitializeFromPix(Pix.LoadTiffFromMemory(memoryStream.ToArray()), language);
        }

        public void InitializeFromStream(Stream stream, string language)
        {
            if (mDisposed)
                throw new ObjectDisposedException(nameof(TesseractOpticalCharacterRecognition));

            using var memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream, 16384);

            Dispose(false);
            PrivateInitializeFromPix(Pix.LoadFromMemory(memoryStream.ToArray()), language);
        }

        public void InitializeFromBuffer(byte[] buffer, string language)
        {
            if (mDisposed)
                throw new ObjectDisposedException(nameof(TesseractOpticalCharacterRecognition));

            Dispose(false);
            PrivateInitializeFromPix(Pix.LoadFromMemory(buffer), language);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public string GetTextAsString()
            => mPage.GetText();

        public Text DetectText()
        {
            if (mDisposed)
                throw new ObjectDisposedException(nameof(TesseractOpticalCharacterRecognition));

            var paragraphs = new List<Paragraph>();
            var lines = new List<Line>();
            var words = new List<Word>();

            mIterator.Begin();

            do
            {
                do
                {
                    do
                    {
                        do
                        {
                            var wordText = mIterator.GetText(PageIteratorLevel.Word);

                            Rectangle? wordBounds = mIterator.TryGetBoundingBox(PageIteratorLevel.Word, out var tessWordBounds)
                                ? new Rectangle(tessWordBounds.X1, tessWordBounds.Y1, tessWordBounds.Width, tessWordBounds.Height)
                                : null;

                            var wordConfidence = mIterator.GetConfidence(PageIteratorLevel.TextLine);

                            words.Add(new Word(wordText, wordConfidence, wordBounds));

                            if (mIterator.IsAtFinalOf(PageIteratorLevel.TextLine, PageIteratorLevel.Word))
                            {
                                Rectangle? lineBounds = mIterator.TryGetBoundingBox(PageIteratorLevel.TextLine, out var tessLineBounds)
                                    ? new Rectangle(tessLineBounds.X1, tessLineBounds.Y1, tessLineBounds.Width, tessLineBounds.Height)
                                    : null;

                                var lineConfidence = mIterator.GetConfidence(PageIteratorLevel.TextLine);

                                lines.Add(new Line(words.ToArray(), lineConfidence, lineBounds));
                                words.Clear();
                            }

                        } while (mIterator.Next(PageIteratorLevel.TextLine, PageIteratorLevel.Word));

                        if (mIterator.IsAtFinalOf(PageIteratorLevel.Para, PageIteratorLevel.TextLine))
                        {
                            Rectangle? paragraphBounds = mIterator.TryGetBoundingBox(PageIteratorLevel.Para, out var tessParagraphBounds)
                                ? new Rectangle(tessParagraphBounds.X1, tessParagraphBounds.Y1, tessParagraphBounds.Width, tessParagraphBounds.Height)
                                : null;

                            var paragraphConfidence = mIterator.GetConfidence(PageIteratorLevel.Para);

                            paragraphs.Add(new Paragraph(lines.ToArray(), paragraphConfidence, paragraphBounds));
                            lines.Clear();
                        }
                    }
                    while (mIterator.Next(PageIteratorLevel.Para, PageIteratorLevel.TextLine));
                }
                while (mIterator.Next(PageIteratorLevel.Block, PageIteratorLevel.Para));
            }
            while (mIterator.Next(PageIteratorLevel.Block));

            return new Text(paragraphs.ToArray());
        }

        private void PrivateInitializeFromPix(Pix pix, string language)
        {
            mEngine = new TesseractEngine(mOptions.TessDataPath, language, EngineMode.Default);
            mPix = pix;
            mPage = mEngine.Process(pix);
            mIterator = mPage.GetIterator();
        }

        private void Dispose(bool disposing)
        {
            if (mDisposed)
                return;

            mIterator?.Dispose();
            mIterator = null;

            mPage?.Dispose();
            mPage = null;

            mPix?.Dispose();
            mPix = null;

            mEngine?.Dispose();
            mEngine = null;

            if (disposing)
                mDisposed = true;
        }

    }

}
