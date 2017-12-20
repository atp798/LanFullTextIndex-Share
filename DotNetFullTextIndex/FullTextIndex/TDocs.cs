using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FullTextIndex
{
    public class TDocs
    {
        private String m_Path;//full path
        private String m_Name;//full file name
        private String m_Title;//without extension or the actual file title
        private String m_Extension;
        private String m_Content;
        private String m_Abstract;

        public String Path {
            get {
                return m_Path;
            }
            set {
                m_Path = value;
            }
        }
        public String Name
        {
            get
            {
                return m_Name;
            }
            set
            {
                m_Name = value;
            }
        }
        public String Title
        {
            get
            {
                return m_Title;
            }
            set
            {
                m_Title = value;
            }
        }
        public String Extension
        {
            get
            {
                return m_Extension;
            }
            set
            {
                m_Extension = value;
            }
        }
        public String Content
        {
            get
            {
                return m_Content;
            }
            set
            {
                m_Content = value;
            }
        }
        public String Abstract
        {
            get
            {
                return m_Abstract;
            }
            set
            {
                m_Abstract = value;
            }
        }
    }
}
