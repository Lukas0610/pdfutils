/*
 * Apache License Version 2.0, January 2004
 * http://www.apache.org/licenses/
 *
 * Copyright (c) 2023 Sandro Hanea
 * Copyright (c) 2024 Lukas Berger <mail@lukasberger.at>
 */

namespace PDFutils.PDFium
{

    public enum PdfError
    {
        Success = (int)NativeMethods.FPDF_ERR.FPDF_ERR_SUCCESS,
        Unknown = (int)NativeMethods.FPDF_ERR.FPDF_ERR_UNKNOWN,
        CannotOpenFile = (int)NativeMethods.FPDF_ERR.FPDF_ERR_FILE,
        InvalidFormat = (int)NativeMethods.FPDF_ERR.FPDF_ERR_FORMAT,
        PasswordProtected = (int)NativeMethods.FPDF_ERR.FPDF_ERR_PASSWORD,
        UnsupportedSecurityScheme = (int)NativeMethods.FPDF_ERR.FPDF_ERR_SECURITY,
        PageNotFound = (int)NativeMethods.FPDF_ERR.FPDF_ERR_PAGE
    }

}
