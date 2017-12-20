using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using DevExpress.Skins;
using DevExpress.LookAndFeel;
using DevExpress.UserSkins;
using DevExpress.XtraBars;
using DevExpress.XtraBars.Ribbon;
using DevExpress.XtraBars.Helpers;
using DevExpress.XtraEditors.Controls;
using System.Data.SQLite;
using System.IO;
using System.Threading;
using System.Diagnostics;
using DevExpress.XtraLayout;
using System.Net;
using Common;
using FullTextIndex;
using NetWork;

namespace DotNetFullTextIndex
{
    public partial class Main : RibbonForm
    {
        private SQLiteCon dbsqlite;
        private IndexOpt indexer;
        private NetOpt netopt;

        public Main()
        {
            InitializeComponent();
            InitSkinGallery();
            InitCon();
        }

        void InitSkinGallery()
        {
            SkinHelper.InitSkinGallery(rgbiSkins, true);
        }

        private void InitCon() {
            INIClass.INIPath = GlobalVar.INI_strSysIniPath;
            dbsqlite = new SQLiteCon();
            dbsqlite.Init();

            indexer = new IndexOpt();
            indexer.Init(dbsqlite.DBPath);
            indexer.IndexBuildFinish += IndexBuildFinishCallBack;
            indexer.SearchFinish += SearchFinishCallBack;

            netopt = new NetOpt();
            netopt.RemoteSearchRequest += RemoteSearchRequestCallBack;
            netopt.RemoteSearchResponse += RemoteSearchResponseCallBack;

            List<string> listFolder = indexer.GetFolderList();
            foreach(string str in listFolder){
                CheckedListBoxItem chkLBi = new CheckedListBoxItem(str, false);
                chkListBoxFolder.Items.Add(chkLBi);
            }
            txtIndexPath.Text = indexer.GetIndexDir();
            chkLocalSearch.Checked = true;
        }

        #region indexPathSet

        #region get folder path
        /// <summary>
        /// get folder path by FolderBrowserDialog
        /// </summary>
        /// <param name="oldpath">the initial path of dialog</param>
        /// <returns></returns>
        private string getBrowserDialog(string oldpath) {
            FolderBrowserDialog indexFolderDlg = new FolderBrowserDialog();
            if (oldpath.Trim() != "")
            {
                DirectoryInfo dDir = new DirectoryInfo(oldpath.Trim());
                if (dDir.Exists)
                {
                    indexFolderDlg.SelectedPath = oldpath ;    
                }
            }
            if (indexFolderDlg.ShowDialog(this) == DialogResult.OK)
            {
                return indexFolderDlg.SelectedPath;
            }
            return "";
        }

        private void btnChooseIndexFolder_Click(object sender, EventArgs e)
        {
            string retPath = getBrowserDialog(txtIndexPath.Text);
            if (retPath != "") {
                txtIndexPath.Text = retPath;
            }
        }

        private void btnChooseFolder_Click(object sender, EventArgs e)
        {
            string retPath = getBrowserDialog(txtNewFolder.Text);
            if (retPath != "")
            {
                txtNewFolder.Text = retPath;
            }
        }

        private void txtIndexPath_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length == 1)
                {
                    DirectoryInfo dir = new DirectoryInfo(files[0]);
                    if (dir.Exists)
                    {
                        txtIndexPath.Text = dir.FullName;
                    }
                }
            }
        }

        private void txtNewFolder_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length == 1)
                {
                    DirectoryInfo dir = new DirectoryInfo(files[0]);
                    if (dir.Exists)
                    {
                        txtNewFolder.Text = dir.FullName;
                    }
                }
            }
        }
        #endregion get folder path

        private void btnSetIndexFolder_Click(object sender, EventArgs e)
        {
            indexer.SetIndexDir(txtIndexPath.Text);
        }

        private void btnAddFolder_Click(object sender, EventArgs e)
        {
            if (indexer.AddFolder(txtNewFolder.Text, checkRecursive.Checked))
            {
                chkListBoxFolder.Items.Add(new CheckedListBoxItem((checkRecursive.Checked?GlobalVar.Str_RecurMark:"")+ txtNewFolder.Text,false));
                txtNewFolder.Text = "";
            }
        }

        private void btnDelFolder_Click(object sender, EventArgs e)
        {
            int selCtn = 0;
            List<CheckedListBoxItem> itemRemove = new List<CheckedListBoxItem>();
            foreach(CheckedListBoxItem clbi in chkListBoxFolder.Items){
                if (clbi.CheckState == CheckState.Checked)
                {
                    selCtn++;
                    if (indexer.DelFolder(clbi.Value as string))
                    {
                        itemRemove.Add(clbi);
                    }
                }
            }
            if (selCtn == 0) {
                MessageBox.Show("请选择要操作的项目");
            }
            foreach (CheckedListBoxItem clbi in itemRemove) {
                chkListBoxFolder.Items.Remove(clbi);
            }
        }

        private void btnRecursive_Click(object sender, EventArgs e)
        {
            int selCtn = 0;
            int itemModify = 0 ;
            foreach (CheckedListBoxItem clbi in chkListBoxFolder.Items)
            {
                selCtn++;
                if (indexer.Recursive(clbi.Value as string)) {
                    itemModify++;
                }
            }
            if (selCtn == 0)
            {
                MessageBox.Show("请选择要操作的项目");
            }
            else {
                MessageBox.Show(String.Format("共选择{0}项，成功修改{1}项",selCtn,itemModify));
            }
        }

        private void btnCancelRecursive_Click(object sender, EventArgs e)
        {
            int selCtn = 0;
            int itemModify = 0;
            foreach (CheckedListBoxItem clbi in chkListBoxFolder.Items)
            {
                selCtn++;
                if (indexer.UnRecursive(clbi.Value as string))
                {
                    itemModify++;
                }
            }
            if (selCtn == 0)
            {
                MessageBox.Show("请选择要操作的项目");
            }
            else
            {
                MessageBox.Show(String.Format("共选择{0}项，成功修改{1}项", selCtn, itemModify));
            }
        }
        #endregion indexPathSet

        #region IndexOption
        private void btnCreateIndex_ItemClick(object sender, ItemClickEventArgs e)
        {
            changeStatusBar("状态：", "正在建立索引...", true);
            Thread buildTh = new Thread(new ParameterizedThreadStart(indexer.BuildIndexThread));
            buildTh.Start(false);
        }

        private void btnRebuildIndex_ItemClick(object sender, ItemClickEventArgs e)
        {
            changeStatusBar("状态：", "正在重建索引...", true);
            Thread buildTh = new Thread(new ParameterizedThreadStart(indexer.BuildIndexThread));
            buildTh.Start(true);
        }

        private void btnUpdateIndex_ItemClick(object sender, ItemClickEventArgs e)
        {

        }

        private void btnIndexOption_ItemClick(object sender, ItemClickEventArgs e)
        {
            xTabConMain.SelectedTabPage = tabPageIndexSet;
        }
        #endregion IndexOption

        private void btnSearch_Click(object sender, EventArgs e)
        {
            if (txtSearch.Text.Trim() == "")
            {
                MessageBox.Show("请输入搜索词", "提示", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                txtSearch.Focus();
                return;
            }
            if (!chkLocalSearch.Checked && !chkP2PSearch.Checked)
            {
                MessageBox.Show("请选择检索源", "提示", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                chkLocalSearch.Focus();
                return;
            }
            if (chkP2PSearch.Checked)
            {
                changeStatusBar("状态:", "发送网络搜索请求", true);
                lcResult.Clear();
                netopt.SendSearchRequest(txtSearch.Text);
            }
            if (chkLocalSearch.Checked)
            {
                changeStatusBar("状态:", "正在本地搜索...", true);
                lcResult.Clear();
                Thread searchTh = new Thread(new ParameterizedThreadStart(indexer.SearchIndexThread));
                SearchObj sobj = new SearchObj(txtSearch.Text, new ClientBrief(null, GlobalVar.LocalPath));
                searchTh.Start(sobj);
            }
        }

        private void iExit_ItemClick(object sender, ItemClickEventArgs e)
        {
            this.Close();
        }

        private void Main_FormClosing(object sender, FormClosingEventArgs e)
        {
            netopt.Exit();
            //System.Environment.Exit(0);//it's a simple way to close all unmanaged resources
        }

        private void changeStatusBar(string strStatus, string strInfo, bool MQshow)
        {
            siStatus.Caption = strStatus;
            siInfo.Caption = strInfo;
            barMQProg.Visibility = MQshow ? BarItemVisibility.Always : BarItemVisibility.OnlyInCustomizing;
        }

        private void IndexBuildFinishCallBack(object sender, MessageEventArgs e)
        {
            object[] retObj = e.MessageObj as object[];
            long[] retNumber = retObj[0] as long[];
            long watchCount = (long)retObj[1];
            string message = String.Format("索引建立完成：共插入{0}项,含{1}字符,用时{2}秒",
                    retNumber[0], retNumber[1], watchCount / 1000 + "." + watchCount % 1000);
            changeStatusBar("状态：", message, false);
            MessageBox.Show(message, "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void addResultItemCons(List<TDocs> doclist, ClientBrief targetClient) {
            foreach (TDocs tdoc in doclist)
            {
                ResultItemCon resultCon = new ResultItemCon(tdoc, targetClient);
                lcResult.Controls.Add(resultCon);
                LayoutControlItem lci = new LayoutControlItem(lcResult, resultCon);
                lci.TextVisible = false;
                lci.ControlAlignment = ContentAlignment.TopLeft;
                resultCon.OpenFile += new EventHandler<MessageEventArgs>(OpenFileCallBack);
            }
        }

        private void SearchFinishCallBack(object sender, MessageEventArgs e)
        {
            object[] retObj = e.MessageObj as object[];
            List<TDocs> listResult = retObj[0] as List<TDocs>;
            long watchCount = (long)retObj[1];
            SearchObj sobj = retObj[2] as SearchObj;
            if (listResult == null || sobj == null) return;
            if (sobj.ReqSource.Name == GlobalVar.LocalPath || sobj.ReqSource.Name == Dns.GetHostName())
            {
                changeStatusBar("本地检索完成：", String.Format("共耗时{0}秒,包含结果{1}项", watchCount / 1000 + "." + watchCount % 1000,listResult.Count), false);
                if (listResult.Count == 0)
                {
                    MessageBox.Show("本地检索完毕，未找到匹配项！");
                }
                addResultItemCons(listResult, new ClientBrief(null,GlobalVar.LocalPath));
            }
            else
            {
                changeStatusBar("[远程检索响应结束]", String.Format("请求节点:{0} 耗时{1}秒,包含结果{2}项", sobj.ReqSource.Name, watchCount / 1000 + "." + watchCount % 1000, listResult.Count), false);
                if (listResult.Count == 0) return;
                netopt.SendSearchResult(sobj, listResult);
            }
        }

        private void RemoteSearchRequestCallBack(object sender, MessageEventArgs e)
        {
            SearchObj sobj = e.MessageObj[0] as SearchObj;
            changeStatusBar("[远程检索请求]", sobj.ReqOpt.ToString(), false);
            Thread searchTh = new Thread(new ParameterizedThreadStart(indexer.SearchIndexThread));
            searchTh.Start(sobj);
        }

        private void RemoteSearchResponseCallBack(object sender, MessageEventArgs e)
        {
            ClientBrief client = e.MessageObj[0] as ClientBrief;
            string strRemoteName = client.Name;
                changeStatusBar("[正在加载局域网搜索结果]", "节点:" + client.Name, true);
            List<TDocs> listResult = e.MessageObj[1] as List<TDocs>;
            addResultItemCons(listResult, client);
            changeStatusBar("[局域网搜索结果加载完成]", "节点:" + client.Name + "  新增结果" + listResult.Count + "项", false);
        }

        private void OpenFileCallBack(object sender, MessageEventArgs e) {
            ClientBrief target = e.MessageObj[0] as ClientBrief;
            string sourcePath = e.MessageObj[1] as string;
            if (target.Name == GlobalVar.LocalPath)
            {
                try
                {
                    System.Diagnostics.Process.Start(sourcePath);
                }
                catch (Exception ex) {
                    Debug.WriteLine(ex.Message);
                }
            }
            else {
                netopt.FileRequest(target.IEP , sourcePath);
            }
        }

    }
}