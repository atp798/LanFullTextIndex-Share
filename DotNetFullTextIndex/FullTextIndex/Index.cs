using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
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
        }

        public void CreateIndex()
        {
            try
            {
                writer = new IndexWriter(INDEX_DIR, new PanGuAnalyzer(), true);
            }
            catch
            {
                writer = new IndexWriter(INDEX_DIR, new PanGuAnalyzer(), false);
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
            field = new Field("ext", tdoc.Extension, Field.Store.YES, Field.Index.UN_TOKENIZED);
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

        private void buildIndexOfSubFiles(DirectoryInfo dir) {
            FileInfo[] fi = dir.GetFiles();
            //default max value is 2147483647 = 2^31-1
            StringBuilder strBuilder = new StringBuilder();
            DateTime dtstart;
            try
            {
                bool wordopen = true;
                Word.Application app = null;
                Word.Document doc = null;
                object unknow = Type.Missing;
                try
                {
                    app = new Microsoft.Office.Interop.Word.Application();
                }
                catch (Exception ex)
                {
                    wordopen = false;
                    Debug.Write(ex.Message);
                }

                foreach (FileInfo f in fi)
                {
                    dtstart = DateTime.Now;
                    strBuilder.Clear();
                    String filepath = f.FullName;
                    String filename = f.Name;
                    TDocs tdoc = new TDocs();
                    tdoc.Path = filepath;
                    tdoc.Name = filename;
                    tdoc.Extension = f.Extension;
                    if (f.Extension == ".txt")
                    {
                        FileStream fs = new FileStream(f.FullName, FileMode.Open);
                        byte[] buf = new byte[1048576];//1mb
                        while (fs.Read(buf, 0, buf.Length) > 0)
                        {
                            strBuilder.Append(Encoding.Default.GetString(buf));
                        }
                        totalChars += strBuilder.Length;
                        tdoc.Title = tdoc.Name.Substring(0, tdoc.Name.LastIndexOf('.'));
                        tdoc.Content = strBuilder.ToString();
                        fs.Close();
                    }
                    else if (f.Extension == ".doc" || f.Extension == ".docx")
                    {
                        if (tdoc.Name.Substring(0, tdoc.Name.LastIndexOf('.')).StartsWith("~$") || !wordopen)
                        {
                            continue;
                        }
                        try
                        {
                            object conf = false;
                            app.Visible = false;
                            object file = f.FullName;
                            doc = app.Documents.Open(ref file,
                                ref conf, ref unknow, ref unknow, ref unknow,
                                ref unknow, ref unknow, ref unknow, ref unknow,
                                ref unknow, ref unknow, ref unknow, ref unknow,
                                ref unknow, ref unknow, ref unknow);
                            int i = 0;
                            //notice that: the index of doc.Paragraphs counts from 1~Count, not starts with 0
                            for (i = 2; i <= doc.Paragraphs.Count; i++)
                            {
                                if (dtstart.AddSeconds(30) < DateTime.Now) {
                                    break;
                                }
                                string temp = doc.Paragraphs[i].Range.Text.Trim();
                                strBuilder.AppendLine(temp);
                            }
                            string titletmp = doc.Paragraphs[1].Range.Text.Trim();
                            tdoc.Title = titletmp==""?tdoc.Name.Substring(0, tdoc.Name.LastIndexOf('.')):titletmp;
                            tdoc.Content = strBuilder.ToString();
                            totalChars += strBuilder.Length;
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
                    }
                    else if (f.Extension == ".pdf")
                    {
                        PdfReader pdfReader = null;
                        try
                        {
                            pdfReader = new PdfReader(f.FullName);
                            int numberOfPages = pdfReader.NumberOfPages;
                            for (int i = 1; i <= numberOfPages; ++i)
                            {
                                if (dtstart.AddSeconds(20) < DateTime.Now)
                                {
                                    break;
                                }
                                iTextSharp.text.pdf.parser.ITextExtractionStrategy strategy = new iTextSharp.text.pdf.parser.SimpleTextExtractionStrategy();
                                strBuilder.Append(iTextSharp.text.pdf.parser.PdfTextExtractor.GetTextFromPage(pdfReader, i, strategy));
                            }
                            tdoc.Title = tdoc.Name.Substring(0, tdoc.Name.LastIndexOf('.'));
                            tdoc.Content = strBuilder.ToString();
                            totalChars += strBuilder.Length;
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
                    }
                    if (strBuilder.Length > 0)
                    {
                        IndexFileContent(tdoc);
                        count++;
                        if (count >= MaxCount)
                        {
                            break;
                        }
                        if (count % 1000 == 0)
                        {
                            writer.Close();
                            CreateIndex();
                            MergeFactor = 10;
                            MaxBufferDocs = 1000;
                            MaxMergeDocs = 10000;
                            MaxFieldLength = 100000;
                        }
                    }
                }
                ((Microsoft.Office.Interop.Word._Application)app).Quit();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
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

            MergeFactor = 10;
            MaxBufferDocs = 1000;
            MaxMergeDocs = 10000;
            MaxFieldLength = 100000;

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
