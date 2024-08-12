/*
 * Apache License Version 2.0, January 2004
 * http://www.apache.org/licenses/
 *
 * Copyright (c) 2023 Sandro Hanea
 * Copyright (c) 2024 Lukas Berger <mail@lukasberger.at>
 */
using System;
using System.Runtime.InteropServices;

namespace PDFutils.PDFium
{

    internal static partial class NativeMethods
    {

        public const int GM_ADVANCED = 2;
        public const uint MWT_LEFTMULTIPLY = 2;

        [DllImport("gdi32.dll")]
        public static extern int SetGraphicsMode(IntPtr hdc, int iMode);

        [DllImport("gdi32.dll")]
        public static extern bool ModifyWorldTransform(IntPtr hdc, [In] ref XFORM lpXform, uint iMode);

        [DllImport("gdi32.dll")]
        public static extern bool SetViewportOrgEx(IntPtr hdc, int X, int Y, out POINT lpPoint);

        [StructLayout(LayoutKind.Sequential)]
        public struct XFORM
        {
            public float eM11;
            public float eM12;
            public float eM21;
            public float eM22;
            public float eDx;
            public float eDy;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

    }

}
