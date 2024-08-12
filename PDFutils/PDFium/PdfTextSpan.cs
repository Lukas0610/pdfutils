/*
 * Apache License Version 2.0, January 2004
 * http://www.apache.org/licenses/
 *
 * Copyright (c) 2023 Sandro Hanea
 * Copyright (c) 2024 Lukas Berger <mail@lukasberger.at>
 */
using System;

namespace PDFutils.PDFium
{

    public sealed class PdfTextSpan : IEquatable<PdfTextSpan>
    {

        public int Page { get; }

        public int Offset { get; }

        public int Length { get; }

        public PdfTextSpan(int page, int offset, int length)
        {
            Page = page;
            Offset = offset;
            Length = length;
        }

        public bool Equals(PdfTextSpan other)
            => Page == other.Page && Offset == other.Offset && Length == other.Length;

        public override bool Equals(object obj)
            => obj is PdfTextSpan span && Equals(span);

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Page;
                hashCode = (hashCode * 397) ^ Offset;
                hashCode = (hashCode * 397) ^ Length;
                return hashCode;
            }
        }

        public static bool operator ==(PdfTextSpan left, PdfTextSpan right)
            => left.Equals(right);

        public static bool operator !=(PdfTextSpan left, PdfTextSpan right)
            => !left.Equals(right);

    }

}
