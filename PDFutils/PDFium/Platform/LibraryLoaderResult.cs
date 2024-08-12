/*
 * Apache License Version 2.0, January 2004
 * http://www.apache.org/licenses/
 *
 * Copyright (c) 2023 Sandro Hanea
 * Copyright (c) 2024 Lukas Berger <mail@lukasberger.at>
 */

namespace PDFutils.PDFium.Platform
{

    public sealed class LibraryLoaderResult
    {

        public static LibraryLoaderResult Success { get; } = new(true, null);

        public bool IsSuccess { get; }

        public string ErrorMessage { get; }

        private LibraryLoaderResult(bool isSuccess, string errorMessage)
        {
            IsSuccess = isSuccess;
            ErrorMessage = errorMessage;
        }

        public static LibraryLoaderResult Failure(string errorMessage)
             => new(false, errorMessage);

    }

}
