using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SQLite;
using System.Windows.Forms;
using System.Data;
using System.Diagnostics;

namespace System.Data.SQLite
{
    public class SQLiteClass
    {
        private string dBPath = "";

        public string DBSource
        {
            get { 
                return string.Format("data source={0}", dBPath);  
            }
            set { dBPath = value; }
        }

        public SQLiteClass(){
        }

        public SQLiteClass(string path)
        {
            DBSource = path;
        }

        bool TestConnection()
        {
            try
            {
                using (SQLiteConnection conn = new SQLiteConnection( DBSource))
                {
                    conn.Open();
                    conn.Close();
                }
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                return false;
            }
        }

        public bool createDB(string fullpath){
            DBSource = fullpath;
            return TestConnection();
        }

        public bool openDB(string fullpath){
            DBSource = fullpath;
            return TestConnection();
        }

        public void createTable(SQLiteTable sqltable) {
            using (SQLiteConnection conn = new SQLiteConnection(DBSource))
            {
                using (SQLiteCommand cmd = new SQLiteCommand())
                {
                    conn.Open();
                    cmd.Connection = conn;

                    SQLiteHelper sh = new SQLiteHelper(cmd);

                    sh.DropTable(sqltable.TableName);
                    sh.CreateTable(sqltable);
                    conn.Close();
                }
            }
        }

        public void insertRow(string tbname,Dictionary<string, object> dic)
        {
            using (SQLiteConnection conn = new SQLiteConnection(DBSource))
            {
                using (SQLiteCommand cmd = new SQLiteCommand())
                {
                    cmd.Connection = conn;
                    conn.Open();

                    SQLiteHelper sh = new SQLiteHelper(cmd);

                    sh.Insert(tbname, dic);
                    conn.Close();
                }
            }
        }

        public void updateRow(string tbname, string colData, object varData, string colCond, object varCond)
        {
            Dictionary<string, object> dicData = new Dictionary<string, object>();
            dicData[colData] = varData;
            Dictionary<string, object> dic = new Dictionary<string, object>();
            dic[colCond] = varCond;
            updateRow(tbname, dicData, dic);
        }

        public void updateRow(string tbname, Dictionary<string, object> dicData, string colCond, object varCond)
        {
            Dictionary<string, object> dic = new Dictionary<string, object>();
            dic[colCond] = varCond;
            updateRow(tbname, dicData, dic);
        }

        public void updateRow(string tbname, Dictionary<string, object> dicData,Dictionary<string,object> dicCond)
        {
            using (SQLiteConnection conn = new SQLiteConnection(DBSource))
            {
                using (SQLiteCommand cmd = new SQLiteCommand())
                {
                    cmd.Connection = conn;
                    conn.Open();

                    SQLiteHelper sh = new SQLiteHelper(cmd);

                    sh.Update(tbname, dicData, dicCond);
                    
                    conn.Close();
                }
            }
        }

        public void deleteRow(string tbName,string colCond, object varCond){
            Dictionary<string, object> dic = new Dictionary<string, object>();
            dic[colCond] = varCond;
            deleteRow(tbName, dic);
        }

        public void deleteRow(string tbName, Dictionary<string,object> dicCond)
        {
            deleteRow(tbName, dicCond, true);
        }

        public void deleteRow(string tbName,Dictionary<string,object> dicCond,bool andOr){
            string sql = String.Format("delete from {0} where ", tbName);
            bool firstItem = true;
            foreach (KeyValuePair<string, object> kv in dicCond)
            {
                if (firstItem)
                    firstItem = false;
                else
                {
                    sql += andOr ? " and " : " or ";
                }
                sql += String.Format(" {0}='{1}' ", kv.Key, kv.Value.ToString());
            }
            executeSql(sql);
        }

        public DataTable selectAll(string tbName)
        {
            string sql = String.Format("select * from {0}", tbName);
            return performSelect(sql);
        }

        public DataTable performSelect(string tbName, string colCond, object varCond)
        {
            Dictionary<string, object> dic = new Dictionary<string, object>();
            dic[colCond] = varCond;
            return performSelect(tbName,dic);
        }

        public DataTable performSelect(string tbName, Dictionary<string, object> dicCond)
        {
            return performSelect(tbName, dicCond, true);
        }

        public DataTable performSelect(string tbName, Dictionary<string, object> dicCond, bool andOr)
        {
            string sql = String.Format("select * from {0} where ", tbName);
            bool firstItem = true;
            foreach (KeyValuePair<string, object> kv in dicCond)
            {
                if (firstItem)
                    firstItem = false;
                else{
                    sql += andOr?" and ":" or ";
                }
                sql += String.Format(" {0}='{1}' ", kv.Key, kv.Value.ToString());
            }
            return performSelect(sql);
        }

        public DataTable performSelect(string sql)
        {
            using (SQLiteConnection conn = new SQLiteConnection(DBSource))
            {
                using (SQLiteCommand cmd = new SQLiteCommand())
                {
                    conn.Open();
                    cmd.Connection = conn;
                    SQLiteHelper sh = new SQLiteHelper(cmd);
                    DataTable dt;
                    try
                    {
                        dt = sh.Select(sql);
                    }
                    catch (Exception ex)
                    {
                        dt = null;
                        Debug.WriteLine(ex.Message);
                    }
                    conn.Close();
                    return dt;
                }
            }
        }

        public void executeSql(string sql){
            using (SQLiteConnection conn = new SQLiteConnection(DBSource))
            {
                using (SQLiteCommand cmd = new SQLiteCommand())
                {
                    conn.Open();
                    cmd.Connection = conn;
                    SQLiteHelper sh = new SQLiteHelper(cmd);
                    sh.Execute(sql);
                    conn.Close();
                }
            }
        }

        public void executeSql(string sql,Dictionary<string,object> dic)
        {
            using (SQLiteConnection conn = new SQLiteConnection(DBSource))
            {
                using (SQLiteCommand cmd = new SQLiteCommand())
                {
                    conn.Open();
                    cmd.Connection = conn;
                    SQLiteHelper sh = new SQLiteHelper(cmd);
                    sh.Execute(sql,dic);
                    conn.Close();
                }
            }
        }
    }
}
