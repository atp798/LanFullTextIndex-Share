using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace FullTextIndex
{
    public interface IDocProc
    {
        TDocs Process(FileInfo fi);
    }

    public class WordProc : IDocProc {
        public WordProc() { 
        
        }

        public TDocs Process(FileInfo fi) {
            return null;
        }
    }
}
