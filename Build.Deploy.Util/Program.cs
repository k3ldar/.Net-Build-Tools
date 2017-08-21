/*
 *  The contents of this file are subject to MIT Licence.  Please
 *  view License.txt for further details. 
 *
 *  The Original Code was created by Simon Carter (s1cart3r@gmail.com)
 *
 *  Copyright (c) 2017 Simon Carter.  All Rights Reserved.
 *
 *  Purpose:  Main Program File
 *
 */
using System;
using System.Diagnostics;
using System.IO;

using Shared.Classes;

namespace Build.Deploy.Util
{
    class Program
    {
        #region Main

        static void Main(string[] args)
        {
            Parameters.Initialise(args, new char[] { '-', '/' }, new char[] { '=' });
                
            // if showing help, show it and get out
            if (HelpWrapper.Initialise())
            {
                return;
            }

            try
            {
                // if debug build and we are ignoring debug, then just exit
                if (!Parameters.OptionExists("Release") && Parameters.OptionExists("IgnoreDebug"))
                {
                    Console.WriteLine("Ignoring Debug Build");
                    return;
                }

                string targetFile = Parameters.GetOption("target", String.Empty);

                if (!File.Exists(targetFile))
                {
                    Console.WriteLine("Could not find target file.");
                    return;
                }

                //Current version of target file
                FileVersionInfo version = FileVersionInfo.GetVersionInfo(targetFile);

                //Add Git release if required
                if (GitWrapper.Initialise(version))
                {
                    GitWrapper.Execute();
                }

                // create/deploy Nuget if required
                if (NugetWrapper.Initialise(version))
                {
                    NugetWrapper.Execute(targetFile);
                }

                if (InnoSetupWrapper.Initialise())
                {
                    InnoSetupWrapper.Execute(version);
                }
            }
            catch (Exception err)
            {
                Console.WriteLine(err.Message);
                Shared.EventLog.Add(err);
            }
            finally
            {
                Console.WriteLine("Build.Deploy.Util Complete");
            }
        }

        #endregion Main
    }
}
