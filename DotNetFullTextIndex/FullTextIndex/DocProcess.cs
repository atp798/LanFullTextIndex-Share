using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace FullTextIndex
{
    public class DocProcess
    {
        private static class LazyHolder
        {
            public static DocProcess m_Instance = new DocProcess();
        }

        private DocProcess()
        {
            dicDocType = new Dictionary<string, IDocProc>();
        }

        public static DocProcess GetInstance(){
            return LazyHolder.m_Instance;
        }

        private static Dictionary<string,IDocProc> dicDocType;


        public bool RegisterDocType(string ext, IDocProc docProcObj)
        { 
            if(!ext.StartsWith(".")){
                return false;
            }
            dicDocType[ext] = docProcObj;
            return true;
        }

        public bool UnRegisterDocType(string ext)
        {
            if (dicDocType.ContainsKey(ext)) {
                dicDocType.Remove(ext);
                return true;
            }
            return false;
        }

        public TDocs DealWithDoc(FileInfo fi)
        {
            if (dicDocType.ContainsKey(fi.Extension)) {
                IDocProc idoc = dicDocType[fi.Extension];
                return idoc.Process(fi);
            }
            return null;
        }
    }
}
