using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;
using System.Data;
using Common;
using FullTextIndex;
using System.Data.SQLite;
using System.Diagnostics;

namespace DotNetFullTextIndex
{
    class IndexOpt
    {
        public event EventHandler<MessageEventArgs> IndexBuildFinish;
        public event EventHandler<MessageEventArgs> SearchFinish;

        private SQLiteClass dbsqlite;
        private Index m_Index;

        public IndexOpt()
        {
            m_Index = new Index();
            dbsqlite = new SQLiteClass();
        }

        public void Init(string dbPath)
        {
            dbsqlite.openDB(dbPath);
            m_Index.INDEX_DIR = INIClass.readIniInfo(GlobalVar.INI_indexSect, GlobalVar.INI_indexFolder);
            DataTable dt = dbsqlite.selectAll(GlobalVar.TBN_Folder);
            int i = 0;
            for (i = 0; i < dt.Rows.Count; i++)
            {
                m_Index.FolderDic[dt.Rows[i][GlobalVar.FCOL_Path].ToString()] = Convert.ToInt32(dt.Rows[i][GlobalVar.FCOL_Recursive]) == 0 ? false : true;
            }
        }

        public string GetIndexDir() {
            return m_Index.INDEX_DIR;
        }

        public bool SetIndexDir(string path) {
            if (path.Trim() == "")
            {
                MessageBox.Show("请输入索引存储路径", "提示");
                return false;
            }
            DirectoryInfo dir = new DirectoryInfo(path.Trim());
            if (dir.Exists)
            {
                m_Index.INDEX_DIR = dir.FullName;
                INIClass.wirteIniInfo(GlobalVar.INI_indexSect, GlobalVar.INI_indexFolder, m_Index.INDEX_DIR);
                MessageBox.Show("设置索引存储路径成功", "提示");
                return true;
            }
            else
            {
                MessageBox.Show("设置索引存储路径失败：路径不存在", "错误");
                return false;
            }
        }
        #region folderOption
        public List<string> GetFolderList()
        {
            List<string> listRet = new List<string>();
            foreach (string path in m_Index.FolderDic.Keys)
            {
                if (m_Index.FolderDic[path])
                    listRet.Add(GlobalVar.Str_RecurMark + path);
                else
                    listRet.Add(path);
            }
            return listRet;
        }

        public bool AddFolder(string path, bool recursive) {
            if (path.Trim() == "") {
                MessageBox.Show("请输入索引路径", "提示");
                return false;
            }
            DirectoryInfo dir = new DirectoryInfo(path);
            if (dir.Exists)
            {
                if (!m_Index.FolderDic.ContainsKey(dir.FullName))
                {
                    m_Index.FolderDic[dir.FullName] = recursive;
                    Dictionary<string, object> dic = new Dictionary<string, object>();
                    dic[GlobalVar.FCOL_Path] = dir.FullName;
                    dic[GlobalVar.FCOL_Recursive] = recursive?1:0;
                    dbsqlite.insertRow(GlobalVar.TBN_Folder, dic);
                    MessageBox.Show("成功添加路径", "提示");
                    return true;
                }
                else
                {
                    MessageBox.Show("添加路径失败：路径已存在并添加", "提示");
                }
            }
            else
            {
                MessageBox.Show("添加路径失败：路径不存在", "提示");
            }
            return false;
        }

        public bool DelFolder(string strItem)
        {
            if (strItem == null)
            {
                MessageBox.Show("删除路径失败:路径错误", "提示");
                return false; }
            string path = strItem;
            bool recursive = false;
            if (strItem.StartsWith(GlobalVar.Str_RecurMark))
            {
                path = strItem.Substring(GlobalVar.Str_RecurMark.Length);
                recursive = true;
            }
            if (m_Index.FolderDic.ContainsKey(path))
            {
                m_Index.FolderDic.Remove(path);
                Dictionary<string, object> dic = new Dictionary<string, object>();
                dic[GlobalVar.FCOL_Path] = path;
                dic[GlobalVar.FCOL_Recursive] = recursive?1:0;
                dbsqlite.deleteRow(GlobalVar.TBN_Folder, dic);
                MessageBox.Show("删除路径成功", "提示");
                return true;
            }
            else
            {
                MessageBox.Show("删除路径失败，路径不存在，请重启软件", "提示");
                return false;
            }
        }

        public bool Recursive(string strItem)
        {
            if (strItem == null)
            {
                return false;
            }
            if (strItem.StartsWith(GlobalVar.Str_RecurMark))
            {
                return false;
            }
            if (m_Index.FolderDic.ContainsKey(strItem))
            {
                m_Index.FolderDic[strItem] = true ;
                Dictionary<string, object> dic = new Dictionary<string, object>();
                dic[GlobalVar.FCOL_Recursive] = 1;
                Dictionary<string, object> dicCon = new Dictionary<string, object>();
                dicCon[GlobalVar.FCOL_Path] = strItem;
                dbsqlite.updateRow(GlobalVar.TBN_Folder,dic,dicCon);
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool UnRecursive(string strItem)
        {
            if (strItem == null)
            {
                return false;
            }
            if (!strItem.StartsWith(GlobalVar.Str_RecurMark))
            {
                return false;
            }
            string path = strItem.Substring(GlobalVar.Str_RecurMark.Length);
            if (m_Index.FolderDic.ContainsKey(path))
            {
                m_Index.FolderDic[strItem] = true;
                Dictionary<string, object> dic = new Dictionary<string, object>();
                dic[GlobalVar.FCOL_Recursive] = 0;
                Dictionary<string, object> dicCon = new Dictionary<string, object>();
                dicCon[GlobalVar.FCOL_Path] = path;
                dbsqlite.updateRow(GlobalVar.TBN_Folder, dic, dicCon);
                return true;
            }
            else
            {
                return false;
            }
        }
        #endregion folderOption
        public void BuildIndexThread(object rebuild)
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();
            long[] ret = m_Index.BuildIndex((bool)rebuild);
            watch.Stop();
            if (IndexBuildFinish != null)
            {
                MessageEventArgs arg = new MessageEventArgs();
                arg.MessageObj = new object[2];
                arg.MessageObj[0] = ret;
                arg.MessageObj[1] = watch.ElapsedMilliseconds; 
                //avoid cross thread calling error
                System.ComponentModel.ISynchronizeInvoke aSynch = SearchFinish.Target as System.ComponentModel.ISynchronizeInvoke;
                if (aSynch.InvokeRequired)
                {
                    object[] args = new object[2] { this, arg };
                    aSynch.Invoke(IndexBuildFinish, args);
                }
                else
                {
                    IndexBuildFinish(this, arg);
                }
            }
        }

        public void SearchIndexThread(object obj){
            Stopwatch watch = new Stopwatch();
            watch.Start();
            SearchObj sobj = obj as SearchObj;
            if (sobj == null) return;
            List<TDocs> listResult = new List<TDocs>();
            try
            {
                listResult = m_Index.Search(sobj.StrQuery);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            watch.Stop();
            if (SearchFinish != null)
            {
                MessageEventArgs arg = new MessageEventArgs();
                arg.MessageObj = new object[3];
                arg.MessageObj[0] = listResult;
                arg.MessageObj[1] = watch.ElapsedMilliseconds;
                arg.MessageObj[2] = sobj;
                System.ComponentModel.ISynchronizeInvoke aSynch = SearchFinish.Target as System.ComponentModel.ISynchronizeInvoke;
                if (aSynch.InvokeRequired)
                {
                    object[] args = new object[2] { this, arg };
                    aSynch.Invoke(SearchFinish, args);
                }
                else
                {
                    SearchFinish(this, arg);
                }
            }
        }
    }
}
