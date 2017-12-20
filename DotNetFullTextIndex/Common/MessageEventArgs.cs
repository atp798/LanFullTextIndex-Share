using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

namespace Common
{
    public class MessageEventArgs:EventArgs
    {
        private object[] messageObj;
        public object[] MessageObj {
            get { return messageObj; }
            set { messageObj = value;}
        }

        public MessageEventArgs() { 
        }

        public MessageEventArgs(object[] message)
        {
            MessageObj = message;
        }
    }
}
