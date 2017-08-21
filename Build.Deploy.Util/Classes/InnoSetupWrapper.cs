/*
 *  The contents of this file are subject to MIT Licence.  Please
 *  view License.txt for further details. 
 *
 *  The Original Code was created by Simon Carter (s1cart3r@gmail.com)
 *
 *  Copyright (c) 2017 Simon Carter.  All Rights Reserved.
 *
 *  Purpose:  Inno Setup wrapper
 *
 */
using System;
using System.Diagnostics;
using System.IO;

using Shared;
using Shared.Classes;

namespace Build.Deploy.Util
{
    /// <summary>
    /// Inno Setup Wrapper
    /// </summary>
    internal static class InnoSetupWrapper
    {
        #region Inno Setup Parameters

        private static string innoExe;
        private static string innoSetupFile;
        private static string innoCompiledSetup;

        #endregion Inno Setup Parameters

        #region Internal Static Methods

        internal static bool Initialise()
        {
            innoExe = Parameters.GetOption("innoExe", String.Empty);
            innoSetupFile = Parameters.GetOption("innoScript", String.Empty);
            innoCompiledSetup = Parameters.GetOption("innoSetup", String.Empty);

            return (File.Exists(innoExe) && 
                File.Exists(innoSetupFile) &&
                !String.IsNullOrEmpty(innoCompiledSetup));
        }

        internal static void Execute(FileVersionInfo version)
        {
            BuildSetupFile(version);
        }

        /// <summary>
        /// Display parameters specific to Nuget options
        /// </summary>
        internal static void ShowParameters()
        {
            Console.WriteLine("Inno Setup Options:");
            Console.WriteLine("  /innoExe         Path/filename to inno compil32.exe.");
            Console.WriteLine("  /innoScript      Path/filename to the iss file.");
            Console.WriteLine("  /innoSetup       Path/filename to the compiled setup file, this");
            Console.WriteLine("                   can should include {0} in the filename which will");
            Console.WriteLine("                   be replaced with the version, e.g.");
            Console.WriteLine("                   c:\\setup\\MyProductSetupFile_{0}.exe");
            Console.WriteLine(String.Empty);
        }

        #endregion Internal Static Methods

        #region Private Methods

        private static void BuildSetupFile(FileVersionInfo version)
        {
            string installFile = Utilities.FileRead(innoSetupFile, true);

            int verPos = installFile.IndexOf("#define MyAppVersion \"");

            Console.WriteLine("Setting Inno Version in Config");

            if (verPos < 0)
            {
                Console.Write("Invalid Inno Script, #define MyAppVersion not found!");
                return;
            }

            string newVersion = String.Format("{0}.{1}.{2}",
                version.FileMajorPart,
                version.FileMinorPart,
                version.FileBuildPart);

            // replace the version in the setup script
            int eolPos = installFile.IndexOf("\r\n", verPos + 1);

            string start = installFile.Substring(0, verPos + 22);
            string end = installFile.Substring(eolPos - 1);
            string newInstallFile = String.Format("{0}{1}{2}", start, newVersion, end);

            // save the script
            Shared.Utilities.FileWrite(innoSetupFile, newInstallFile);

            //
            string innoCompiledSetupFile = String.Format(innoCompiledSetup, newVersion);

            if (System.IO.File.Exists(innoCompiledSetupFile))
            {
                System.IO.File.Delete(innoCompiledSetupFile);
            }

            Console.WriteLine("New Inno Setup file is {0}", innoCompiledSetupFile);

            // compile install file
            ProcessStartInfo startInfo = new ProcessStartInfo(innoExe,
                String.Format("/cc \"{0}\"", innoSetupFile));
            Process proc = System.Diagnostics.Process.Start(startInfo);

            Console.WriteLine("Building Inno Setup File");

            proc.WaitForExit();

            // is the install file valid
            if (!System.IO.File.Exists(innoCompiledSetupFile))
            {
                Console.WriteLine("Failed to build Inno setup file");
            }
            else
            {
                Console.WriteLine("Successfully built Inno setup file");
            }
        }
            
        #endregion Private Methods
    }
}
