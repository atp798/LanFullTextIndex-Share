using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Word = Microsoft.Office.Interop.Word;
using iTextSharp.text.pdf;
using System.Diagnostics;
using System.Threading;

namespace FullTextIndex
{
    public interface IDocProc
    {
        void SetTimeLimit(int milliSeconds);
        List<string> GetSupportExt();
        TDocs Process(FileInfo fi);
    }

    public class DefaultProc : IDocProc
    {
        private int timeLimit = 100000;
        private List<string> listExt = new List<string>();
        public DefaultProc(){}
        
        public void SetTimeLimit(int milliSeconds)
        {
            timeLimit = milliSeconds;
        }

        public bool AddSupportExt(string ext){
            if (!ext.StartsWith("."))
            {
                return false;
            }
            listExt.Add(ext);
            return true;
        }

        public List<string> GetSupportExt()
        {
            return listExt;
        }

        public TDocs Process(FileInfo fi)
        {
            TDocs tdoc = new TDocs();
            tdoc.Path = fi.FullName;
            tdoc.Name = fi.Name;
            tdoc.Extension = fi.Extension.ToLower();
            tdoc.Title = tdoc.Name.Substring(0, tdoc.Name.LastIndexOf('.'));
            return tdoc;
        }

    }

    public class WordProc : IDocProc
    {
        private int timeLimit = 100000;

        private static int MaxTitle { get { return 30; } }

        public WordProc() {
        }

        ~WordProc(){
        }

        public void SetTimeLimit(int milliSeconds)
        {
            timeLimit = milliSeconds;
        }

        public List<string> GetSupportExt()
        {
            List<string> list = new List<string>();
            list.Add(".doc");
            list.Add(".docx");
            return list;
        }

        public TDocs Process(FileInfo fi)
        {
            if (fi.Name.StartsWith("~$"))
            {
                return null;
            }
            //default max value is 2147483647 = 2^31-1
            StringBuilder strBuilder = new StringBuilder();

            Word.Application app = null;
            bool appOpened = false;
            try
            {
                app = new Microsoft.Office.Interop.Word.Application();
                appOpened = true;
            }
            catch (Exception ex)
            {
                appOpened = false;
                Debug.Write(ex.Message);
            }
        
            Word.Document doc = null;
            object unknow = Type.Missing;

            TDocs tdoc = new TDocs();
            tdoc.Path = fi.FullName;
            tdoc.Name = fi.Name;
            tdoc.Extension = fi.Extension.ToLower();
            tdoc.Title = tdoc.Name.Substring(0, tdoc.Name.LastIndexOf('.'));

            if (!appOpened)
                return tdoc;

            try
            {
                DateTime dtStart = DateTime.Now;
                object conf = false;
                app.Visible = false;
                object file = fi.FullName;
                doc = app.Documents.Open(ref file,
                    ref conf, ref unknow, ref unknow, ref unknow,
                    ref unknow, ref unknow, ref unknow, ref unknow,
                    ref unknow, ref unknow, ref unknow, ref unknow,
                    ref unknow, ref unknow, ref unknow);
                int i = 0;
                int locaTitle = 0;
                string strTitle="";
                //notice that: the index of doc.Paragraphs counts from 1~Count, not starts with 0
                for (i = 1; i <= doc.Paragraphs.Count; i++)
                {
                    string temp = doc.Paragraphs[i].Range.Text.Trim();
                    if (temp == "") {
                        continue;
                    }
                    //find the first line not null, it maybe the title
                    if (locaTitle == 0)
                    {
                        locaTitle = i;
                        strTitle = temp;
                    }
                    strBuilder.AppendLine(temp);
                    if (dtStart.AddMilliseconds(timeLimit) < DateTime.Now)
                    {
                        break;
                    }
                }
                if (strTitle != "" && strTitle.Length < WordProc.MaxTitle)
                {
                    tdoc.Title = strTitle;
                }
                DateTime dtEnd = DateTime.Now;
                TimeSpan timeInter = dtEnd - dtStart;
                tdoc.Content = strBuilder.ToString();
                ((Microsoft.Office.Interop.Word._Document)doc).Close(ref unknow, ref unknow, ref unknow);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            finally
            {
                ((Microsoft.Office.Interop.Word._Application)app).Quit();
            }
            return tdoc;
        }
    }

    public class PdfProc : IDocProc {
        private int timeLimit = 20000;

        public PdfProc() {
        }

        public void SetTimeLimit(int milliSeconds)
        {
            timeLimit = milliSeconds;
        }

        public List<string> GetSupportExt()
        {
            List<string> list = new List<string>();
            list.Add(".pdf");
            return list;
        }

        public TDocs Process(FileInfo fi) {
            StringBuilder strBuilder = new StringBuilder();
            PdfReader pdfReader = null;

            TDocs tdoc = new TDocs();
            tdoc.Path = fi.FullName;
            tdoc.Name = fi.Name;
            tdoc.Extension = fi.Extension.ToLower();
            tdoc.Title = tdoc.Name.Substring(0, tdoc.Name.LastIndexOf('.'));
            try
            {
                DateTime dtStart = DateTime.Now;
                pdfReader = new PdfReader(fi.FullName);
                int numberOfPages = pdfReader.NumberOfPages;
                int i;
                for (i = 1; i <= numberOfPages; ++i)
                {
                    if (dtStart.AddMilliseconds(timeLimit) < DateTime.Now)
                    {
                        break;
                    }
                    iTextSharp.text.pdf.parser.ITextExtractionStrategy strategy = new iTextSharp.text.pdf.parser.SimpleTextExtractionStrategy();
                    strBuilder.Append(iTextSharp.text.pdf.parser.PdfTextExtractor.GetTextFromPage(pdfReader, i, strategy));
                }
                DateTime dtEnd = DateTime.Now;
                TimeSpan timeInter = dtEnd - dtStart;
                tdoc.Content = strBuilder.ToString();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            finally
            {
                if (pdfReader != null)
                {
                    pdfReader.Close();
                }
            }
            return tdoc;
        }
    }

    public class TxtProc : IDocProc
    {
        private int timeLimit = 10000;

        public TxtProc() {
        }

        public void SetTimeLimit(int milliSeconds)
        {
            timeLimit = milliSeconds;
        }

        public List<string> GetSupportExt()
        {
            List<string> list = new List<string>();
            list.Add(".txt");
            return list;
        }

        public TDocs Process(FileInfo fi)
        {
            StringBuilder strBuilder = new StringBuilder();

            TDocs tdoc = new TDocs();
            tdoc.Path = fi.FullName;
            tdoc.Name = fi.Name;
            tdoc.Extension = fi.Extension.ToLower();
            tdoc.Title = tdoc.Name.Substring(0, tdoc.Name.LastIndexOf('.'));
            FileStream fs = null;
            try
            {
                DateTime dtStart = DateTime.Now;
                fs = new FileStream(fi.FullName, FileMode.Open);
                byte[] buf = new byte[1048576];//1mb
                int byteRead = 0;
                do
                {
                    if (dtStart.AddMilliseconds(timeLimit) < DateTime.Now)
                    {
                         break;
                    }
                    byteRead = fs.Read(buf, 0, buf.Length);
                    string str = Encoding.Default.GetString(buf,0,byteRead);
                    strBuilder.Append(str);
                } while (byteRead > 0);
                DateTime dtEnd = DateTime.Now;
                TimeSpan timeInter = dtEnd - dtStart;
                tdoc.Content = strBuilder.ToString();
                fs.Close();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            finally
            {
                if (fs != null)
                {
                    fs.Close();
                }
            }
            return tdoc;
        }
    }
}
