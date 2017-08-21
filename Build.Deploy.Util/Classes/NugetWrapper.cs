/*
 *  The contents of this file are subject to MIT Licence.  Please
 *  view License.txt for further details. 
 *
 *  The Original Code was created by Simon Carter (s1cart3r@gmail.com)
 *
 *  Copyright (c) 2017 Simon Carter.  All Rights Reserved.
 *
 *  Purpose:  Nuget Pack/Push Wrapper
 *
 */
using System;
using System.Diagnostics;
using System.IO;

using Shared;
using Shared.Classes;

namespace Build.Deploy.Util
{
    internal static class NugetWrapper
    {
        #region Nuget Parameters

        private static string netVersion;

        private static string nugetExe;
        private static string nugetFile;
        private static bool nugetVersion;
        private static string nugetVersionString;
        private static string nugetPackage;
        private static bool nugetOverwrite;

        private static bool nugetPush;
        private static string nugetKey;
        private static string nugetPushSource;

        #endregion Nuget Parameters

        #region Internal Static Methods

        internal static bool Initialise(FileVersionInfo version)
        {
            // pack
            netVersion = Parameters.GetOption("netVersion", String.Empty);

            if (String.IsNullOrEmpty(netVersion))
            {
                Console.WriteLine("netVersion parameter is missing");
                return (false);
            }

            nugetFile = Parameters.GetOption("NugetPack", String.Empty);
            nugetVersion = Parameters.OptionExists("NugetVer");
            nugetExe = Parameters.GetOption("NugetExe", String.Empty);
            nugetOverwrite = Parameters.OptionExists("NugetOverwrite");
            int verString = Parameters.GetOption("NugetVer", 3);

            switch (verString)
            {
                case 1:
                    nugetVersionString = String.Format("{0}.0", version.ProductMajorPart);
                    break;
                case 2:
                    nugetVersionString = String.Format("{0}.{1}",
                        version.ProductMajorPart, version.ProductMinorPart);
                    break;
                case 4:
                    nugetVersionString = version.ProductVersion;
                    break;
                default:
                    nugetVersionString = String.Format("{0}.{1}.{2}",
                        version.ProductMajorPart,
                        version.ProductMinorPart,
                        version.ProductBuildPart);
                    break;
            }

            // push
            nugetPush = Parameters.OptionExists("nugetPush");

            if (nugetPush)
            {
                string nugetKeyFile = Parameters.GetOption("nugetKeyFile", String.Empty);

                if (File.Exists(nugetKeyFile))
                {
                    nugetKey = Utilities.FileRead(nugetKeyFile, false);
                    nugetPushSource = Parameters.GetOption("nugetSource",
                        "https://www.nuget.org/api/v2/package");
                    nugetPush = !String.IsNullOrEmpty(nugetKey);
                }
                else
                {
                    nugetPush = false;
                }
            }

            return (File.Exists(nugetExe) && File.Exists(nugetFile));
        }

        internal static void Execute(string targetFile)
        {
            if (NugetWrapper.CreateNugetPackage(targetFile))
                NugetWrapper.PushNugetPackage();
        }

        /// <summary>
        /// Display parameters specific to Nuget options
        /// </summary>
        internal static void ShowParameters()
        {
            Console.WriteLine("Nuget Pack Options:");
            Console.WriteLine("  /netVersion      Target .Net version (4 = net4, 4.5.2=net452 etc).");
            Console.WriteLine("  /NugetExe        Path/filename to the nuget.exe file.");
            Console.WriteLine("  /NugetPack       Path/filename to the .nuspec file.");
            Console.WriteLine("  /NugetVer        Indicates the current version should be used.  Optionally");
            Console.WriteLine("                   send number of version parts 1 to 4 ");
            Console.WriteLine("                   (e.g. 3 = Major.Minor.Buid).");
            Console.WriteLine("  /NugetOverwrite  Allow overwrite of existing package, if it exists.");
            Console.WriteLine(String.Empty);
            Console.WriteLine("Nuget Push Options:");
            Console.WriteLine("  /nugetPush       If set nuget package will be uploaded to a server (Nuget");
            Console.WriteLine("                   Pack parameters must also be set).");
            Console.WriteLine("  /nugetKeyFile    Path/file name to file with nuget private key.");
            Console.WriteLine("  /nugetSource     Remote http location, defaults to nuget.org ");
            Console.WriteLine("                   (https://www.nuget.org/api/v2/package).");
            Console.WriteLine(String.Empty);
        }

        #endregion Internal Static Methods

        #region Private Static Methods

        /// <summary>
        /// Creates a Nuget Package
        /// </summary>
        /// <param name="targetFile">target .exe/.dll</param>
        internal static bool CreateNugetPackage(string targetFile)
        {
            Console.WriteLine("Creating Nuget Package");

            string targetPath = Path.GetDirectoryName(targetFile);
            string targetPathTemp = Utilities.AddTrailingBackSlash(targetPath) + "temp\\";
            string targetPathLib = String.Format("{0}lib\\{1}\\", targetPathTemp, netVersion);

            if (Directory.Exists(targetPathTemp))
            {
                Directory.Delete(targetPathTemp, true);
            }

            Directory.CreateDirectory(targetPathLib);

            string tempFile = targetPathLib + Path.GetFileName(targetFile);
            File.Copy(targetFile, tempFile);

            targetFile = Path.ChangeExtension(targetFile, ".xml");
            tempFile = Path.ChangeExtension(tempFile, ".xml");

            if (File.Exists(targetFile))
            {
                File.Copy(targetFile, tempFile);
            }

            // get nuspec file
            string nuspecContents = Utilities.FileRead(nugetFile, true);

            // do we need to replace the version information
            if (nugetVersion)
            {
                nuspecContents = nuspecContents.Replace("$version$", nugetVersionString);
            }

            tempFile = targetPathTemp + Path.GetFileName(nugetFile);
            Utilities.FileWrite(tempFile, nuspecContents);

            // call nuget to create the package
            ProcessStartInfo nugetStartInfo = new ProcessStartInfo(nugetExe,
                String.Format("pack \"{0}\" -NoPackageAnalysis",
                tempFile));
            nugetStartInfo.WorkingDirectory = Path.GetDirectoryName(tempFile);
            nugetStartInfo.UseShellExecute = false;
            Process nugetProc = Process.Start(nugetStartInfo);

            nugetProc.WaitForExit();

            string[] compiledPackages = Directory.GetFiles(Path.GetDirectoryName(tempFile),
                "*.nupkg", SearchOption.AllDirectories);

            if (compiledPackages.Length > 0)
            {
                nugetPackage = compiledPackages[0].Replace("temp\\", String.Empty);

                if (File.Exists(nugetPackage))
                {
                    if (nugetOverwrite)
                    {
                        File.Delete(nugetPackage);
                    }
                    else
                    {
                        Console.WriteLine("Nuget Package already exists");
                        return (false);
                    }
                }

                File.Move(compiledPackages[0], nugetPackage);
                Console.WriteLine("Nuget Package Created");
            }
            else
            {
                // new package not found
                Console.WriteLine("Failed to create Nuget Package");
                return (false);
            }

            Directory.Delete(targetPathTemp, true);

            return (true);
        }

        /// <summary>
        /// Pushes a Nuget package to a server
        /// </summary>
        internal static void PushNugetPackage()
        {
            if (!nugetPush)
                return;

            // call nuget to push the package
            if (!File.Exists(nugetPackage))
            {
                Console.WriteLine("Nuget Package not found!");
                return;
            }

            ProcessStartInfo nugetStartInfo = new ProcessStartInfo(nugetExe,
                String.Format("push \"{0}\" {1} -Source {2}",
                nugetPackage, nugetKey, nugetPushSource));
            nugetStartInfo.WorkingDirectory = Path.GetDirectoryName(Path.GetDirectoryName(nugetPackage));
            nugetStartInfo.UseShellExecute = false;
            Process nugetProc = Process.Start(nugetStartInfo);

            nugetProc.WaitForExit();

            if (nugetProc.ExitCode == 0)
            {
                Console.WriteLine("Nuget Push Complete");
            }
            else
            {
                Console.WriteLine("Nuget Push Failed");
            }

        }

        #endregion Private Static Methods
    }
}
