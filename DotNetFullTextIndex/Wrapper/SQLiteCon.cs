using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SQLite;
using System.Data;
using Common;
using System.IO;

namespace DotNetFullTextIndex
{
    class SQLiteCon
    {
        private string dbPath;
        public string DBPath {
            get
            {
                return dbPath;
            }
            set
            {
                dbPath = value;
            }
        }

        private SQLiteClass dbsqlite;

        public SQLiteCon(){
            
        }

        public void Init(){
            dbsqlite = new SQLiteClass();
            DBPath = INIClass.readIniInfo(GlobalVar.INI_systemSect, GlobalVar.INI_sysDBFileName);
            if (DBPath.Trim() == "")
            {
                DBPath = "sysdb.db";
                INIClass.wirteIniInfo(GlobalVar.INI_systemSect, GlobalVar.INI_sysLogFileName, DBPath);
            }
            FileInfo finfo = new FileInfo(DBPath);
            if (finfo.Exists)
            {
                dbsqlite.openDB(DBPath);
            }
            else
            {
                dbsqlite.createDB(DBPath);
                SQLiteTable tb = new SQLiteTable(GlobalVar.TBN_Global);
                tb.Columns.Add(new SQLiteColumn(GlobalVar.GCOL_ID, true));
                tb.Columns.Add(new SQLiteColumn(GlobalVar.GCOL_Category, ColType.Text));
                tb.Columns.Add(new SQLiteColumn(GlobalVar.GCOL_Key, ColType.Text));
                tb.Columns.Add(new SQLiteColumn(GlobalVar.GCOL_Value, ColType.Text));
                dbsqlite.createTable(tb);

                tb = new SQLiteTable(GlobalVar.TBN_Folder);
                tb.Columns.Add(new SQLiteColumn(GlobalVar.FCOL_FID, true));
                tb.Columns.Add(new SQLiteColumn(GlobalVar.FCOL_Path, ColType.Text));
                tb.Columns.Add(new SQLiteColumn(GlobalVar.FCOL_Recursive, ColType.Integer));
                dbsqlite.createTable(tb);
            }
        }
    }
}
