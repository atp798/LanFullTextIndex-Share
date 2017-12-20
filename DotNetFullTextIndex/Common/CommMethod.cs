using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Net;

namespace Common
{
    public class CommMethod
    {
        public static string RandString(int length, string charSet = "")
        {
            byte[] b = new byte[4];
            new System.Security.Cryptography.RNGCryptoServiceProvider().GetBytes(b);
            Random r = new Random(BitConverter.ToInt32(b, 0));
            if (charSet == null || charSet == "")
            {
                //another charset:  !\"#$%&'()*+,-./:;<=>?@[\\]^_`{|}~
                charSet = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
            }
            string s = "";
            for (int i = 0; i < length; i++)
            {
                s += charSet.Substring(r.Next(0, charSet.Length - 1), 1);
            }
            return s;
        }

        /// <summary>
        /// Transform string to IPEndPoint object
        /// </summary>
        /// <param name="str">string to be convert, eg："192.168.1.1:80"</param>
        /// <returns></returns>
        public static IPEndPoint IEPParse(string str) { 
            int locaSplit = str.IndexOf(GlobalVar.SplitChar_IEP);
            if(locaSplit<0)return null;
            IPEndPoint ret = null;
            try{
                 ret =  new IPEndPoint(IPAddress.Parse(str.Substring(0, locaSplit)),Int32.Parse(str.Substring(locaSplit+1)));
            }catch(Exception e){
                Debug.WriteLine(e.Message);
            }
            return ret;
        }

        public byte[] hexStringToByte(string hexstring)// ,string charset="utf8")
        {
            int len = hexstring.Length;
            if (len % 2 != 0)
            {
                len++;
                hexstring = "0" + hexstring;
            }
            byte[] byteret = new byte[len / 2];
            int i = 0;
            for (i = 0; 2 * i < len; i++)
            {
                try
                {
                    byteret[i] = byte.Parse(hexstring.Substring(i * 2, 2), System.Globalization.NumberStyles.HexNumber);
                }
                catch
                {
                    throw new ArgumentException("hex is not a valid hex number!", "hex");
                }
            }
            //System.Text.Encoding chs = System.Text.Encoding.GetEncoding(charset);
            return byteret;
        }

        public string byteToString(byte[] hexbyte)
        {
            string stret = "";
            if (hexbyte != null)
            {
                int i = 0;
                for (i = 0; i < hexbyte.Length; i++)
                {
                    stret += hexbyte[i].ToString("X2");
                }
            }
            return stret;
        }

    }
}
