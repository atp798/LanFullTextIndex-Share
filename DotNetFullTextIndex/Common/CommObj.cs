using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace Common
{
    public enum ClientType
    {
        LOCAL, REMOTE
    }

    public class ClientBrief
    {
        private static char SplitChar { get { return '#'; } }

        //public ClientType Type { get; set; }
        private IPEndPoint iep;
        public IPEndPoint IEP
        {
            get
            {
                return iep;
            }
            set
            {
                if (value == null) return;
                IP = value.Address;
                Port = (short)value.Port;
                iep = value;
            }
        }
        private IPAddress IP { get; set; }
        private Int16 Port { get; set; }
        private string name;
        public string Name
        {
            get
            {
                return name;
            }
            set
            {
                name = value;
            }
        }

        public ClientBrief()
        {
        }

        public ClientBrief(IPEndPoint iep, string strname)
        {
            IEP = iep;
            Name = strname;
        }

        public ClientBrief(string str)
        {
            int locaSplit = str.IndexOf(SplitChar);
            IEP = CommMethod.IEPParse(str.Substring(0, locaSplit));
            Name = str.Substring(locaSplit + 1);
        }

        public override string ToString()
        {
            if (IEP == null) return null;
            return IEP.ToString() + SplitChar + Name;
        }
    }

    public class SearchObj
    {
        public string StrQuery { get; set; }
        public ClientBrief ReqSource { get; set; }
        public object ReqOpt { get; set; }

        public SearchObj() { 
        }

        public SearchObj(string str)
        {
            StrQuery = str;
        }

        public SearchObj(string str, ClientBrief req):this(str) 
        {
            ReqSource = req;
        }

        public SearchObj(string str, ClientBrief req, object obj):this(str,req)
        {
            ReqOpt = obj;
        }
    }

}
