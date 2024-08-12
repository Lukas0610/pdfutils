/*
 * Apache License Version 2.0, January 2004
 * http://www.apache.org/licenses/
 *
 * Copyright (c) 2023 Sandro Hanea
 * Copyright (c) 2024 Lukas Berger <mail@lukasberger.at>
 */
using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

using PDFutils.PDFium.Platform;
using PDFutils.PDFium.Platform.Linux;
using PDFutils.PDFium.Platform.MacOS;
using PDFutils.PDFium.Platform.Windows;

namespace PDFutils.PDFium
{

    public sealed class PdfiumLibrary : IDisposable
    {

        private static readonly object _syncRoot = new();
        private static PdfiumLibrary _library;

        public static void EnsureLoaded()
        {
            lock (_syncRoot)
            {
                if (_library == null)
                    _library = new PdfiumLibrary();
            }
        }

        private bool _disposed;

        private PdfiumLibrary()
        {
            if (RuntimeInformation.OSArchitecture.ToString() == "Wasm")
                return;

            var architecture =
                RuntimeInformation.OSArchitecture switch
                {
                    Architecture.X64 => "x64",
                    Architecture.X86 => "x86",
                    Architecture.Arm => "arm",
                    Architecture.Arm64 => "arm64",
                    _ => throw new PlatformNotSupportedException($"Unsupported OS platform, architecture: {RuntimeInformation.OSArchitecture}")
                };

            var (platform, fileName) =
                Environment.OSVersion.Platform switch
                {
                    _ when RuntimeInformation.IsOSPlatform(OSPlatform.Windows) => ("win", "pdfium.dll"),
                    _ when RuntimeInformation.IsOSPlatform(OSPlatform.Linux) => ("linux", "libpdfium.so"),
                    _ when RuntimeInformation.IsOSPlatform(OSPlatform.OSX) => ("osx", "libpdfium.dylib"),
                    _ => throw new PlatformNotSupportedException($"Unsupported OS platform, architecture: {RuntimeInformation.OSArchitecture}")
                };

            var assemblySearchPaths = new[]
            {
                AppDomain.CurrentDomain.RelativeSearchPath,
                Path.GetDirectoryName(typeof(NativeMethods).Assembly.Location),
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                Path.GetDirectoryName(Environment.GetCommandLineArgs()[0])
            };

            string path = null;

            foreach (var assemblySearchPath in assemblySearchPaths)
            {
                if (string.IsNullOrEmpty(assemblySearchPath))
                    continue;

                path = Path.Combine(assemblySearchPath, "runtimes", $"{platform}-{architecture}", "native", fileName);
                if (File.Exists(path))
                    break;

                path = Path.Combine(assemblySearchPath, fileName);
                if (File.Exists(path))
                    break;
            }

            if (string.IsNullOrWhiteSpace(path))
                throw new FileNotFoundException("PDFium native library not found");
            else if (!File.Exists(path))
                throw new FileNotFoundException("PDFium native library not found", path);

            ILibraryLoader libraryLoader =
                platform switch
                {
                    "win" => new WindowsLibraryLoader(),
                    "osx" => new MacOSLibraryLoader(),
                    "linux" => new LinuxLibraryLoader(),
                    _ => throw new PlatformNotSupportedException($"Currently {platform} platform is not supported")
                };

            libraryLoader.OpenLibrary(path);
            NativeMethods.FPDF_InitLibrary();
        }

        ~PdfiumLibrary()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            NativeMethods.FPDF_DestroyLibrary();
            _disposed = true;
        }

    }

}
