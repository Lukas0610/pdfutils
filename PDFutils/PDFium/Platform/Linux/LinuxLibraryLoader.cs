/*
 * Apache License Version 2.0, January 2004
 * http://www.apache.org/licenses/
 *
 * Copyright (c) 2023 Sandro Hanea
 * Copyright (c) 2024 Lukas Berger <mail@lukasberger.at>
 */
using System;
using System.Runtime.InteropServices;

namespace PDFutils.PDFium.Platform.Linux
{

    internal class LinuxLibraryLoader : ILibraryLoader
    {


        [DllImport("libdl.so", ExactSpelling = true, CharSet = CharSet.Auto, EntryPoint = "dlopen")]
        public static extern nint NativeOpenLibraryLibdl(string filename, int flags);

        [DllImport("libdl.so.2", ExactSpelling = true, CharSet = CharSet.Auto, EntryPoint = "dlopen")]
        public static extern nint NativeOpenLibraryLibdl2(string filename, int flags);

        [DllImport("libdl.so", ExactSpelling = true, CharSet = CharSet.Auto, EntryPoint = "dlerror")]
        public static extern nint GetLoadError();

        [DllImport("libdl.so.2", ExactSpelling = true, CharSet = CharSet.Auto, EntryPoint = "dlerror")]
        public static extern nint GetLoadError2();

        public LibraryLoaderResult OpenLibrary(string fileName)
        {
            nint loadedLib;
            try
            {
                // open with rtls lazy flag
                loadedLib = NativeOpenLibraryLibdl2(fileName, 0x00001);
            }
            catch (DllNotFoundException)
            {
                loadedLib = NativeOpenLibraryLibdl(fileName, 0x00001);
            }

            if (loadedLib == 0)
            {
                string errorMessage;
                try
                {
                    errorMessage = Marshal.PtrToStringAnsi(GetLoadError2()) ?? "Unknown error";
                }
                catch (DllNotFoundException)
                {
                    errorMessage = Marshal.PtrToStringAnsi(GetLoadError()) ?? "Unknown error";
                }

                return LibraryLoaderResult.Failure(errorMessage);
            }

            return LibraryLoaderResult.Success;
        }

    }

}
