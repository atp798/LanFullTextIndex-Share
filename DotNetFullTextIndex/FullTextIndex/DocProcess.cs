using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace FullTextIndex
{
    public class DocProcess
    {
        //single instance pattern
        private static class LazyHolder
        {
            public static DocProcess m_Instance = new DocProcess();
        }

        private DocProcess()
        {
            dicDocType = new Dictionary<string, IDocProc>();
            indexAllFileNames = true;
        }

        public static DocProcess GetInstance(){
            return LazyHolder.m_Instance;
        }

        private static Dictionary<string,IDocProc> dicDocType;
        private bool indexAllFileNames;

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

        public void IndexAllFileNames() {
            indexAllFileNames = true;
        }

        public void UnIndexUnsupportType() {
            indexAllFileNames = false;
        }

        private TDocs defaultProc(FileInfo fi) {
            if (indexAllFileNames)
            {
                TDocs tdoc = new TDocs();
                tdoc.Path = fi.FullName;
                tdoc.Name = fi.Name;
                tdoc.Extension = fi.Extension.ToLower();
                tdoc.Title = tdoc.Name.Substring(0, tdoc.Name.LastIndexOf('.'));
                tdoc.Content = "";
                return tdoc;
            }
            else {
                return null;
            }
        }

        public TDocs DealWithDoc(FileInfo fi)
        {
            if (dicDocType.ContainsKey(fi.Extension.ToLower())) {
                IDocProc idoc = dicDocType[fi.Extension.ToLower()];
                return idoc.Process(fi);
            }
            return defaultProc(fi);
        }
    }
}
