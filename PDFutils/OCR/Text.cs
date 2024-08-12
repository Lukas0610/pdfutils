/*
 * Apache License Version 2.0, January 2004
 * http://www.apache.org/licenses/
 *
 * Copyright (c) 2024 Lukas Berger <mail@lukasberger.at>
 */
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace PDFutils.OCR
{

    public sealed class Text : IEnumerable<Paragraph>
    {

        private readonly IEnumerable<Paragraph> mParagraphs;

        public Text(Paragraph[] paragraphs)
            => mParagraphs = paragraphs;

        public override string ToString()
        {
            var stringBuilder = new StringBuilder();

            var paragraph = (Paragraph[])mParagraphs;
            var lastParagraphIndex = paragraph.Length - 1;

            for (var i = 0; i < paragraph.Length; i++)
            {
                stringBuilder.Append(paragraph[i]);

                if (i < lastParagraphIndex)
                    stringBuilder.Append('\n');
            }

            return stringBuilder.ToString();
        }

        public static implicit operator string(Text text)
            => text.ToString();

        public IEnumerator<Paragraph> GetEnumerator()
            => mParagraphs.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => mParagraphs.GetEnumerator();

    }

}
