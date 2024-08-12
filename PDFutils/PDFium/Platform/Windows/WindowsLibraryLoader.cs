/*
 * Apache License Version 2.0, January 2004
 * http://www.apache.org/licenses/
 *
 * Copyright (c) 2023 Sandro Hanea
 * Copyright (c) 2024 Lukas Berger <mail@lukasberger.at>
 */
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace PDFutils.PDFium.Platform.Windows
{

    internal class WindowsLibraryLoader : ILibraryLoader
    {

        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern nint LoadLibrary([MarshalAs(UnmanagedType.LPTStr)] string lpFileName);

        public LibraryLoaderResult OpenLibrary(string fileName)
        {
            var loadedLib = LoadLibrary(fileName);

            if (loadedLib == 0)
            {
                var errorCode = Marshal.GetLastWin32Error();
                var errorMessage = new Win32Exception(errorCode).Message;
                return LibraryLoaderResult.Failure(errorMessage);
            }

            return LibraryLoaderResult.Success;
        }

    }

}
