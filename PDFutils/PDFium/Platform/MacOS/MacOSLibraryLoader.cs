/*
 * Apache License Version 2.0, January 2004
 * http://www.apache.org/licenses/
 *
 * Copyright (c) 2023 Sandro Hanea
 * Copyright (c) 2024 Lukas Berger <mail@lukasberger.at>
 */
using System.Runtime.InteropServices;

namespace PDFutils.PDFium.Platform.MacOS
{

    internal class MacOSLibraryLoader : ILibraryLoader
    {

        [DllImport("libdl.dylib", ExactSpelling = true, CharSet = CharSet.Auto, EntryPoint = "dlopen")]
        public static extern nint NativeOpenLibraryLibdl(string filename, int flags);

        [DllImport("libdl.dylib", ExactSpelling = true, CharSet = CharSet.Auto, EntryPoint = "dlerror")]
        public static extern nint GetLoadError();

        public LibraryLoaderResult OpenLibrary(string fileName)
        {
            var loadedLib = NativeOpenLibraryLibdl(fileName, 0x00001);

            if (loadedLib == 0)
            {
                var errorMessage = Marshal.PtrToStringAnsi(GetLoadError()) ?? "Unknown error";
                return LibraryLoaderResult.Failure(errorMessage);
            }

            return LibraryLoaderResult.Success;
        }

    }

}
