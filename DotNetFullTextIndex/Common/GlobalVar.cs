using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Common
{
    public class GlobalVar
    {
        public static char SplitChar_IEP { get { return ':'; } }

        //after programed, this config is fixed, the file is unable to be moved
        public static string INI_strSysIniPath { get { return ".\\SysConfig.ini"; } }


        #region system
        public static string INI_systemSect { get { return "System"; } }
        public static string INI_sysDBFileName { get { return "dbfile"; } }
        public static string INI_sysRecvTmpPath { get { return "tmprecv"; } }
        #endregion system

        #region sysdb
        public static string TBN_Global { get { return "tb_global"; } }
        public static string GCOL_ID { get { return  "ID"; } }
        public static string GCOL_Category { get { return "Category"; } }
        public static string GCOL_Key  { get { return "Key"; } }
        public static string GCOL_Value { get { return  "Value"; } }
        public static string TBN_Folder { get { return "tb_folder"; } }
        public static string FCOL_FID { get { return  "FID"; } }
        public static string FCOL_Path  { get { return "Path"; } }
        public static string FCOL_Recursive  { get { return "Recursive"; } }
        #endregion sysdb

        #region index
        public static string Str_RecurMark { get { return "[递归子目录]"; } }
        public static string INI_indexSect { get { return  "Index"; } }
        public static string INI_indexFolder { get { return "path"; } }
        #endregion index

        #region log
        public static string INI_sysLogSect { get { return "SysLog"; } }
        public static string INI_sysLogFileName  { get { return "filename"; } }
        #endregion log

        #region Network
        public static string LocalPath { get { return "Localhost"; } }
        /// <summary>
        /// must be a charactor won't appear in file name, according to operation system's rules
        /// in windows: \ / : * ? " < > |
        /// </summary>
        public static char SplitChar_Path { get { return '*'; } }

        public static string INI_serverSect { get { return "Server"; } }
        public static string INI_serverIP { get { return "ip"; } }
        public static string INI_serverPort { get { return "port"; } }

        public static string INI_localServiceSect { get { return "LocalService"; } }
        public static string INI_localServiceIP { get { return "ip"; } }
        public static string INI_localServicePort { get { return "port"; } }
        public static string INI_localFileRecvPort { get { return "fileport"; } }

        public static string INI_multiCastSect { get { return "MultiCast"; } }
        public static string INI_multiCastGroupIP { get { return "ip"; } }
        public static string INI_multiCastPort { get { return "port"; } }

        #endregion Network
    }
}
