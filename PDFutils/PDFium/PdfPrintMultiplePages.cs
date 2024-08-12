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

    /// <summary>
    /// Configuration for printing multiple PDF pages on a single page.
    /// </summary>
    public sealed class PdfPrintMultiplePages
    {

        /// <summary>
        /// Gets the number of pages to print horizontally.
        /// </summary>
        public int Horizontal { get; }

        /// <summary>
        /// Gets the number of pages to print vertically.
        /// </summary>
        public int Vertical { get; }

        /// <summary>
        /// Gets the orientation in which PDF pages are layed out on the
        /// physical page.
        /// </summary>
        public PdfOrientation Orientation { get; }

        /// <summary>
        /// Gets the margin between PDF pages in device units.
        /// </summary>
        public float Margin { get; }

        /// <summary>
        /// Creates a new instance of the PdfPrintMultiplePages class.
        /// </summary>
        /// <param name="horizontal">The number of pages to print horizontally.</param>
        /// <param name="vertical">The number of pages to print vertically.</param>
        /// <param name="orientation">The orientation in which PDF pages are layed out on
        /// the physical page.</param>
        /// <param name="margin">The margin between PDF pages in device units.</param>
        public PdfPrintMultiplePages(int horizontal, int vertical, PdfOrientation orientation, float margin)
        {
            if (horizontal < 1)
                throw new ArgumentOutOfRangeException("horizontal cannot be less than one");
            if (vertical < 1)
                throw new ArgumentOutOfRangeException("vertical cannot be less than one");
            if (margin < 0)
                throw new ArgumentOutOfRangeException("margin cannot be less than zero");

            Horizontal = horizontal;
            Vertical = vertical;
            Orientation = orientation;
            Margin = margin;
        }

    }

}
