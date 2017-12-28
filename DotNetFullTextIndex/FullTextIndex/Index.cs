using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Threading;
using Lucene.Net.Index;
using Lucene.Net.Documents;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Search;
using Lucene.Net.QueryParsers;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.PanGu;
using Word = Microsoft.Office.Interop.Word;
//using iTextSharp.text;
using iTextSharp.text.pdf;
using PanGu;
using Common;

namespace FullTextIndex
{
    public class FolderObj:Object
    {
        private string m_path;
        private bool m_recursive;

        public string Path
        {
            get
            {
                return m_path;
            }
            set
            {
                m_path = value;
            }
        }
        public bool Recursive
        {
            get
            {
                return m_recursive;
            }
            set
            {
                m_recursive = value;
            }
        } 
    }

    class Index
    {
        private IndexWriter writer = null;
        private int count=0;
        private long totalChars=0;
        //every maxcount will trigger indexWriter create
        private const int MaxCount = 10000;
        public Dictionary<string, bool> FolderDic;
        private string _indexDir;
        DocProcess docProc;

        private delegate void ProcessDocsDelegate(FileInfo fi);
        ProcessDocsDelegate procDocsDelg;
        private int asyncProcCtn;
        private static Mutex procMutex;
        public int MaxAsyncProc { get; set; }
        private int procWaitInterval = 1000; //milliseconds

        public String INDEX_DIR
        {
            get
            {
                return _indexDir;
            }
            set
            {
                _indexDir = value;
            }
        }
        /// <summary>
        /// indicate the number of docs/segments to merge into a new segment, default 10
        /// </summary>
        public int MergeFactor
        {
            get
            {
                if (writer != null)
                {
                    return writer.GetMergeFactor();
                }
                else
                {
                    return 0;
                }
            }

            set
            {
                if (writer != null)
                {
                    writer.SetMergeFactor(value);
                }
            }
        }
        /// <summary>
        /// MergeDocs indicate the max number of docs in one segment, default Integer.MAX_VALUE
        /// </summary>
        public int MaxMergeDocs
        {
            get
            {
                if (writer != null)
                {
                    return writer.GetMaxMergeDocs();
                }
                else
                {
                    return 0;
                }
            }

            set
            {
                if (writer != null)
                {
                    writer.SetMaxMergeDocs(value);
                }
            }
        }
        /// <summary>
        /// the buffered docs number, it won't affect the segment size on disk, default 10
        /// </summary>
        public int MaxBufferDocs
        {
            get
            {
                if (writer != null)
                {
                    return writer.GetMaxBufferedDocs();
                }
                else
                {
                    return 0;
                }
            }

            set
            {
                if (writer != null)
                {
                    writer.SetMaxBufferedDocs(value);
                }
            }
        }
        /// <summary>
        /// max term number of each field, exceeded part will be ignored
        /// </summary>
        public int MaxFieldLength {
            get {
                if (writer != null)
                {
                    return writer.GetMaxFieldLength();
                }
                else {
                    return 0;
                }
            }
            set {
                if (writer != null)
                {
                    writer.SetMaxFieldLength(value);
                }
            }
        }

        public Index() {
            FolderDic = new Dictionary<string, bool>();
            docProc = DocProcess.GetInstance();
            procDocsDelg = new ProcessDocsDelegate(ProcessDocsCallBack);
            MaxAsyncProc = 10;
            procMutex = new Mutex(false);
        }

        public void CreateIndex(bool create=true)
        {
            try
            {
                writer = new IndexWriter(INDEX_DIR, new PanGuAnalyzer(), create);
            }
            catch
            {
                writer = new IndexWriter(INDEX_DIR, new PanGuAnalyzer(), create);
            }
        }

        public int IndexFileContent(TDocs tdoc)
        {
            Document doc = new Document();
            Field field = new Field("path", tdoc.Path, Field.Store.YES, Field.Index.NO);
            doc.Add(field);
            field = new Field("name", tdoc.Name, Field.Store.YES, Field.Index.TOKENIZED);
            doc.Add(field);
            field = new Field("title",tdoc.Title, Field.Store.YES, Field.Index.TOKENIZED);
            doc.Add(field);
            field = new Field("ext", tdoc.Extension.ToLower(), Field.Store.YES, Field.Index.UN_TOKENIZED);
            doc.Add(field);
            field = new Field("content", tdoc.Content, Field.Store.YES, Field.Index.TOKENIZED);
            doc.Add(field);

            writer.AddDocument(doc);

            int num = writer.DocCount();
            return num;
        }

        public List<string> SplitKeyWords(string keywords, Analyzer analyzer)
        {
            System.IO.StreamReader reader = new System.IO.StreamReader(PanGu.Framework.Stream.WriteStringToStream(keywords,
                Encoding.UTF8), Encoding.UTF8);

            TokenStream tokenStream = analyzer.TokenStream("", reader);

            global::Lucene.Net.Analysis.Token token = tokenStream.Next();

            List<string> result = new List<string>();

            while (token != null)
            {
                result.Add(keywords.Substring(token.StartOffset(), token.EndOffset() - token.StartOffset()));
                token = tokenStream.Next();
            }

            return result;

        }

        public string GetKeyWordsSplitBySpace(string keywords, PanGuTokenizer ktTokenizer)
        {
            StringBuilder result = new StringBuilder();

            ICollection<WordInfo> words = ktTokenizer.SegmentToWordInfos(keywords);

            foreach (WordInfo word in words)
            {
                if (word == null)
                {
                    continue;
                }

                result.AppendFormat("{0}^{1}.0 ", word.Word, (int)Math.Pow(3, word.Rank));
            }

            return result.ToString().Trim();
        }

        /// <summary>
        /// search 
        /// </summary>
        /// <param name="indexDir"></param>
        /// <param name="q">keyword</param>
        /// <returns></returns>
        public List<TDocs> Search( String q)
        {
            int recCount;
            return Search(q, 100, 1, out recCount);
        }

        /// <summary>
        /// search 
        /// </summary>
        /// <param name="indexDir"></param>
        /// <param name="q">keyword</param>
        /// <param name="pageLen">every page's length</param>
        /// <param name="pageNo">page number</param>
        /// <param name="recCount">result number</param>
        /// <returns></returns>
        public List<TDocs> Search(String q, int pageLen, int pageNo, out int recCount)
        {
            string keywords = q;
            IndexSearcher search = new IndexSearcher(INDEX_DIR);
            q = GetKeyWordsSplitBySpace(q, new PanGuTokenizer());
            QueryParser queryParser = new QueryParser("content", new PanGuAnalyzer(true));

            Query query = queryParser.Parse(q);

            Hits hits = search.Search(query,Sort.RELEVANCE);

            List<TDocs> result = new List<TDocs>();

            recCount = hits.Length();
            int i = (pageNo - 1) * pageLen;

            while (i < recCount && result.Count < pageLen)
            {
                TDocs docs = null;

                try
                {
                    docs = new TDocs();
                    docs.Path = hits.Doc(i).Get("path");
                    docs.Name = hits.Doc(i).Get("name");
                    docs.Title = hits.Doc(i).Get("title");
                    docs.Extension = hits.Doc(i).Get("ext");
                    //rem this item in case the search result will be too large & consume too much memory, 
                    //   takes loading time, abstract is enough
                    //docs.Content = hits.Doc(i).Get("content");

                    PanGu.HighLight.SimpleHTMLFormatter simpleHTMLFormatter =
                        new PanGu.HighLight.SimpleHTMLFormatter("<color=red>", "</color>");

                    PanGu.HighLight.Highlighter highlighter =
                        new PanGu.HighLight.Highlighter(simpleHTMLFormatter,
                        new Segment());

                    highlighter.FragmentSize = 100;
                    docs.Abstract = highlighter.GetBestFragment(keywords, hits.Doc(i).Get("content"));
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                }
                finally
                {
                    result.Add(docs);
                    i++;
                }
            }
            search.Close();
            return result;
        }

        private void ProcessDocsCallBack(object fi) {
            procMutex.WaitOne();
            asyncProcCtn++;
            procMutex.ReleaseMutex();

            TDocs tdoc = docProc.DealWithDoc((FileInfo)fi);
            if (tdoc == null)
            {
                procMutex.WaitOne();
                asyncProcCtn--;
                procMutex.ReleaseMutex();
                return; 
            }
            DateTime dtStart = DateTime.Now;

            procMutex.WaitOne();
            IndexFileContent(tdoc);
            DateTime dtEnd = DateTime.Now;
            TimeSpan time = dtEnd - dtStart;
            totalChars += tdoc.Content.Length;
            count++;
            if (count >= MaxCount)
            {
                asyncProcCtn--;
                return;
            }
            /*
            if (count % 10 == 0)
            {
                writer.Close();
                CreateIndex(false);
                MergeFactor = 10;
                MaxBufferDocs = 1000;
                MaxMergeDocs = 10000;
                MaxFieldLength = 100000;
            }
             * */
            asyncProcCtn--;
            procMutex.ReleaseMutex();
        }

        private void buildIndexOfSubFiles(DirectoryInfo dir) {
            FileInfo[] fiArr = dir.GetFiles();
            foreach (FileInfo fi in fiArr)
            {
                while (true) { 
                    if (asyncProcCtn < MaxAsyncProc) {
                        break;
                    }
                    Thread.Sleep(procWaitInterval);
                }
                
                procDocsDelg.BeginInvoke(fi, null, null);

                //Thread procTh = new Thread(new ParameterizedThreadStart(ProcessDocsCallBack));
                //procTh.Start(fi);
                
            }
            
            while (true)
            {
                if (asyncProcCtn <= 0)
                {
                    break;
                }
                Thread.Sleep(procWaitInterval);
            }
             
        }

        private void buildIndexOfSubDir(DirectoryInfo dir) { 
            DirectoryInfo[] dirs = dir.GetDirectories();
            foreach (DirectoryInfo subdir in dirs) {
                if (subdir.Name == "." || subdir.Name == ".." || subdir.Name == "RECYCLER" || subdir.Name == "RECYCLED" || subdir.Name == "Recycled" || subdir.Name == "System Volume Information")
                {
                    continue;
                }
                buildIndexOfSubDir(subdir);
            }
            buildIndexOfSubFiles(dir);
        }

        public long[] BuildIndex(bool rebuild)
        {
            long[] ret = new long[2];
            if (FolderDic.Count == 0)
            {
                return ret;
            }
            if ((bool)rebuild)
            {
                writer = new IndexWriter(INDEX_DIR, new PanGuAnalyzer(), true);
            }
            else
            {
                CreateIndex();
            }
            count = 0;
            totalChars = 0;
            MergeFactor = 10;
            MaxBufferDocs = 1000;
            MaxMergeDocs = 10000;
            MaxFieldLength = 100000;
            asyncProcCtn = 0;

            foreach (string path in FolderDic.Keys)
            {
                try
                {
                    DirectoryInfo dir = new DirectoryInfo(path);
                    if (!dir.Exists) continue;
                    if (FolderDic[path])
                    {
                        buildIndexOfSubDir(dir);
                    }
                    else
                    {
                        buildIndexOfSubFiles(dir);
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                }
            }
            writer.Optimize();
            writer.Close();
            ret[0] = count;
            ret[1] = totalChars;
            return ret;
        }

        public void UpdateIndex()
        {

        }

    }
}
