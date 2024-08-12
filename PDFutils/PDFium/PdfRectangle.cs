/*
 * Apache License Version 2.0, January 2004
 * http://www.apache.org/licenses/
 *
 * Copyright (c) 2023 Sandro Hanea
 * Copyright (c) 2024 Lukas Berger <mail@lukasberger.at>
 */
using System;
using System.Drawing;

namespace PDFutils.PDFium
{

    public sealed class PdfRectangle : IEquatable<PdfRectangle>
    {

        public int Page { get; }

        public RectangleF Bounds { get; }

        private PdfRectangle()
        {

        }

        public PdfRectangle(int page, RectangleF bounds)
        {
            Page = page;
            Bounds = bounds;
        }

        public bool Equals(PdfRectangle other)
            => Page == other.Page && Bounds == other.Bounds;

        public override bool Equals(object obj)
            => obj is PdfRectangle rectangle && Equals(rectangle);

        public override int GetHashCode()
        {
            unchecked
            {
                return (Page * 397) ^ Bounds.GetHashCode();
            }
        }

        public static bool operator ==(PdfRectangle left, PdfRectangle right)
            => left.Equals(right);

        public static bool operator !=(PdfRectangle left, PdfRectangle right)
            => !left.Equals(right);

    }

}
