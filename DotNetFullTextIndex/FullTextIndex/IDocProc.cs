using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Word = Microsoft.Office.Interop.Word;
using iTextSharp.text.pdf;
using System.Diagnostics;

namespace FullTextIndex
{
    public interface IDocProc
    {
        void SetTimeLimit(int milliSeconds);
        List<string> GetSupportExt();
        TDocs Process(FileInfo fi);
    }

    public class WordProc : IDocProc
    {
        private int timeLimit = 100000;
        //default max value is 2147483647 = 2^31-1
        private StringBuilder strBuilder;

        private Word.Application app = null;
        private bool appOpened = false;
        private static int MaxTitle { get { return 30; } }

        public WordProc() {
            strBuilder = new StringBuilder();
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
        }

        ~WordProc() { 
            if(appOpened)
                ((Microsoft.Office.Interop.Word._Application)app).Quit();
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

        public TDocs Process(FileInfo fi) {
            if (!appOpened)
                return null;
            if (fi.Name.StartsWith("~$"))
            {
                return null;
            }
            Word.Document doc = null;
            object unknow = Type.Missing;
            DateTime dtStart = DateTime.Now;
            strBuilder.Clear();

            TDocs tdoc = new TDocs();
            tdoc.Path = fi.FullName;
            tdoc.Name = fi.Name;
            tdoc.Extension = fi.Extension;
            tdoc.Title = tdoc.Name.Substring(0, tdoc.Name.LastIndexOf('.'));

            try
            {
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
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            finally
            {
                if (doc != null)
                {
                    ((Microsoft.Office.Interop.Word._Document)doc).Close(ref unknow, ref unknow, ref unknow);
                }
            }
            return tdoc;
        }
    }

    public class PdfProc : IDocProc {
        private int timeLimit = 20000;
        private StringBuilder strBuilder;

        public PdfProc() {
            strBuilder = new StringBuilder();
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
            PdfReader pdfReader = null;
            DateTime dtStart = DateTime.Now;
            strBuilder.Clear();

            TDocs tdoc = new TDocs();
            tdoc.Path = fi.FullName;
            tdoc.Name = fi.Name;
            tdoc.Extension = fi.Extension;
            tdoc.Title = tdoc.Name.Substring(0, tdoc.Name.LastIndexOf('.'));
            try
            {
                pdfReader = new PdfReader(fi.FullName);
                int numberOfPages = pdfReader.NumberOfPages;
                for (int i = 1; i <= numberOfPages; ++i)
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
                //StreamWriter wlog = File.AppendText(System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase + "\\mylog.log");
                //wlog.Flush();
                //wlog.Close(); 
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
        private StringBuilder strBuilder;

        public TxtProc() {
            strBuilder = new StringBuilder();
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

        public TDocs Process(FileInfo fi) {
            DateTime dtStart = DateTime.Now;
            strBuilder.Clear();

            TDocs tdoc = new TDocs();
            tdoc.Path = fi.FullName;
            tdoc.Name = fi.Name;
            tdoc.Extension = fi.Extension;
            tdoc.Title = tdoc.Name.Substring(0, tdoc.Name.LastIndexOf('.'));
            FileStream fs = null;
            try
            {

                fs = new FileStream(fi.FullName, FileMode.Open);
                byte[] buf = new byte[1048576];//1mb
                while (fs.Read(buf, 0, buf.Length) > 0)
                {
                    if (dtStart.AddMilliseconds(timeLimit) < DateTime.Now)
                    {
                        break;
                    }
                    strBuilder.Append(Encoding.Default.GetString(buf));
                }
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
