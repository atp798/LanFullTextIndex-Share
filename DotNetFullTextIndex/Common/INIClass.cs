using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;

namespace Common
{
    public class INIClass
    {
        static string iniPath = "";
        public static string INIPath {
            get {
                return string.Format("data source={0}", iniPath);  
            }
            set {
                /*
                if (!File.Exists(iniPath))
                {
                    FileInfo finfo = new FileInfo(iniPath);
                    try
                    {
                        finfo.Create();
                    }
                    catch (Exception exx)
                    {
                        MessageBox.Show(exx.Message);
                    }
                }
                 */
                iniPath = value;
            }
        }

        [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool WritePrivateProfileString(
            string lpAppName, string lpKeyName, string lpString, string lpFileName);
        [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int GetPrivateProfileString(
            string lpAppName, string lpKeyName, string lpDefault, StringBuilder lpReturnedString,
            int nSize, string lpFileName);

        public INIClass(){
        }

        public static void wirteIniInfo(string sect,string key,string val)
        {
            WritePrivateProfileString(sect, key, val,iniPath);
        }

        public static string readIniInfo(string sect,string key){
            const int maxLen=1024;
            StringBuilder sbstr=new StringBuilder(maxLen);
            GetPrivateProfileString(sect,key,"",sbstr,maxLen,iniPath);
            return sbstr.ToString();
        }
    }
}
