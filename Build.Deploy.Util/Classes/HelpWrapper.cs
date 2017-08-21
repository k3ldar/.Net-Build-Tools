/*
 *  The contents of this file are subject to MIT Licence.  Please
 *  view License.txt for further details. 
 *
 *  The Original Code was created by Simon Carter (s1cart3r@gmail.com)
 *
 *  Copyright (c) 2017 Simon Carter.  All Rights Reserved.
 *
 *  Purpose:  Help Wrapper
 *
 */
using System;
using Shared.Classes;

namespace Build.Deploy.Util
{
    internal static class HelpWrapper
    {
        /// <summary>
        /// Determines if we are showing help information
        /// </summary>
        /// <returns>true if help is shown, otherwise false</returns>
        internal static bool Initialise()
        {
            Console.WriteLine("Build.Deploy.Util.exe");
            Console.WriteLine("Copyright 2017 (C) Simon Carter.  All Rights Reserved.");
            Console.WriteLine("Optionally updates Git repository with new build and optionally");
            Console.WriteLine("builds a nuget package and deploys it.");
            Console.WriteLine(String.Empty);

            if (Parameters.OptionExists("?"))
            {
                Console.WriteLine("Mandatory Parameters:");
                Console.WriteLine("  /Debug           Indicates it's a debug build see Macro below.");
                Console.WriteLine("  /Release         Indicates it's a release build see Macro below.");
                Console.WriteLine("                   If neither Debug or Release are set, defaults to Debug");
                Console.WriteLine("  /Target          Path/File of main project file see Macro below.");
                Console.WriteLine(String.Empty);
                Console.WriteLine("Optional Parameters:");
                Console.WriteLine("  /?               Help (shows this).");
                Console.WriteLine("  /IgnoreDebug     If the build is Debug and this param is set");
                Console.WriteLine("                   then nothing is done and the program exits");
                Console.WriteLine("                   this allows same build events for debug and release");
                Console.WriteLine("                   version without performing any action when building");
                Console.WriteLine("                   debug versions during development.");
                Console.WriteLine(String.Empty);

                GitWrapper.ShowParameters();
                NugetWrapper.ShowParameters();
                InnoSetupWrapper.ShowParameters();

#if ALLOW_FTP
                Console.WriteLine("  /");
                Console.WriteLine("  /ftpUser");
                Console.WriteLine("  /ftpPassword");
                Console.WriteLine("  /ftpServer");
                Console.WriteLine("  /ftpPort");
                Console.WriteLine(String.Empty);
#endif

                Console.WriteLine("Visual Studio Macro's:");
                Console.WriteLine("                   VS $(ConfigurationName) macro can be used to ");
                Console.WriteLine("                   send the build type in the build event command line");
                Console.WriteLine(String.Empty);
                Console.WriteLine("                   VS $(TargetPath) macro can be used to send the");
                Console.WriteLine("                   target file (.exe/.dll)");
                Console.WriteLine(String.Empty);
                return (true);
            }

            return (false);
        }
    }
}
