/*
 * Apache License Version 2.0, January 2004
 * http://www.apache.org/licenses/
 *
 * Copyright (c) 2024 Lukas Berger <mail@lukasberger.at>
 */
using System;

using SixLabors.ImageSharp;

namespace PDFutils.OCR
{

    public sealed class Word
    {

        private const int WordBaseHashCode = 0x7f5ac9b6;

        public string Text { get; }

        public Rectangle? Bounds { get; }

        public float Confidence { get; }

        public Word(string text, float confidence, Rectangle? bounds)
        {
            Text = text;

            Confidence = confidence;
            Bounds = bounds;
        }

        public static implicit operator string(Word word)
            => word.Text;

        public override string ToString()
            => Text;

        public override bool Equals(object obj)
            => Text.Equals(obj);

        public override int GetHashCode()
            => HashCode.Combine(WordBaseHashCode, Text);

    }

}
