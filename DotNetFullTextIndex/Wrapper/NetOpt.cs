using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using NetWork;
using Common;
using FullTextIndex;
using Newtonsoft.Json;

namespace DotNetFullTextIndex
{
    public class NetOpt
    {
        public event EventHandler<MessageEventArgs> RemoteSearchRequest;
        public event EventHandler<MessageEventArgs> RemoteSearchResponse;

        public static string SearchResponseHead { get { return "SearchResp:"; } }
        public static string FileRequestHead { get { return "FileReq:"; } }
        //private List<FileWaitObjcet> listFileWaitRecv;
        private Dictionary<string, FileWaitObjcet> dicFileWait;
        //private List<FileRequestObjcet> listFileRequest;
        //private static Mutex FileRequestListMutex;
        private static Mutex fileWaitMutex;

        private NetClass netc;
        public Hashtable ClientInGroup; //IEP->list<object>:list[0]->client join DataTime;list[1]->client heartbeat string
        private string recvTmpPath;

        public NetOpt()
        {
            ClientInGroup = new Hashtable();
            //listFileWaitRecv = new List<FileWaitObjcet>();
            dicFileWait = new Dictionary<string, FileWaitObjcet>();
            fileWaitMutex = new Mutex(false);
            //listFileRequest = new List<FileRequestObjcet>();
            try
            {
                netc = new NetClass();
                IPAddress localServiceIP = IPAddress.Parse(INIClass.readIniInfo(GlobalVar.INI_localServiceSect, GlobalVar.INI_localServiceIP));
                int portLocalService;
                if (!Int32.TryParse(INIClass.readIniInfo(GlobalVar.INI_localServiceSect, GlobalVar.INI_localServicePort), out portLocalService)) { return; }
                netc.LocalServiceIEP = new IPEndPoint(localServiceIP, portLocalService);
                int portLocalFile;
                if (!Int32.TryParse(INIClass.readIniInfo(GlobalVar.INI_localServiceSect, GlobalVar.INI_localFileRecvPort), out portLocalFile)) { return; }
                netc.LocalFileRecvIEP = new IPEndPoint(localServiceIP, portLocalFile);

                IPAddress multiCastGroupIP = IPAddress.Parse(INIClass.readIniInfo(GlobalVar.INI_multiCastSect, GlobalVar.INI_multiCastGroupIP));
                int portMultiCastGroup;
                if (!Int32.TryParse(INIClass.readIniInfo(GlobalVar.INI_multiCastSect, GlobalVar.INI_multiCastPort), out portMultiCastGroup)) { return; }
                netc.MultiCastGroupIEP = new IPEndPoint(multiCastGroupIP, portMultiCastGroup);

                recvTmpPath = INIClass.readIniInfo(GlobalVar.INI_systemSect, GlobalVar.INI_sysRecvTmpPath);

                netc.RecvMultiCast += RecvMultiCastCallBack;
                netc.RecvTCPString += RecvTCPStringCallBack;
                netc.RecvFile += FileRecvCallBack;
                netc.Start();
            }
            catch(Exception e){
                Debug.WriteLine(e.Message);
            }
        }

        public void Exit() {
            netc.ExitThread();
        }

        private void RecvMultiCastCallBack(object sender, MessageEventArgs e)
        {
            object[] recvObj = e.MessageObj as object[];
            MultiCastObject mcobj = recvObj[0] as MultiCastObject;
            if (mcobj == null) return;
            switch (mcobj.ObjType) {
                case MultiCastType.OnLine:
                    ClientInGroup.Add(mcobj.RemoteIEP, mcobj.RemoteName);
                    break;
                case MultiCastType.OffLine:
                    ClientInGroup.Remove(mcobj.RemoteIEP);
                    break;
                case MultiCastType.SearchRequest:
                    if (mcobj.RemoteIEP.Equals(netc.LocalServiceIEP)) {
                        break;
                    }
                    if (RemoteSearchRequest != null)
                    {
                        MessageEventArgs argSearch = new MessageEventArgs();
                        argSearch.MessageObj = new object[1];
                        SearchObj sobj = new SearchObj(mcobj.Message, new ClientBrief(mcobj.RemoteIEP,mcobj.RemoteName), mcobj.RemoteIEP);
                        argSearch.MessageObj[0] = sobj;
                        RemoteSearchRequest(this, argSearch);
                    }
                    break;
                default:
                    break;
            }
        }

        private void RecvTCPStringCallBack(object sender, MessageEventArgs e) {
            string strRead = e.MessageObj[0] as string;
            if (strRead.StartsWith(SearchResponseHead))
            {
                searchResponse(strRead.Substring(SearchResponseHead.Length));
            }
            else if (strRead.StartsWith(FileRequestHead))
            {
                string strReq = strRead.Substring(FileRequestHead.Length);
                fileResponse(strReq);
            }
            else
            {

            }
        }

        private void searchResponse(string strResult)
        {
            //in contrast:
            //netc.SendTCPString(remoteIEP, SearchResponseHead + clientLocal.ToString() + GlobalVar.SplitChar_Path + strResult);
            int locaSplit = strResult.LastIndexOf(GlobalVar.SplitChar_Path);
            ClientBrief clientRemote = new ClientBrief(strResult.Substring(0, locaSplit));
            List<TDocs> listResult = JsonConvert.DeserializeObject<List<TDocs>>(strResult.Substring(locaSplit+1));
            object[] obj = new object[3];
            obj[0] = clientRemote;
            obj[1] = listResult;
            if (RemoteSearchResponse != null) {
                System.ComponentModel.ISynchronizeInvoke aSynch = RemoteSearchResponse.Target as System.ComponentModel.ISynchronizeInvoke;
                if (aSynch.InvokeRequired)
                {
                    MessageEventArgs msarg = new MessageEventArgs(obj);
                    object[] args = new object[2] { this, msarg };
                    aSynch.Invoke(RemoteSearchResponse, args);
                }
                else {
                    RemoteSearchResponse(this, new MessageEventArgs(obj));
                }
            }
        }

        private void fileResponse(string strRead)
        {
            try{
                FileRequestObjcet fro = new FileRequestObjcet(strRead);
                FileInfo fi = new FileInfo(fro.FilePath);
                if (fi.Exists)
                {
                    TcpClient fileSendClient = new TcpClient(AddressFamily.InterNetwork);
                    fileSendClient.Connect(fro.RemoteIEP);
                    NetworkStream ns = fileSendClient.GetStream();
                    BinaryWriter bw = new BinaryWriter(ns);
                    bw.Write(fro.ID);
                    FileStream fs = new FileStream(fro.FilePath, FileMode.Open);
                    byte[] readBuf = new byte[NetClass.BUFF_SIZE];
                    int byteRead = 0;
                    while ((byteRead = fs.Read(readBuf, 0, NetClass.BUFF_SIZE)) > 0) {
                        bw.Write(readBuf, 0, byteRead);
                    }
                    fs.Close();
                    bw.Close();
                    ns.Close();
                    fileSendClient.Close();
                }

            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }

        public void SendSearchRequest(string strQuery) { 
            MultiCastObject mcobj = new MultiCastObject();
            mcobj.ObjType = MultiCastType.SearchRequest;
            mcobj.RemoteIEP = netc.LocalServiceIEP;
            mcobj.RemoteName = Dns.GetHostName();
            mcobj.Message = strQuery;
            netc.SendUDPByteArray(mcobj.GetBytes());
        }
        
        public void SendSearchResult(SearchObj sobj,List<TDocs> listResult) 
        {
            IPEndPoint remoteIEP = sobj.ReqSource.IEP;
            /* 
             *JavaScriptSerializer:
             * obj.ToScriptJsonString<T>()
             * obj.ToScriptJsonObject<T>()
             * reference to System.Runtime.Serialization.dll
             * using System.Runtime.Serialization.Json;
             *DataContract:
             * obj.ToJsonString<T>()
             * obj.ToJsonObject<T>()
             * reference to System.Web.Extensions.dll
             * using System.Web.Script.Serialization;
             */
            string strResult = JsonConvert.SerializeObject(listResult);
            ClientBrief clientLocal = new ClientBrief(netc.LocalServiceIEP, Dns.GetHostName());
            netc.SendTCPString(remoteIEP, SearchResponseHead + clientLocal.ToString() + GlobalVar.SplitChar_Path + strResult);
        }

        public void FileRequest(IPEndPoint remote, string path) 
        {
            DialogResult dr = MessageBox.Show("是否保存文件：是[保存并打开]，否[仅打开]", "选择", MessageBoxButtons.YesNoCancel);
            string fileSavePath="";
            if (dr == DialogResult.Yes) {
                SaveFileDialog sfdlg = new SaveFileDialog();
                if (sfdlg.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                    fileSavePath = sfdlg.FileName;
                }
            }
            else if (dr == DialogResult.No)
            {
                int loca = path.LastIndexOf('\\');
                //int DateTime.Now.ToString format, fff... stands for milliseconds
                //for example ss-ffff:57-2141
                fileSavePath = recvTmpPath + "\\" + DateTime.Now.ToString("yyyyMMddHHmmss") + "_";
                string fileSaveName;
                while (true)
                {
                    fileSaveName = CommMethod.RandString(4) + "_";
                    if (loca > 0)
                    {
                        fileSaveName += path.Substring(loca + 1);
                    }
                    else {
                        fileSaveName += path;
                    }
                    FileInfo fs = new FileInfo(fileSavePath + fileSaveName);
                    if (!fs.Exists)
                    {
                        fileSavePath += fileSaveName;
                        break;
                    }
                }
            }
            else {
                return;
            }
            FileWaitObjcet fwo = new FileWaitObjcet();
            fwo.ID = CommMethod.RandString(FileWaitObjcet.IDLength);
            fwo.SavePath = fileSavePath;

            fileWaitMutex.WaitOne();
            while (dicFileWait.ContainsKey(fwo.ID)) {
                fwo.ID = CommMethod.RandString(FileWaitObjcet.IDLength);
            }
            dicFileWait.Add(fwo.ID, fwo);
            fileWaitMutex.ReleaseMutex();
            
            FileRequestObjcet fro =new FileRequestObjcet(fwo.ID, netc.LocalFileRecvIEP, path);
            netc.SendTCPString(remote, FileRequestHead + fro.ToString());
        }

        private void FileRecvCallBack(object sender, MessageEventArgs e)
        {
            try
            {
                TcpClient fileRecvClient = e.MessageObj[0] as TcpClient;
                if (fileRecvClient == null) return;
                NetworkStream netStream = fileRecvClient.GetStream();
                BinaryReader br = new BinaryReader(netStream);
                string strID = br.ReadString();
                fileWaitMutex.WaitOne();
                if (!dicFileWait.ContainsKey(strID)) {
                    br.Close();
                    netStream.Close();
                    fileRecvClient.Close();
                    fileWaitMutex.ReleaseMutex();
                }
                FileWaitObjcet fro = dicFileWait[strID];
                dicFileWait.Remove(strID);
                fileWaitMutex.ReleaseMutex();
                FileStream fs = new FileStream(fro.SavePath, FileMode.Create);
                int readCtn = 0;
                byte[] readBuff = new byte[NetClass.BUFF_SIZE];
                while ((readCtn = br.Read(readBuff, 0, NetClass.BUFF_SIZE)) > 0) {
                    fs.Write(readBuff, 0, readCtn);
                }
                fs.Close();
                br.Close();
                netStream.Close();
                fileRecvClient.Close();
                System.Diagnostics.Process.Start(fro.SavePath);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }
    }
}
