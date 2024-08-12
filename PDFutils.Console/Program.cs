/*
 * Apache License Version 2.0, January 2004
 * http://www.apache.org/licenses/
 *
 * Copyright (c) 2024 Lukas Berger <mail@lukasberger.at>
 */
using System;
using System.Collections.Generic;
using System.Linq;

using CommandLine;

using PDFutils.Console.Verbs;
using PDFutils.Console.Verbs.API;

namespace PDFutils.Console
{

    internal static class Program
    {

        private static int Main(string[] args)
        {
            var optionTypes = PrivateBuildOptionTypeList().ToArray();
            var result = Parser.Default.ParseArguments(args, optionTypes);

#if DEBUG
            try
            {
                return MainSecondary(result);
            }
            finally
            {
                System.Console.WriteLine("Press any key to exit...");
                System.Console.ReadKey(true);
            }
#else
            return MainSecondary(result);
#endif
        }

        private static int MainSecondary(ParserResult<object> result)
        {
            if (result.Errors.Any())
                return 1;

            if (result.Value is IVerb verb)
                return verb.Run();

            return 1;
        }

        private static IEnumerable<Type> PrivateBuildOptionTypeList()
        {
            yield return typeof(RasterizeVerb);
            yield return typeof(OCRVerb);

            if (OperatingSystem.IsWindows())
                yield return typeof(PrintVerb);
        }

    }

}