using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using Common;

namespace NetWork
{
    public class StateObject
    {
        public Socket workSocket = null;
        public const int BUFFER_SIZE = 65536;
        public byte[] buffer = new byte[BUFFER_SIZE + 1];
        public StringBuilder sb = new StringBuilder();
        public byte[] dataBuf;// = new byte[1 << 31-1];
        public int offset = 0;
    }

    //[Flags], maybe used
    public enum MultiCastType : byte { 
        OnLine = 0x00,
        OffLine = 0xFF,
        SearchRequest = 0xAA
    }
    /// <summary>
    /// message should be wrapped to this object when send to multicast group
    /// </summary>
    public class MultiCastObject
    {
        private static char SplitChar { get { return ':'; } }

        public MultiCastType ObjType { get; set; }
        private IPAddress RemoteIP { get; set; }
        private Int16 RemotePort { get; set; }
        private IPEndPoint remoteIEP;
        public IPEndPoint RemoteIEP 
        {
            get {
                return remoteIEP;
            }
            set {
                if (value == null) return;
                RemoteIP = value.Address;
                RemotePort = (short)value.Port;
                remoteIEP = value;
            } 
        }
        public string RemoteName { get; set; }
        public string Message { get; set; }

        public MultiCastObject() { 
            
        }

        public MultiCastObject(byte[] recvByte)
        {
            ObjType = (MultiCastType)recvByte[0];
            RemoteIP = new IPAddress(recvByte.Skip(1).Take(4).ToArray());
            RemotePort = BitConverter.ToInt16(recvByte,5);
            RemoteIEP = new IPEndPoint(RemoteIP, RemotePort);
            string strRecv = Encoding.Unicode.GetString(recvByte.Skip(7).ToArray());
            RemoteName = strRecv.Substring(0, strRecv.IndexOf(SplitChar));
            Message = strRecv.Substring(strRecv.IndexOf(SplitChar) + 1);
        }

        public MultiCastObject(MultiCastType objtype, IPEndPoint iep, string remotename, string message)
        {
            ObjType = objtype;
            RemoteIEP = iep;
            RemoteName = remotename;
            Message = message;
        }

        public byte[] GetBytes() {
            List<byte> list = new List<byte>();
            list.Add((byte)ObjType);
            list.AddRange(RemoteIP.GetAddressBytes());
            list.AddRange(BitConverter.GetBytes(RemotePort));
            list.AddRange(Encoding.Unicode.GetBytes(RemoteName + SplitChar));
            list.AddRange(Encoding.Unicode.GetBytes(Message));//should contain a '\0' in the end
            return list.ToArray();
        }
    }

    public class FileRequestObjcet
    {
        private static char SplitPath { get { return '#'; } }
        private static char SplitIEP { get { return ':'; } }
        public static int IDLength { get { return 8; } }
        public string ID { get; set; }
        public IPEndPoint RemoteIEP { get; set; }
        public string FilePath { get; set; }

        public FileRequestObjcet() { 
            
        }

        public FileRequestObjcet(string id, IPEndPoint remoteIEP, string path) {
            ID = id;
            RemoteIEP = remoteIEP;
            FilePath = path;
        }

        public FileRequestObjcet(string str)
        {
            ID = str.Substring(0, IDLength);
            string strIEP = str.Substring(IDLength, str.IndexOf(SplitPath)-IDLength);
            string strIP = strIEP.Substring(0, strIEP.IndexOf(SplitIEP));
            string strPort = strIEP.Substring(strIEP.IndexOf(SplitIEP) + 1);
            RemoteIEP = new IPEndPoint(IPAddress.Parse(strIP),Int32.Parse(strPort));
            FilePath = str.Substring(str.IndexOf(SplitPath)+1);
        }

        public override string ToString()
        {
            return ID + RemoteIEP.ToString() + SplitPath + FilePath;
        }
    }

    public class FileWaitObjcet {
        public static int IDLength { get { return 8; } }
        public string ID { get; set; }
        public string SavePath { get; set; }
    }
}
