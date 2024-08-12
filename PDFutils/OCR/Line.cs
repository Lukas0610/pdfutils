/*
 * Apache License Version 2.0, January 2004
 * http://www.apache.org/licenses/
 *
 * Copyright (c) 2024 Lukas Berger <mail@lukasberger.at>
 */
using System.Collections;
using System.Collections.Generic;
using System.Text;

using SixLabors.ImageSharp;

namespace PDFutils.OCR
{

    public sealed class Line : IEnumerable<Word>
    {

        private readonly IEnumerable<Word> mWords;

        public Rectangle? Bounds { get; }

        public float Confidence { get; }

        public Line(Word[] words, float confidence, Rectangle? bounds)
        {
            mWords = words;

            Confidence = confidence;
            Bounds = bounds;
        }

        public override string ToString()
        {
            var stringBuilder = new StringBuilder();

            var words = (Word[])mWords;
            var lastWordIndex = words.Length - 1;

            for (var i = 0; i < words.Length; i++)
            {
                stringBuilder.Append(words[i]);

                if (i < lastWordIndex)
                    stringBuilder.Append(' ');
            }

            return stringBuilder.ToString();
        }

        public static implicit operator string(Line line)
            => line.ToString();

        public IEnumerator<Word> GetEnumerator()
            => mWords.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => mWords.GetEnumerator();

    }

}
