/*
 *  The contents of this file are subject to MIT Licence.  Please
 *  view License.txt for further details. 
 *
 *  The Original Code was created by Simon Carter (s1cart3r@gmail.com)
 *
 *  Copyright (c) 2017 Simon Carter.  All Rights Reserved.
 *
 *  Purpose:  FTP Wrapper, not in use 
 *
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Build.Deploy.Util
{
    class FTPWrapper
    {

        #region Private Static Members

#if ALLOW_FTP
        #region FTP Parameters

        private static string ftp_user;
        private static string ftp_pass;
        private static string ftp_server;
        private static string ftp_port;

        #endregion FTP Parameters
#endif

        #endregion Private Static Members

        #region Private Static Methods

#if ALLOW_FTP
        private static bool FTPDetailsGet()
        {
            Console.WriteLine("Obtaining FTP Details");
            ftp_user = Parameters.GetOption("ftpUser", String.Empty);
            ftp_pass = Parameters.GetOption("ftpPassword", String.Empty);
            ftp_server = Parameters.GetOption("ftpServer", String.Empty);
            ftp_port = Parameters.GetOption("ftpPort", "21");

            return (!String.IsNullOrEmpty(ftp_user) && !String.IsNullOrEmpty(ftp_pass) && !String.IsNullOrEmpty(ftp_server));
        }
#endif

        #endregion Private Static Methods
    }
}
