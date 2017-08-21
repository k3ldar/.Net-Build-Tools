/*
 *  The contents of this file are subject to MIT Licence.  Please
 *  view License.txt for further details. 
 *
 *  The Original Code was created by Simon Carter (s1cart3r@gmail.com)
 *
 *  Copyright (c) 2016- 2017 Simon Carter.  All Rights Reserved.
 *
 *  Purpose:  Updates a .net assembly/executable version information during build
 *
 */
using System;

using Shared;
using Shared.Classes;

namespace UpdateVersion
{
    class Program
    {
        #region Main

        static void Main(string[] args)
        {
            string Result = "Parameters\r\n";

            try
            {
                Parameters.Initialise(args, new char[] { '/', '-' }, new char[] { '=', ' ' });

                if (isHelp())
                {
                    return;
                }

                string path = Parameters.GetOption("path", String.Empty);

                if (path.EndsWith("\""))
                    path = path.Substring(0, path.Length - 1);

                path = Utilities.AddTrailingBackSlash(path);

                bool ignore = Parameters.OptionExists("ignore");

                if (ignore)
                {
                    string ignoreFile = Utilities.CurrentPath(true) + "ignoreList.dat";
                    string ignoreList = Utilities.FileRead(ignoreFile, false);
                    string newFile = String.Empty;

                    foreach (string line in ignoreList.Split('\n'))
                    {
                        if (String.IsNullOrEmpty(line))
                            continue;

                        if (line.StartsWith(path))
                        {
                            string[] parts = line.Split('#');

                            if (parts.Length < 2)
                                continue;

                            DateTime ignoreDate = Utilities.StrToDateTime(parts[1]);
                            double minutes = Parameters.GetOption("ignore", 0);
                            TimeSpan span = DateTime.Now - ignoreDate;

                            if (span.TotalMinutes < minutes)
                                return;
                        }

                        if (!line.StartsWith(path))
                            newFile += line + '\n';
                    } // for


                    if (!newFile.Contains(path))
                    {
                        newFile += String.Format("{0}#{1}\n", path, DateTime.Now);
                    }

                    Utilities.FileWrite(ignoreFile, newFile);
                }

                if (System.IO.Directory.Exists(path))
                {
                    Result += "\r\nPath Exists\r\n";

                    string propertyPath = Utilities.AddTrailingBackSlash(path + "properties") + "AssemblyInfo.cs";

                    if (System.IO.File.Exists(propertyPath))
                    {
                        Result += "\r\n" + propertyPath + " Found\r\n";
                        UpdateFileVersion(propertyPath, ref Result);
                    }
                    else
                    {
                        Result += "\r\n" + propertyPath + " NOT Found\r\n";
                        string file = path + "AssemblyInfo.cs";

                        if (System.IO.File.Exists(file))
                        {
                            Result += "\r\n" + file + " Found\r\n";
                            UpdateFileVersion(file, ref Result);
                        }
                        else
                        {
                            Result += "\r\n" + file + " NOT Found\r\n";
                            path = Utilities.AddTrailingBackSlash(path + "properties");

                            file = path + "AssemblyInfo.cs";

                            if (System.IO.File.Exists(file))
                            {
                                Result += "\r\n" + file + " Found\r\n";
                                UpdateFileVersion(file, ref Result);
                            }
                            else
                            {
                                Result += "\r\n" + file + " NOT Found\r\n";
                            }
                        }
                    }
                }
            }
            catch (Exception err)
            {
                Result += "\r\n" + err.Message;
                EventLog.Add(err);
            }
            finally
            {
                if (Parameters.OptionExists("output"))
                {
                    string outFile = Parameters.GetOption("output", Utilities.CurrentPath(true) + "verinfoUpdate.txt");
                    Utilities.FileWrite(Parameters.GetOption("output", Utilities.CurrentPath(true) + "verinfoUpdate.txt"), Result);
                }
            }
        }

        #endregion Main

        #region Private Consts

        private const string SearchStringAssembly = "\n[assembly: AssemblyVersion(\"";
        private const string SearchStringFile = "\n[assembly: AssemblyFileVersion(\"";
        private const string NewStringAssembly = "\r\n[assembly: AssemblyVersion(\"1.0.0.0\")]";
        private const string NewStringFile = "\r\n[assembly: AssemblyFileVersion(\"1.0.0.0\")]";

        #endregion Private Consts

        #region Private Static Methods

        /// <summary>
        /// Determines if we are showing help information
        /// </summary>
        /// <returns>true if help is shown, otherwise false</returns>
        private static bool isHelp()
        {
            Console.WriteLine("UpdateVersion.exe");
            Console.WriteLine("Copyright 2016 - 2017 (C) Simon Carter.  All Rights Reserved.");

            if (Parameters.OptionExists("?"))
            {
                Console.WriteLine("  /?               Help (shows this)");
                Console.WriteLine("  /ignore=T        ignore updating version T = number of minutes");
                Console.WriteLine("  /Path=P          Path to assembly, P = path");
                Console.WriteLine("  /output=F        Writes Results to a file, F = file name/path");
                Console.WriteLine("  /IncreaseMajor   Increases the major number");
                Console.WriteLine("  /IncreaseMinor   Increases the minor number");
                Console.WriteLine("  /IncreaseBuild   Increases the build number");
                Console.WriteLine("  /Release         Indicates it is a release build (same as /IncreaseBuild");
                return (true);
            }

            return (false);
        }
        
        private static void UpdateFileVersion(string fileName, ref string output)
        {
            string text = Utilities.FileRead(fileName, false);
            string searchString = Parameters.OptionExists("FileVersion") ? SearchStringFile : SearchStringAssembly;
            int posStart = text.IndexOf(searchString);
            int posEnd = text.IndexOf("\")]", posStart + 1);

            if (posStart == -1)
            {
                // version info not found so add it
                text += Parameters.OptionExists("FileVersion") ? NewStringFile : NewStringAssembly;
                posStart = text.IndexOf(searchString);
                posEnd = text.IndexOf("\")]", posStart + 1);
            }

            string version = text.Substring(posStart + searchString.Length, posEnd - (posStart + searchString.Length));
            version = version.Replace('*', '0').Replace("-", "");
            EventLog.Add(fileName + " " + version);
            Version ver = new Version(version);

            int major = ver.Major;
            int minor = ver.Minor;
            int build = ver.Build;
            int revision = ver.Revision;

            if (Parameters.OptionExists("IncreaseMajor"))
                major++;

            if (Parameters.OptionExists("IncreaseMinor"))
                minor++;

            if (Parameters.OptionExists("IncreaseBuild") || Parameters.OptionExists("Release"))
                build++;

            // always increase revision
            revision++;
            
            string newVersion = String.Format("{0}.{1}.{2}.{3}", major, minor, build, revision);
            
            output += "\r\nNew Version " + newVersion + "\r\n";

            string newFileContents = text.Substring(0, posStart + searchString.Length) + 
                newVersion + text.Substring(posEnd, text.Length - posEnd);

            Utilities.FileWrite(fileName, newFileContents);
        }

        #endregion Private Static Methods
    }
}
