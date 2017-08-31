/*
 *  The contents of this file are subject to MIT Licence.  Please
 *  view License.txt for further details. 
 *
 *  The Original Code was created by Simon Carter (s1cart3r@gmail.com)
 *
 *  Copyright (c) 2017 Simon Carter.  All Rights Reserved.
 *
 *  Purpose:  String Resource Updater
 *
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

using Shared.Classes;

namespace Build.Deploy.Util
{
    internal class ResXUpdater : IDisposable
    {
        #region Private Members

        private Dictionary<string, ResXEntry> _masterEntries = new Dictionary<string, ResXEntry>();

        #endregion Private Members

        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="masterFile">Master ResX File</param>
        /// <param name="slaveFiles">Slave ResX Files</param>
        public ResXUpdater(string masterFile, List<string> slaveFiles)
            : this()
        {
            MasterFile = masterFile;
            SlaveFiles = slaveFiles;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public ResXUpdater()
        {

        }

        #endregion Constructors

        #region Properties

        /// <summary>
        /// Master ResX File
        /// </summary>
        public string MasterFile { get; set; }

        /// <summary>
        /// Files that will be updated with new entries from master file
        /// </summary>
        public List<string> SlaveFiles { get; set; }

        /// <summary>
        /// Indicates wether a backup of the files will be taken prior to updating
        /// </summary>
        public bool Backup { get; set; }

        /// <summary>
        /// Path where backuped up files will be saved
        /// </summary>
        public string BackupPath { get; set; }

        #endregion Properties

        #region Events

        /// <summary>
        /// Event raised when an entry is missing
        /// </summary>
        public event ResXEntryHandler EntryMissing;

        #endregion Events

        #region Event Wrappers

        /// <summary>
        /// Wrapper for EntryMissing event
        /// </summary>
        /// <param name="resxFile"></param>
        /// <param name="entryName"></param>
        private void RaiseEntryMissing(string resxFile, string entryName)
        {
            if (EntryMissing != null)
                EntryMissing(this, new ResXEntryArgs(resxFile, entryName));
        }

        #endregion Event Wrappers

        #region Public Methods

        /// <summary>
        /// Tests slave files to see what, if anything is wrong/missing
        /// </summary>
        public void Run(bool updateFiles)
        {
            if (!File.Exists(MasterFile))
                throw new Exception("MasterFile does not exist");

            if (SlaveFiles == null || SlaveFiles.Count() == 0)
                throw new Exception("No slave files");

            ParseMasterResXFiles();

            foreach (string file in SlaveFiles)
            {
                if (Backup)
                {
                    string backupFile = Shared.Utilities.AddTrailingBackSlash(BackupPath) + Path.GetFileName(file);

                    if (File.Exists(backupFile))
                        File.Delete(backupFile);

                    File.Copy(file, backupFile);
                }

                Dictionary<string, ResXEntry> slaveEntries = ParseSlaveResXFile(file);
                try
                {
                    CompareSlaveEntries(slaveEntries, updateFiles, file);
                }
                finally
                {
                    slaveEntries.Clear();
                    slaveEntries = null;
                }
            }
        }

        #endregion Public Methods

        #region Static Properties

        /// <summary>
        /// Name of primary file, if empty then a default 
        /// file of LanguageStrings.resx will be used
        /// </summary>
        private static string PrimaryFile { get; set; }

        /// <summary>
        /// Primary file, without extension
        /// </summary>
        private static string PrimaryFileName { get; set; }

        /// <summary>
        /// Indicates that only looking for differences
        /// </summary>
        private static bool TestOnly { get; set; }

        /// <summary>
        /// Path to where the project is located
        /// </summary>
        private static string ProjectPath { get; set; }

        /// <summary>
        /// Slave files which will be compared and/or updated
        /// </summary>
        private static List<string> Slaves { get; set; }

        #endregion Static Properties

        #region Internal Static Methods

        /// <summary>
        /// Retrieves Git parameters and validates
        /// </summary>
        /// <param name="version"></param>
        /// <returns></returns>
        internal static bool Initialise()
        {
            if (!Parameters.OptionExists("ResX"))
                return (false);

            ProjectPath = Parameters.GetOption("ResXPath", String.Empty);

            if (String.IsNullOrWhiteSpace(ProjectPath))
                return (false);

            Slaves = new List<string>();

            TestOnly = Parameters.OptionExists("ResXTestOnly");
            PrimaryFile = Parameters.GetOption("ResXPrimary", "LanguageStrings.resx");
            PrimaryFileName = Path.GetFileNameWithoutExtension(PrimaryFile);
            string targetPath = System.IO.Path.GetDirectoryName(ProjectPath);

            string[] allFiles = System.IO.Directory.GetFiles(targetPath, "*.resx");

            foreach (string s in allFiles)
            {
                if (String.IsNullOrEmpty(s))
                    continue;

                if (s.ToLower().EndsWith(PrimaryFile.ToLower()))
                {
                    PrimaryFile = s;
                    continue;
                }

                string file = Path.GetFileName(s).ToLower();

                if (!file.StartsWith(PrimaryFileName.ToLower()))
                    continue;

                Slaves.Add(s);
            }

            return (true);
        }

        internal static int missingCount;

        internal static void Execute()
        {
            ResXUpdater updater = new ResXUpdater(PrimaryFile, Slaves);
            try
            {
                updater.EntryMissing += Updater_EntryMissing;
                updater.BackupPath = Parameters.GetOption("ResXBackup", String.Empty);
                updater.Backup = System.IO.Directory.Exists(updater.BackupPath);
                Console.WriteLine("Checking for missing Resx entries");
                missingCount = 0;
                updater.Run(!TestOnly);

                Console.WriteLine("{0} entries updated.", missingCount);
            }
            finally
            {
                updater.Dispose();
                updater = null;
            }
        }

        private static void Updater_EntryMissing(object sender, ResXEntryArgs e)
        {
            missingCount++;
            Console.WriteLine("Entry {0} missing from {1}", e.EntryName, Path.GetFileName(e.ResXFile));
        }

        internal static void ShowParameters()
        {
            Console.WriteLine("ResX Update Options:");
            Console.WriteLine("  /ResX            Indicates that String Resource Files will be updated,");
            Console.WriteLine("                   All othe deploy options will be ignored if using this.");
            Console.WriteLine("  /ResXPrimary     Name of primary Resource file (LanguageStrings.resx).");
            Console.WriteLine("  /ResXBackup      Path where files will be copied to prior to updating.");
            Console.WriteLine("  /ResXTestOnly    Only test differences, see VS Output window");
            Console.WriteLine("  /ResXPath        Path to the project path (see VS Macro's below)");
            Console.WriteLine(String.Empty);
        }

        #endregion Internal Static Methods

        #region Private Methods

        /// <summary>
        /// Compares a slave ResX file to the master and updates if allowed
        /// </summary>
        /// <param name="slaveResX">Slave entries</param>
        /// <param name="allowUpdate">true if missing entries are to be added to the slave, otherwise false</param>
        /// <param name="slaveResXFile">File for slave ResX entries</param>
        private void CompareSlaveEntries(Dictionary<string, ResXEntry> slaveResX,
            bool allowUpdate, string slaveResXFile)
        {
            XmlDocument xDoc = new XmlDocument();
            try
            {
                xDoc.Load(slaveResXFile);

                foreach (KeyValuePair<string, ResXEntry> entry in _masterEntries)
                {
                    if (!slaveResX.ContainsKey(entry.Key))
                    {
                        RaiseEntryMissing(slaveResXFile, entry.Key);

                        if (allowUpdate)
                        {
                            XmlNode xNode = xDoc.CreateNode(XmlNodeType.Element, "data", "");
                            XmlAttribute xName = xDoc.CreateAttribute("name");
                            XmlAttribute xproperty = xDoc.CreateAttribute("xml:space");
                            xName.Value = entry.Key;
                            xproperty.Value = "preserve";

                            xNode.Attributes.Append(xName);
                            xNode.Attributes.Append(xproperty);

                            XmlNode xValueNode = xDoc.CreateNode(XmlNodeType.Element, "value", "");
                            xValueNode.InnerText = entry.Value.EntryText;
                            xNode.AppendChild(xValueNode);

                            XmlNode xCommentNode = xDoc.CreateNode(XmlNodeType.Element, "comment", "");
                            xCommentNode.InnerText = entry.Value.Description;
                            xNode.AppendChild(xCommentNode);


                            xDoc.GetElementsByTagName("root")[0].InsertAfter(xNode, xDoc.GetElementsByTagName("root")[0].LastChild);

                            // clear values
                            xNode = null;
                            xName = null;
                            xproperty = null;
                            xValueNode = null;
                            xCommentNode = null;
                        }
                    }
                }
            }
            finally
            {
                xDoc.Save(slaveResXFile);
                xDoc = null;
            }
        }

        /// <summary>
        /// Loads all entries from master ResX file
        /// </summary>
        private void ParseMasterResXFiles()
        {
            _masterEntries.Clear();

            XDocument resxXML = XDocument.Load(MasterFile);
            try
            {
                var resxData = from data in resxXML.Root.Descendants("data").ToArray()
                               select new
                               {
                                   ResxName = data.Attribute("name").Value,
                                   ResxValue = data.TryGetElementValue("value"),
                                   ResxDescription = data.TryGetElementValue("comment")
                               };

                foreach (var ele in resxData)
                {
                    try
                    {
                        _masterEntries.Add(ele.ResxName, new ResXEntry(ele.ResxName, ele.ResxValue, ele.ResxDescription));
                    }
                    catch (Exception err)
                    {
                        if (err.Message.Contains(";ajdfa;sdfjk"))
                            throw;
                    }
                }
            }
            finally
            {
                resxXML = null;
            }
        }

        /// <summary>
        /// Loads all entries from Slave ResX file
        /// </summary>
        /// <param name="testOnly"></param>
        private Dictionary<string, ResXEntry> ParseSlaveResXFile(string fileName)
        {
            Dictionary<string, ResXEntry> Result = new Dictionary<string, ResXEntry>();

            XDocument resxXML = XDocument.Load(fileName);
            try
            {
                var resxData = from data in resxXML.Root.Descendants("data")
                               select new
                               {
                                   ResxName = data.Attribute("name").Value,
                                   ResxValue = data.TryGetElementValue("value")
                               };

                foreach (var ele in resxData)
                {
                    Result.Add(ele.ResxName, new ResXEntry(ele.ResxName, ele.ResxValue, String.Empty));
                }
            }
            finally
            {
                resxXML = null;
            }

            return (Result);
        }

        #endregion Private Methods

        #region Public Methods

        public void Dispose()
        {
            _masterEntries.Clear();
            _masterEntries = null;
        }

        #endregion Public Methods
    }

    public static class ElementExtender
    {

        public static string TryGetElementValue(this XElement parentEl, string elementName, string defaultValue = "")
        {
            var foundEl = parentEl.Element(elementName);

            if (foundEl != null)
            {
                return foundEl.Value;
            }

            return defaultValue;
        }

    }

    internal sealed class ResXEntry
    {
        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="name">Name of entry</param>
        /// <param name="text">Text for entry</param>
        /// <param name="description">Description of entry</param>
        internal ResXEntry(string name, string text, string description)
        {
            EntryName = name;
            EntryText = text;
            Description = description;
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        /// Name of the entry
        /// </summary>
        internal string EntryName { get; set; }

        /// <summary>
        /// Text for Entry
        /// 
        /// Can be translated text
        /// </summary>
        internal string EntryText { get; set; }

        /// <summary>
        /// Description of ResX Entry
        /// </summary>
        internal string Description { get; set; }

        #endregion Properties
    }

    public sealed class ResXEntryArgs
    {
        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="resxFile">Slave ResX Filename</param>
        /// <param name="entryName">Entry to be reviewed</param>
        /// <param name="entryType">Type of Entry</param>
        public ResXEntryArgs(string resxFile, string entryName)
        {
            ResXFile = resxFile;
            EntryName = entryName;
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        /// ResXFile being reviewed
        /// </summary>
        public string ResXFile { get; private set; }

        /// <summary>
        /// Name of the entry missing from the ResX file
        /// </summary>
        public string EntryName { get; private set; }

        #endregion Properties
    }

    public delegate void ResXEntryHandler(object sender, ResXEntryArgs e);
}
