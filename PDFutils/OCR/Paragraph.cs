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

    public sealed class Paragraph : IEnumerable<Line>
    {

        private readonly IEnumerable<Line> mLines;

        public Rectangle? Bounds { get; }

        public float Confidence { get; }

        public Paragraph(Line[] line, float confidence, Rectangle? bounds)
        { 
            mLines = line;

            Confidence = confidence;
            Bounds = bounds;
        }

        public string ToContinuousString()
        {
            var stringBuilder = new StringBuilder();

            var lines = (Line[])mLines;
            var lastLineIndex = lines.Length - 1;

            for (var i = 0; i < lines.Length; i++)
            {
                stringBuilder.Append(lines[i]);

                if (i < lastLineIndex)
                    stringBuilder.Append(' ');
            }

            return stringBuilder.ToString();
        }

        public override string ToString()
        {
            var stringBuilder = new StringBuilder();

            foreach (var line in mLines)
            {
                stringBuilder.Append(line.ToString());
                stringBuilder.Append('\n');
            }

            return stringBuilder.ToString();
        }

        public static implicit operator string(Paragraph paragraph)
            => paragraph.ToString();

        public IEnumerator<Line> GetEnumerator()
            => mLines.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => mLines.GetEnumerator();

    }

}
