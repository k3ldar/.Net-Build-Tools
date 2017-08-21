/*
 *  The contents of this file are subject to MIT Licence.  Please
 *  view License.txt for further details. 
 *
 *  The Original Code was created by Simon Carter (s1cart3r@gmail.com)
 *
 *  Copyright (c) 2017 Simon Carter.  All Rights Reserved.
 *
 *  Purpose:  Git Tag Wrapper
 *
 */
using System;
using System.Diagnostics;
using System.IO;

using Shared.Classes;

namespace Build.Deploy.Util
{
    internal static class GitWrapper
    {
        #region Git Parameters

        private static string git;
        private static string gitRepositoryName;
        private static string gitVersionName;
        private static string gitTagName;
        private static string gitWorking;

        #endregion Git Parameters

        #region Internl Static Methods

        /// <summary>
        /// Retrieves Git parameters and validates
        /// </summary>
        /// <param name="version"></param>
        /// <returns></returns>
        internal static bool Initialise(FileVersionInfo version)
        {
            gitRepositoryName = Parameters.GetOption("GitRepository", String.Empty);
            gitVersionName = String.Format("{0}_{1}",
                Parameters.GetOption("GitVersionName", version.FileDescription),
                version.FileVersion).ToUpper().Replace(' ', '_');
            gitTagName = String.Format("{0} v{1}",
                gitRepositoryName,
                version.FileVersion);
            git = Parameters.GetOption("git", String.Empty);
            gitWorking = Parameters.GetOption("GitWorkingDir", String.Empty);

            return (File.Exists(git) &&
                !String.IsNullOrEmpty(git) &&
                !String.IsNullOrEmpty(gitWorking) &&
                !String.IsNullOrEmpty(gitRepositoryName));
        }

        internal static void Execute()
        {
            CreateGitTags();
        }

        /// <summary>
        /// Display parameters specific to Git options
        /// </summary>
        internal static void ShowParameters()
        {
            Console.WriteLine("Git Options:");
            Console.WriteLine("  /Git             Path/filename to Git.exe.");
            Console.WriteLine("  /GitRepository   Git Repository Name.");
            Console.WriteLine("  /GitWorkingDir   Repository working directory.");
            Console.WriteLine("  /GitVersionName  Git Version Tag Name (Default is Version.FileDescription.");
            Console.WriteLine("  /GitTagName      Git Tag Name (Default is Version.FileDescription.");
            Console.WriteLine(String.Empty);
        }

        #endregion Internal Static Methods

        #region Private Methods

        private static void CreateGitTags()
        {
            Console.WriteLine("Creating Git tag");
            Console.WriteLine("VersionName: {0}", gitVersionName);
            Console.WriteLine("TagName: {0}", gitTagName);
            Console.WriteLine("Repository: {0}", gitRepositoryName);
            Console.WriteLine("Working Folder: {0}", gitWorking);
            Console.WriteLine(String.Empty);

            // git - create tag and push it to server
            ProcessStartInfo gitStartInfo = new ProcessStartInfo(git,
                String.Format("tag -a {0} -m \"{1}\"",
                gitVersionName, gitTagName));
            gitStartInfo.WorkingDirectory = gitWorking;
            gitStartInfo.UseShellExecute = false;
            Process gitProc = Process.Start(gitStartInfo);

            gitProc.WaitForExit();


            gitStartInfo = new ProcessStartInfo(git,
                String.Format("push origin tag {0}",
                gitVersionName));
            gitStartInfo.WorkingDirectory = gitWorking;
            gitStartInfo.UseShellExecute = false;
            gitProc = System.Diagnostics.Process.Start(gitStartInfo);

            gitProc.WaitForExit();
        }

        #endregion Private Methods
    }
}
