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

        public void Register(IDocProc docProcObj)
        {
            List<string> extList = docProcObj.GetSupportExt();
            foreach (string ext in extList)
            {
                if (!ext.StartsWith("."))
                {
                    continue;
                }
                dicDocType[ext] = docProcObj;
            }
        }

        public void UnRegister(IDocProc docProcObj)
        {
            List<string> extList = docProcObj.GetSupportExt();
            foreach (string ext in extList)
            {
                if (dicDocType.ContainsKey(ext))
                {
                    dicDocType.Remove(ext);
                }
            }
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
