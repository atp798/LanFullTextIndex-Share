using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FullTextIndex;
using Common;

namespace DotNetFullTextIndex
{
    public partial class ResultItemCon : UserControl
    {
        public event EventHandler<MessageEventArgs> OpenFile;
        public ClientBrief TargetClient { get; set; }

        private TDocs tdoc;
        private string iconHTML;
        private bool linkOpen;

        public ResultItemCon()
        {
            tdoc = null;
            InitializeComponent();
        }

        public ResultItemCon(TDocs indoc)
            : this()
        {
            InitCon(indoc);
        }

        public ResultItemCon(TDocs indoc, ClientBrief target)
            : this(indoc)
        {
            InitCon(indoc,target);
        }

        public void InitCon(TDocs indoc)
        {
            InitCon(indoc,new ClientBrief(null, GlobalVar.LocalPath));
        }

        public void InitCon(TDocs indoc,ClientBrief target )
        {
            if (indoc == null)
                return;
            tdoc = indoc;
            TargetClient = target;
            updateCon();
        }

        private void updateCon() {
            labelTitle.ImageAlignToText = DevExpress.XtraEditors.ImageAlignToText.LeftCenter;
            if (tdoc.Extension.ToLower().Contains("txt"))
            {
                iconHTML = "<image=TXT.png>";
            }
            else if (tdoc.Extension.ToLower().Contains("doc") || tdoc.Extension.ToLower().Contains("docx"))
            {
                iconHTML = "<image=MSWord.png>";
            }
            else if (tdoc.Extension.ToLower().Contains("xls") || tdoc.Extension.ToLower().Contains("xlsx"))
            {
                iconHTML = "<image=MSExcel.png>";
            }
            else if (tdoc.Extension.ToLower().Contains("ppt") || tdoc.Extension.ToLower().Contains("pptx"))
            {
                iconHTML = "<image=MSPPT.png>";
            }
            else if (tdoc.Extension.ToLower().Contains("pdf"))
            {
                iconHTML = "<image=PDF.gif>";
            }
            labelTitle.Text = iconHTML + "<color=#0000FF><u>" + tdoc.Title + "</u></color>";   
            labelAbstract.Text = tdoc.Abstract;
            labelTitle.ToolTip = "文档路径:"+ TargetClient.Name + "\\\\" + tdoc.Path;
            linkOpen = false;
            
        }

        private void labelTitle_Click(object sender, EventArgs e)
        {
            labelTitle.Text = iconHTML + "<color=#FF0000><u>" + tdoc.Title + "</u></color>";
            try
            {
                MessageEventArgs arg = new MessageEventArgs();
                arg.MessageObj = new object[2];
                arg.MessageObj[0] = TargetClient;
                arg.MessageObj[1] = tdoc.Path;
                if (OpenFile != null) {
                    OpenFile(this, arg);
                }
            }catch(Exception excpt){
                MessageBox.Show("打开文件错误:" + excpt.Message, "错误",MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            linkOpen = true;
        }

        private void labelTitle_MouseHover(object sender, EventArgs e)
        {
            labelTitle.Text = iconHTML + "<color=#00008B><u>" + tdoc.Title + "</u></color>";
            this.Cursor = Cursors.Hand;
        }

        private void labelTitle_MouseLeave(object sender, EventArgs e)
        {
            labelTitle.Text = iconHTML + String.Format("<color={0}><u>{1}</u></color>", linkOpen ? "#8A2BE2" : "#0000FF", tdoc.Title);
            this.Cursor = Cursors.Default;
        }

    }
}
