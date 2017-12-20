using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using Common;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.IO;

namespace NetWork
{
    public class NetClass:IDisposable
    {
        public static int BUFF_SIZE { get { return 1024; } } //milliseconds
        public event EventHandler<MessageEventArgs> RecvMultiCast;
        public event EventHandler<MessageEventArgs> RecvTCPString;
        public event EventHandler<MessageEventArgs> RecvFile;

        private static int Interval { get { return 100; } } //milliseconds
        private static int TimeWait { get { return 500; } } //milliseconds
        private static int TimeOUT { get { return 10000; } } //milliseconds


        /// <summary>
        /// local IPEndPoint bind to tcp listener
        /// </summary>
        private IPEndPoint localServiceIEP;
        public IPEndPoint LocalServiceIEP
        {
            get { return localServiceIEP; }
            set { localServiceIEP = value; }
        }
        TcpListener searchListener;
        //private CancellationTokenSource searchConnCTS = new CancellationTokenSource();

        public IPEndPoint LocalFileRecvIEP { get; set; }
        TcpListener fileRecvListener;
        //private CancellationTokenSource fileRecvCTS = new CancellationTokenSource();

        /// <summary>
        /// local IPEndPoint bind to udp multicast listener
        /// </summary>
        private IPEndPoint multiCastGroupIEP;
        public IPEndPoint MultiCastGroupIEP {
            get { return multiCastGroupIEP; }
            set { multiCastGroupIEP = value; }
        }
        UdpClient multiCastUdpClient;
        private CancellationTokenSource multiCastRecvCTS = new CancellationTokenSource();
        //mark whether the resources has been disposed, in case disposed again
        private bool isDisposed = false;

        public NetClass() {
        }

        ~NetClass() { 
            Dispose(false);
        }

        public void Dispose() {
            Dispose(true);
            //notify the GC not to call destructor, in order to improve performance
            GC.SuppressFinalize(this);
        }
        /// <summary>
        /// release the resources, use virtual to make sure the children class inherited will do rightly of it's base class
        /// however, consider this class has only one instance in the program, it's not very necessary to implement the IDisposable
        /// interface, all resources will be recycled by system
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing) {
            if (!this.isDisposed)
            {
                if (disposing)
                {
                    if (multiCastRecvCTS != null)
                    {
                        multiCastRecvCTS.Dispose();
                        multiCastRecvCTS = null;
                    }
                }
            }
            this.isDisposed = true;
        }

        /// <summary>
        /// cancel three listening thread
        /// </summary>
        public void ExitThread()
        {
            searchListener.Stop();
            fileRecvListener.Stop();
            multiCastRecvCTS.Cancel();
        }

        public void Start() {
            try
            {
                Thread multiCastRecvTh = new Thread(MultiCastRecvThread);
                multiCastRecvTh.Start();
                Thread searchTh = new Thread(SearchListenerThread);
                searchTh.Start();
                Thread fileRecvTh = new Thread(FileRecvThread);
                fileRecvTh.Start();
            }
            catch (Exception e)
            {
                Debug.Write(e.Message);
            }
        }

        /// <summary>
        /// main udp function thread
        /// listen & deal the multicast in the local area network
        /// do not transmit more than Internet: 576[MTU limit]-20[IP header]-8[UDP header]=548 bytes,
        ///                           Ethernet:1500           -20           -8            =1472
        /// </summary>
        private void MultiCastRecvThread()
        {
            try
            {
                IPEndPoint mcLocalIEP = new IPEndPoint(LocalServiceIEP.Address, MultiCastGroupIEP.Port);
                multiCastUdpClient = new UdpClient(mcLocalIEP);
                multiCastUdpClient.JoinMulticastGroup(MultiCastGroupIEP.Address, 50);//50 is default TTL
                while (true) {
                    AsyncCallback async = new AsyncCallback(MultiCastRecvCallBack);
                    IAsyncResult result = multiCastUdpClient.BeginReceive(async, multiCastUdpClient);
                    while (result.IsCompleted == false)
                    {
                        if (multiCastRecvCTS.IsCancellationRequested)
                        {
                            multiCastUdpClient.Close();
                            break;
                        }
                        Thread.Sleep(NetClass.Interval);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }

        private void MultiCastRecvCallBack(IAsyncResult iar) {
            try
            {
                UdpClient multiCastUdpClient = (UdpClient)iar.AsyncState;
                IPEndPoint remoteIEP = new IPEndPoint(IPAddress.Any, 0);
                byte[] recv = multiCastUdpClient.EndReceive(iar, ref remoteIEP);
                MultiCastObject mcobj = new MultiCastObject(recv);
                MessageEventArgs arg = new MessageEventArgs();
                arg.MessageObj = new object[1];
                arg.MessageObj[0] = mcobj;
                if (RecvMultiCast != null) {
                    RecvMultiCast(this, arg);
                }
            }
            catch (Exception e) {
                Debug.WriteLine(e.Message);
            }
        }

        /// <summary>
        /// main function thread, 
        /// 1、receive search result
        /// 2、deal with the client file request
        /// </summary>
        private void SearchListenerThread()
        {
            try
            {
                searchListener = new TcpListener(LocalServiceIEP);
                searchListener.Start();
                while (true)
                {
                    //it will block here when no client connect
                    TcpClient searchClient = searchListener.AcceptTcpClient();
                    if (searchClient != null)
                    {
                        Thread searchAcceptTh = new Thread(new ParameterizedThreadStart(AcceptClientThread));
                        searchAcceptTh.Start(searchClient);
                    }

                }
            }//use the exception: "一个封锁操作被对 WSACancelBlockingCall 的调用中断" to exit the thread
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }

        private void AcceptClientThread(object obj){
            try {
                TcpClient searchClient = obj as TcpClient;
                NetworkStream netStream = searchClient.GetStream();
                BinaryReader br = new BinaryReader(netStream);
                BinaryWriter bw = new BinaryWriter(netStream);
                string strRead = br.ReadString();
                if (RecvTCPString != null) {
                    MessageEventArgs arg = new MessageEventArgs();
                    arg.MessageObj = new object[1];
                    arg.MessageObj[0] = strRead;
                    RecvTCPString(this, arg);
                }
                br.Close();
                bw.Close();
                netStream.Close();
                searchClient.Close();
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }

        public void SendUDPByteArray(byte[] request)
        {
            UdpClient mcgReqClient = new UdpClient();
            mcgReqClient.Send(request, request.Length, MultiCastGroupIEP);
            mcgReqClient.Close();
        }

        public void SendTCPString(IPEndPoint remoteIEP, string strSend ) {
            try{
                //use this socket to connect & send info to server
                TcpClient localClient = new TcpClient(AddressFamily.InterNetwork);
                localClient.Connect(remoteIEP);
                NetworkStream ns = localClient.GetStream();
                BinaryWriter bw = new BinaryWriter(ns);
                //BinaryWriter.Write will auto add string length before string send, 
                //      such BinaryReader.ReadString is able to get the right string
                bw.Write(strSend);
                bw.Close();
                ns.Close();
                localClient.Close();
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }

        private void FileRecvThread() {
            try
            {
                fileRecvListener = new TcpListener(LocalFileRecvIEP);
                fileRecvListener.Start();
                while (true) {
                    //it will block here when no client connect
                    TcpClient fileRecvClient = fileRecvListener.AcceptTcpClient();
                    if (fileRecvClient != null)
                    {
                        if (RecvFile != null)
                        {
                            MessageEventArgs arg = new MessageEventArgs();
                            arg.MessageObj = new object[1];
                            arg.MessageObj[0] = fileRecvClient;
                            RecvFile(this, arg);
                        }
                        else {
                            fileRecvClient.Close();
                        }
                    }

                }
            }//use the exception: "一个封锁操作被对 WSACancelBlockingCall 的调用中断" to exit the thread
            catch (Exception e)
            { 
                Debug.WriteLine(e.Message);
            }
        }
    }
}
