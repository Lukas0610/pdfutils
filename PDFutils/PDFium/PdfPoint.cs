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

    public sealed class PdfPoint : IEquatable<PdfPoint>
    {

        public int Page { get; }

        public PointF Location { get; }

        public PdfPoint(int page, PointF location)
        {
            Page = page;
            Location = location;
        }

        public bool Equals(PdfPoint other)
            => Page == other.Page && Location == other.Location;

        public override bool Equals(object obj)
            => obj is PdfPoint point && Equals(point);

        public override int GetHashCode()
        {
            unchecked
            {
                return (Page * 397) ^ Location.GetHashCode();
            }
        }

        public static bool operator ==(PdfPoint left, PdfPoint right)
            => left.Equals(right);

        public static bool operator !=(PdfPoint left, PdfPoint right)
            => !left.Equals(right);

    }

}
