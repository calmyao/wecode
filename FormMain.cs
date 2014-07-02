﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Data.OleDb;
using System.IO;
using System.Configuration;
using ScintillaNET;
using WeifenLuo.WinFormsUI.Docking;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Web;
using System.Xml;

namespace WeCode1._0
{
    public partial class FormMain : Form
    {
        private FormTreeLeft frTree;
        private YouDaoTree frYoudaoTree;

        private FormAttachment frmAttchment;
        private DocMark frmMark;
        private DocFind FormFind;

        private DeserializeDockContent m_deserializeDockContent;


        #region Fields

        private int _newDocumentCount = 0;
        private string[] _args;
        private int _zoomLevel=0;
        private const int LINE_NUMBERS_MARGIN_WIDTH = 35;

        #endregion Fields

        #region Properties

        public DocumentForm ActiveDocument
        {
            get
            {
                return dockPanel1.ActiveDocument as DocumentForm;
            }
        }

        #endregion Properties


        public FormMain()
        {
            InitializeComponent();

            //书签
            frmMark = new DocMark();
            frmMark.formParent = this;
            //frmMark.Show(dockPanel1);

            //显示查找
            FormFind = new DocFind();
            FormFind.formParent = this;
            //FormFind.Show(dockPanel1);

            //显示树窗口
            frTree = new FormTreeLeft();
            frTree.formParent = this;
            //frTree.Show(dockPanel1);

            //显示有道树窗口
            frYoudaoTree = new YouDaoTree();
            frYoudaoTree.formParent = this;
            //frYoudaoTree.Show(dockPanel1);

            //显示附件窗口
            Attachment.ActiveNodeId = "-1";
            frmAttchment = new FormAttachment();
            Attachment.AttForm = frmAttchment;
            //frmAttchment.Show(dockPanel1);

            m_deserializeDockContent = new DeserializeDockContent(GetContentFromPersistString);

            //第一次打开界面，先判断token是否有效（为空或者过期）
            //有效，同步XML到本地，加载有道云树目录;无效，打开授权页面进行授权
            //授权成功后，云端创建两个目录以及配置文件，然后加载树目录
            IniYouDaoAuthor();
            

        }

        //初始化授权相关
        public void IniYouDaoAuthor()
        {
            //判断token是否有效
            string IsAuthor = AuthorAPI.GetIsAuthor();
            if (IsAuthor != "OK")
            {
                //禁用云目录
                this.toolStripMenuItemLogin.Visible = true;
                this.toolStripMenuItemUinfo.Visible = false;
                Text = "WeCode--未登录";

                Attachment.IsTokeneffective = 0;
                this.Load += new System.EventHandler(this.showNoAuthor);
            }
            else
            {
                //从云端拉取XML同步到本地
                XMLAPI.Yun2XML();
                Attachment.IsTokeneffective = 1;
                //获取用户信息并禁用登陆按钮
                this.toolStripMenuItemLogin.Visible = false;
                this.toolStripMenuItemUinfo.Visible = true;
                Text = "WeCode--已登录";
            }
            
        }

        private void showNoAuthor(object sender, System.EventArgs e)
        {
            //MessageBox.Show("未授权有道云笔记或者授权已过期，请点击用户--登录以重新授权！");
            if (ConfigurationManager.AppSettings["authorAlert"] != "0")
            {
                if (MessageBox.Show("未授权有道云笔记或者授权已过期，请点击菜单用户--登录以重新授权！\n点击“确定”不再提醒", "登录提醒", MessageBoxButtons.OKCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == DialogResult.OK)
                {
                    PubFunc.SetConfiguration("authorAlert", "0");
                }
            }

        }
        
        //上移
        private void toolStripButtonUp_Click(object sender, EventArgs e)
        {
            if(dockPanel1.ActiveContent.GetType()==typeof(FormTreeLeft))
            {
                frTree.setNodeUp();
            }
            else if (dockPanel1.ActiveContent.GetType() == typeof(YouDaoTree))
            {
                frYoudaoTree.setNodeUp();
            }
            else
            {
                return;
            }
        }

        //下移
        private void toolStripButtonDown_Click(object sender, EventArgs e)
        {
            if (dockPanel1.ActiveContent.GetType() == typeof(FormTreeLeft))
            {
                frTree.setNodeDown();
            }
            else if (dockPanel1.ActiveContent.GetType() == typeof(YouDaoTree))
            {
                frYoudaoTree.setNodeDown();
            }
            else
            {
                return;
            }
        }

        //关闭文章
        public void CloseDoc(string nodeId)
        {
            if (Attachment.isWelcomePageopen == "0")
            {
                foreach (DocumentForm documentForm in dockPanel1.Documents)
                {
                    if (nodeId.Equals(documentForm.NodeId, StringComparison.OrdinalIgnoreCase))
                    {
                        documentForm.Close();
                        break;
                    }
                }
            }
        }

        //打开文章
        public void openNew(string nodeId)
        {
            //欢迎窗口是否打开，如果打开则关闭
            if (Attachment.isWelcomePageopen == "1")
            {
                IDockContent[] documents = dockPanel1.DocumentsToArray();

                foreach (IDockContent content in documents)
                {
                        content.DockHandler.Close();
                }
                Attachment.isWelcomePageopen = "0";
            }
            // 如果已经打开，则定位，否则新窗口打开
            bool isOpen = false;
            foreach (DocumentForm documentForm in dockPanel1.Documents)
            {
                if (nodeId.Equals(documentForm.NodeId, StringComparison.OrdinalIgnoreCase))
                {
                    documentForm.Select();
                    isOpen = true;
                    break;
                }
            }

            // Open the files
            if (!isOpen)
                OpenFile(nodeId);
        }

        //打开云笔记
        public void openNewYouDao(string nodeId,string title)
        {
            //欢迎窗口是否打开，如果打开则关闭
            if (Attachment.isWelcomePageopen == "1")
            {
                IDockContent[] documents = dockPanel1.DocumentsToArray();

                foreach (IDockContent content in documents)
                {
                    content.DockHandler.Close();
                }
                Attachment.isWelcomePageopen = "0";
            }
            // 如果已经打开，则定位，否则新窗口打开
            bool isOpen = false;
            foreach (DocumentForm documentForm in dockPanel1.Documents)
            {
                if (nodeId.Equals(documentForm.NodeId, StringComparison.OrdinalIgnoreCase))
                {
                    documentForm.Select();
                    isOpen = true;
                    break;
                }
            }

            // Open the files
            if (!isOpen)
                OpenFileYouDao(nodeId,title);
        }

        private DocumentForm OpenFileYouDao(string nodeId,string title)
        {

            //获取文章信息

            string Content = NoteAPI.GetNote(nodeId);

            DocumentForm doc = new DocumentForm();
            SetScintillaToCurrentOptions(doc);
            doc.Scintilla.Text = Content;
            doc.Scintilla.UndoRedo.EmptyUndoBuffer();
            doc.Scintilla.Modified = false;
            doc.Text = title;
            doc.NodeId = nodeId;
            doc.Type = "online";
            doc.Show(dockPanel1);


            return doc;
        }


        ////打开文章
        //public void openNew(string nodeId)
        //{

        //        // 如果已经打开，则定位，否则新窗口打开
        //        bool isOpen = false;
        //        foreach (DocumentForm documentForm in dockPanel1.Documents)
        //        {
        //            if (nodeId.Equals(documentForm.NodeId, StringComparison.OrdinalIgnoreCase))
        //            {
        //                documentForm.Select();
        //                isOpen = true;
        //                break;
        //            }
        //        }

        //        // Open the files
        //        if (!isOpen)
        //            OpenFile(nodeId);
        //}


        private DocumentForm OpenFile(string nodeId)
        {

            //获取文章信息
            string SQL = "select Title,Content from TContent inner join TTree on TContent.NodeId=Ttree.NodeId where TContent.NodeId=" + nodeId;
            DataTable temp = AccessAdo.ExecuteDataSet(SQL, null).Tables[0];
            if (temp.Rows.Count == 0)
                return null;
            string Title = temp.Rows[0]["Title"].ToString();
            string Content = temp.Rows[0]["Content"].ToString();

            DocumentForm doc = new DocumentForm();
            SetScintillaToCurrentOptions(doc);
            doc.Scintilla.Text = Content;
            doc.Scintilla.UndoRedo.EmptyUndoBuffer();
            doc.Scintilla.Modified = false;
            doc.Text = Title;
            doc.NodeId = nodeId;
            doc.Type = "local";
            doc.Show(dockPanel1);
            

            return doc;
        }

        //配置相关显示参数
        private void SetScintillaToCurrentOptions(DocumentForm doc)
        {
            //// Turn on line numbers?
            //if (lineNumbersToolStripMenuItem.Checked)
                doc.Scintilla.Margins.Margin0.Width = LINE_NUMBERS_MARGIN_WIDTH;
            //else
            //    doc.Scintilla.Margins.Margin0.Width = 0;

            //// Turn on white space?
            //if (whitespaceToolStripMenuItem.Checked)
            //    doc.Scintilla.Whitespace.Mode = WhitespaceMode.VisibleAlways;
            //else
            //    doc.Scintilla.Whitespace.Mode = WhitespaceMode.Invisible;

            //// Turn on word wrap?
            //if (wordWrapToolStripMenuItem.Checked)
            //    doc.Scintilla.LineWrapping.Mode = LineWrappingMode.Word;
            //else
            //    doc.Scintilla.LineWrapping.Mode = LineWrappingMode.None;

            //// Show EOL?
            //doc.Scintilla.EndOfLine.IsVisible = endOfLineToolStripMenuItem.Checked;

            // Set the zoom
            doc.Scintilla.ZoomFactor = _zoomLevel;
        }


        private void toolStripButtonNewText_Click(object sender, EventArgs e)
        {
            if (dockPanel1.ActiveContent.GetType() == typeof(FormTreeLeft))
            {
                frTree.NewDoc();
            }
            else if (dockPanel1.ActiveContent.GetType() == typeof(YouDaoTree))
            {
                frYoudaoTree.NewDoc();
            }
        }

        private void toolStripButtonNewDir_Click(object sender, EventArgs e)
        {
            if (dockPanel1.ActiveContent.GetType() == typeof(FormTreeLeft))
            {
                frTree.NewDir();
            }
            else if (dockPanel1.ActiveContent.GetType() == typeof(YouDaoTree))
            {
                frYoudaoTree.NewDir();
            }
        }

        //保存
        private void toolStripButtonSv_Click(object sender, EventArgs e)
        {
            if (ActiveDocument != null)
                ActiveDocument.Save();
        }


        //设置语言(激活文档)
        public void SetLanguage(string language)
        {
            if ("ini".Equals(language, StringComparison.OrdinalIgnoreCase))
            {
                // Reset/set all styles and prepare _scintilla for custom lexing
                ActiveDocument.IniLexer = true;
                IniLexer.Init(ActiveDocument.Scintilla);
            }
            else
            {
                // Use a built-in lexer and configuration
                ActiveDocument.IniLexer = false;
                ActiveDocument.Scintilla.ConfigurationManager.Language = language;

                // Smart indenting...
                if ("cs".Equals(language, StringComparison.OrdinalIgnoreCase))
                    ActiveDocument.Scintilla.Indentation.SmartIndentType = ScintillaNET.SmartIndent.CPP;
                else
                    ActiveDocument.Scintilla.Indentation.SmartIndentType = ScintillaNET.SmartIndent.None;
            }
        }

        //设置语言
        public void SetLanguageByDoc(string language,string id)
        {
            //根据id设置语言
            if (Attachment.isWelcomePageopen == "0")
            {
                foreach (DocumentForm documentForm in dockPanel1.Documents)
                {
                    if (id.Equals(documentForm.NodeId, StringComparison.OrdinalIgnoreCase))
                    {
                        documentForm.SetLanguageByDoc(language);
                        break;
                    }
                }
            }
        }
        
        //保存所有
        private void toolStripButtonSvAll_Click(object sender, EventArgs e)
        {
            if (Attachment.isWelcomePageopen == "1")
            {
                return;
            }
            foreach (DocumentForm doc in dockPanel1.Documents)
            {
                doc.Activate();
                doc.Save();
            }
        }

        //新建数据库
        private void toolStripMenuItemNewDB_Click(object sender, EventArgs e)
        {
            SaveFileDialog sf = new SaveFileDialog();
            string path = "";
            //设置文件类型
            sf.Filter = "数据文件(*.mdb)|*.mdb";
            if (sf.ShowDialog() == DialogResult.OK)
            {
                path = sf.FileName;

                if (File.Exists(path)) //检查数据库是否已存在
                {
                    throw new Exception("目标数据库已存在,无法创建");
                }
                // 可以加上密码,这样创建后的数据库必须输入密码后才能打开
                path = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + path;
                // 创建一个CatalogClass对象的实例,
                ADOX.CatalogClass cat = new ADOX.CatalogClass();
                // 使用CatalogClass对象的Create方法创建ACCESS数据库
                cat.Create(path);

                //创建表
                OleDbConnection conn = new OleDbConnection(path);
                string crtSQL=" CREATE TABLE TTree ( "+
				" [NodeId] INTEGER CONSTRAINT PK_TTree26 PRIMARY KEY, "+
				" [Title] VARCHAR, "+
				" [ParentId] INTEGER, "+
				" [Type] INTEGER, "+
				" [CreateTime] INTEGER, "+
				" [SynId] INTEGER, "+
				" [Turn] INTEGER,  "+
				" [MarkTime] INTEGER) ";
                AccessAdo.ExecuteNonQuery(conn, crtSQL);

                crtSQL=" CREATE TABLE TContent ( "+
				" [NodeId] INTEGER CONSTRAINT PK_TTree27 PRIMARY KEY, "+
				" [Content] MEMO, "+
				" [Note] MEMO, "+
				" [Link] MEMO) ";
                AccessAdo.ExecuteNonQuery(conn, crtSQL);

                crtSQL = " CREATE TABLE TAttachment ( " +
                " [AffixId] INTEGER CONSTRAINT PK_TTree28 PRIMARY KEY, " +
                " [NodeId] INTEGER, " +
                " [Title] VARCHAR, " +
                " [Data] IMAGE , " +
                " [Size] INTEGER, " +
                " [Time] INTEGER)";
                AccessAdo.ExecuteNonQuery(conn, crtSQL);

            }

        }

        private bool closeAll()
        {
            //关闭所有打开的文档
                string IsDocModi = "false";
                if (Attachment.isWelcomePageopen == "0")
                {
                    foreach (DocumentForm doc in dockPanel1.Documents)
                    {
                        if (doc.Scintilla.Modified)
                            IsDocModi = "true";
                    }
                }

                if (IsDocModi == "true")
                {
                    DialogResult dr = MessageBox.Show(this, "是否保存所有文档?", "提示", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                    if (dr == DialogResult.Cancel)
                    {
                        return false;
                    }
                    else if (dr == DialogResult.No)
                    {
                        CloseAllDoment();
                        return true;
                    }
                    else
                    {
                        foreach (DocumentForm doc in dockPanel1.Documents)
                        {
                            doc.Save();
                        }
                        CloseAllDoment();
                        return true;
                    }
                }

                else
                {
                    CloseAllDoment();
                    return true;
                }
        }

        //关闭所有文档
        private void CloseAllDoment()
        {
            if (Attachment.isWelcomePageopen == "0")
            {
                IDockContent[] documents = dockPanel1.DocumentsToArray();

                foreach (IDockContent content in documents)
                {
                    content.DockHandler.Close();
                }
                
            }
        }

        //打开数据库
        private void toolStripMenuItemOpenDB_Click(object sender, EventArgs e)
        {
           
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            //openFileDialog1.InitialDirectory = "c:\\";
            openFileDialog1.Filter = "数据文件(*.mdb)|*.mdb";
            openFileDialog1.RestoreDirectory = false;

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                //关闭所有打开的文档
                if (closeAll() == false)
                    return;
                //刷新附件列表数据
                if (Attachment.isWelcomePageopen == "0")
                {
                    openWelcomePage();
                }
                Attachment.ActiveNodeId = "-1";
                Attachment.AttForm.ReFreshAttachGrid();


                string fileName = openFileDialog1.FileName;
               //修改连接字符串，并重新加载
                string conStr = "Provider=Microsoft.Jet.OLEDB.4.0; Data Source=" + fileName;
                UpdateConnectionStringsConfig("DBConn",conStr);
                
                //重新加载所有资源
                AccessAdo.strConnection = conStr;
                

                frTree.frmTree_Reload();
                ReSetMarkFind();
            }
        }

        public void ReSetMarkFind()
        {
            //清空搜索重新加载书签
            FormFind.IniData();
            frmMark.RefreshGrid("local");
        }


        ///<summary> 
        ///更新连接字符串  
        ///</summary> 
        ///<param name="newName">连接字符串名称</param> 
        ///<param name="newConString">连接字符串内容</param> 
        private static void UpdateConnectionStringsConfig(string newName,
            string newConString)
        {
            bool isModified = false;    //记录该连接串是否已经存在  
            //如果要更改的连接串已经存在  
            if (ConfigurationManager.ConnectionStrings[newName] != null)
            {
                isModified = true;
            }
            //新建一个连接字符串实例  
            ConnectionStringSettings mySettings =
                new ConnectionStringSettings(newName, newConString);
            // 打开可执行的配置文件*.exe.config  
            Configuration config =
                ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            // 如果连接串已存在，首先删除它  
            if (isModified)
            {
                config.ConnectionStrings.ConnectionStrings.Remove(newName);
            }
            // 将新的连接串添加到配置文件中.  
            config.ConnectionStrings.ConnectionStrings.Add(mySettings);
            // 保存对配置文件所作的更改  
            config.Save(ConfigurationSaveMode.Modified);
            // 强制重新载入配置文件的ConnectionStrings配置节  
            ConfigurationManager.RefreshSection("ConnectionStrings");
        }

        //压缩数据库
        private void toolStripMenuItemZipDB_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            //openFileDialog1.InitialDirectory = "c:\\";
            openFileDialog1.Filter = "数据文件(*.mdb)|*.mdb";
            openFileDialog1.RestoreDirectory = false;

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                string fileName = openFileDialog1.FileName;
                //压缩
                Compact(fileName);
            }
        }

        ///压缩修复ACCESS数据库,mdbPath为数据库绝对路径
        public void Compact(string mdbPath)
        {
            if (!File.Exists(mdbPath)) //检查数据库是否已存在
            {
                throw new Exception("目标数据库不存在,无法压缩");
            }
            //声明临时数据库的名称
            string temp = DateTime.Now.Year.ToString();
            temp += DateTime.Now.Month.ToString();
            temp += DateTime.Now.Day.ToString();
            temp += DateTime.Now.Hour.ToString();
            temp += DateTime.Now.Minute.ToString();
            temp += DateTime.Now.Second.ToString() + ".bak";
            temp = mdbPath.Substring(0, mdbPath.LastIndexOf("\\") + 1) + temp;
            //定义临时数据库的连接字符串
            string temp2 = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + temp;
            //定义目标数据库的连接字符串
            string mdbPath2 = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + mdbPath;
            //创建一个JetEngineClass对象的实例
            JRO.JetEngineClass jt = new JRO.JetEngineClass();
            //使用JetEngineClass对象的CompactDatabase方法压缩修复数据库
            jt.CompactDatabase(mdbPath2, temp2);
            //拷贝临时数据库到目标数据库(覆盖)
            File.Copy(temp, mdbPath, true);
            //最后删除临时数据库
            File.Delete(temp);
        }

        //备份当前数据库
        private void toolStripMenuItemBackUpDB_Click(object sender, EventArgs e)
        {
            OleDbConnection conn = new OleDbConnection(AccessAdo.strConnection);
            string Path1 = conn.DataSource;

            SaveFileDialog sf = new SaveFileDialog();
            //设置文件类型
            sf.Filter = "数据文件(*.mdb)|*..mdb";
            if (sf.ShowDialog() == DialogResult.OK)
            {
                string Path2 = sf.FileName;
                Backup(Path1, Path2);
            }
        }

        /// 备份数据库,mdb1,源数据库绝对路径; mdb2: 目标数据库绝对路径 
        public void Backup(string mdb1, string mdb2)
        {
            if (!File.Exists(mdb1))
            {
                throw new Exception("源数据库不存在");
            }
            try
            {
                File.Copy(mdb1, mdb2, true);
            }
            catch (IOException ixp)
            {
                throw new Exception(ixp.ToString());
            }
        }

        private void toolStripMenuItemUndo_Click(object sender, EventArgs e)
        {
            if (ActiveDocument != null)
                ActiveDocument.Scintilla.UndoRedo.Undo();
        }

        private void toolStripMenuItemRedo_Click(object sender, EventArgs e)
        {
            if (ActiveDocument != null)
                ActiveDocument.Scintilla.UndoRedo.Redo();
        }

        private void toolStripMenuItemCut_Click(object sender, EventArgs e)
        {
            if (ActiveDocument != null)
                ActiveDocument.Scintilla.Clipboard.Cut();
        }

        private void toolStripMenuItemCopy_Click(object sender, EventArgs e)
        {
            if (ActiveDocument != null)
                ActiveDocument.Scintilla.Clipboard.Copy();
        }

        private void toolStripMenuItempaste_Click(object sender, EventArgs e)
        {
            if (ActiveDocument != null)
                ActiveDocument.Scintilla.Clipboard.Paste();
        }

        private void toolStripMenuItemFind_Click(object sender, EventArgs e)
        {
            if (ActiveDocument != null)
                 ActiveDocument.Scintilla.FindReplace.ShowFind();
        }

        private void toolStripMenuItemReplace_Click(object sender, EventArgs e)
        {
            if (ActiveDocument != null)
                ActiveDocument.Scintilla.FindReplace.ShowReplace();
        }

        private void toolStripMenuItemSelAll_Click(object sender, EventArgs e)
        {
            if (ActiveDocument != null)
                ActiveDocument.Scintilla.Selection.SelectAll();
        }

        private void toolStripButtonDel_Click(object sender, EventArgs e)
        {
            if (dockPanel1.ActiveContent.GetType() == typeof(FormTreeLeft))
            {
                frTree.DelNode();
            }
            else if (dockPanel1.ActiveContent.GetType() == typeof(YouDaoTree))
            {

                frYoudaoTree.DelNode();
            }
        }

        //自动换行
        private void toolStripMenuItemAutoWrap_Click(object sender, EventArgs e)
        {
            // 所有打开的文档自动换行
            toolStripMenuItemAutoWrap.Checked = !toolStripMenuItemAutoWrap.Checked;
            if (Attachment.isWelcomePageopen == "0")
            {
                foreach (DocumentForm doc in dockPanel1.Documents)
                {
                    if (toolStripMenuItemAutoWrap.Checked)
                        doc.Scintilla.LineWrapping.Mode = LineWrappingMode.Word;
                    else
                        doc.Scintilla.LineWrapping.Mode = LineWrappingMode.None;
                }
            }
            
        }

        private void toolStripButton6_Click(object sender, EventArgs e)
        {
           
        }

        private void toolStripMenuItemFont_Click(object sender, EventArgs e)
        {
            FontDialog fontDialog = new FontDialog();
            fontDialog.AllowScriptChange = true;
            fontDialog.ShowEffects = false;
            if (fontDialog.ShowDialog() != DialogResult.Cancel)
            {
                FontConverter fc = new FontConverter();
                string sfont = fc.ConvertToString(fontDialog.Font);

                if (Attachment.isWelcomePageopen == "1")
                {
                    return;
                }
                foreach (DocumentForm doc in dockPanel1.Documents)
                {
                    SetFont(doc,(Font)fc.ConvertFromString(sfont));
                }

                PubFunc.SetConfiguration("defaultFont", sfont);
            }
        }

        public void SetFont(DocumentForm doc,Font xFont)
        {
            //ActiveDocument.Scintilla.Font = xFont;

        doc.Scintilla.Styles.Default.Font = xFont;

        doc.Scintilla.Styles[0].Font = xFont;               //'white space
        doc.Scintilla.Styles[1].Font = xFont;              //'comments-block
        doc.Scintilla.Styles[2].Font = xFont;              // 'comments-singlechar
        doc.Scintilla.Styles[3].Font = xFont;              // 'half-formed comment
        doc.Scintilla.Styles[4].Font = xFont;            //   'numbers
        doc.Scintilla.Styles[5].Font = xFont;              // 'keyword after complete
        doc.Scintilla.Styles[6].Font = xFont;              // 'quoted text-double
        doc.Scintilla.Styles[7].Font = xFont;              // 'quoted text-single
        doc.Scintilla.Styles[8].Font = xFont;               //'"table" keyword l
        doc.Scintilla.Styles[9].Font = xFont;               //'? knows
        doc.Scintilla.Styles[10].Font = xFont;              //'symbol (<>);=-
        doc.Scintilla.Styles[11].Font = xFont;              //'half-formed words
        doc.Scintilla.Styles[12].Font = xFont;              //'mixed quoted text 'text"  ?strange
        doc.Scintilla.Styles[14].Font = xFont;              //'sql-type keyword (still looks weird)
        doc.Scintilla.Styles[15].Font = xFont;              //'sql @symbol in a comment 
        doc.Scintilla.Styles[16].Font = xFont;              //'sql function returning INT
        doc.Scintilla.Styles[19].Font = xFont;              //'in/out
        doc.Scintilla.Styles[32].Font = xFont;              //'plain ordinary whitespace, that exists everywhere

        doc.Scintilla.Styles[StylesCommon.LineNumber].Font = xFont;
        doc.Scintilla.Styles[StylesCommon.BraceBad].Font = xFont;
        doc.Scintilla.Styles[StylesCommon.BraceLight].Font = xFont;
        doc.Scintilla.Styles[StylesCommon.CallTip].Font = xFont;
        doc.Scintilla.Styles[StylesCommon.ControlChar].Font = xFont;
        doc.Scintilla.Styles[StylesCommon.Default].Font = xFont;
        doc.Scintilla.Styles[StylesCommon.IndentGuide].Font = xFont;
        doc.Scintilla.Styles[StylesCommon.LastPredefined].Font = xFont;
        doc.Scintilla.Styles[StylesCommon.Max].Font = xFont;
        }


        private void toolStripMenuItemIsShowTB_Click(object sender, EventArgs e)
        {
            toolStripMenuItemIsShowTB.Checked = !toolStripMenuItemIsShowTB.Checked;

            toolStripMain.Visible = toolStripMenuItemIsShowTB.Checked;
        }

        private void toolStripMenuItemIsShowSb_Click(object sender, EventArgs e)
        {
            toolStripMenuItemIsShowSb.Checked = !toolStripMenuItemIsShowSb.Checked;
            statusStripMain.Visible = toolStripMenuItemIsShowSb.Checked;
        }

        private void toolStripMenuItemIsShowLeft_Click(object sender, EventArgs e)
        {
            frTree.Show(dockPanel1);
            frmMark.Show(dockPanel1);
            FormFind.Show(dockPanel1);
            frYoudaoTree.Show(dockPanel1);
        }

        private void toolStripMenuItemIsShowAtt_Click(object sender, EventArgs e)
        {
            frmAttchment.Show(dockPanel1);
        }

        private void toolStripButtonProp_Click(object sender, EventArgs e)
        {
            if (dockPanel1.ActiveContent.GetType() == typeof(FormTreeLeft))
            {
                frTree.ShowProp();
            }
            else if (dockPanel1.ActiveContent.GetType() == typeof(YouDaoTree))
            {

                frYoudaoTree.ShowProp();
            }
        }

        //打开欢迎界面
        public void openWelcomePage()
        {
            WelcomeDoc welDoc = new WelcomeDoc();
            welDoc.NodeId = "-1";
            welDoc.Show(dockPanel1);
            Attachment.isWelcomePageopen = "1";
        }

        private void FormMain_Load(object sender, EventArgs e)
        {
            Attachment.frmMain = this;

            //加载布局
            string configFile = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "DockPanel.config");

            if (File.Exists(configFile))
                dockPanel1.LoadFromXml(configFile, m_deserializeDockContent);

            //打开欢迎界面
            openWelcomePage();
        }

        //状态栏标题
        public void showFullPathTime(string path,string crtTime)
        {
            this.toolStripStatusLabelTitle.Text = path;
            this.toolStripStatusLabelDocTime.Text = crtTime;
        }

        public void showLinePosition(int x, int y)
        {
            this.toolStripStatusLabelRowCol.Text = "行 " + x.ToString() + "        列 " + y.ToString();
        }

        //删除所有书签
        private void toolStripMenuItemDelAllMark_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("删除所有书签？", "提示", MessageBoxButtons.YesNo) == DialogResult.No)
            {
                return;
            } 
            AccessAdo.ExecuteNonQuery("update ttree set marktime=0");
            //删除有道云书签
            if (Attachment.IsTokeneffective == 1)
            {
                XmlDocument doc = new XmlDocument();
                doc.Load("TreeNodeLocal.xml");
                XmlNodeList xlist = doc.SelectNodes("//note[@isMark='1']");
                foreach (XmlNode xnode in xlist)
                {
                    xnode.Attributes["isMark"].Value = "0";
                }
                doc.Save("TreeNodeLocal.xml");
                XMLAPI.XML2Yun();
            }
        }

        private void toolStripMenuItemExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void FormMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            string configFile = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "DockPanel.config");
                dockPanel1.SaveAsXml(configFile);
        }

        private IDockContent GetContentFromPersistString(string persistString)
        {
            if (persistString == typeof(FormTreeLeft).ToString())
                return frTree;
            else if (persistString == typeof(YouDaoTree).ToString())
                return frYoudaoTree;
            else if (persistString == typeof(FormAttachment).ToString())
                return frmAttchment;
            else if (persistString == typeof(DocFind).ToString())
                return FormFind;
            else if (persistString == typeof(DocMark).ToString())
                return frmMark;
            else
            {
                return null;
            }
        }

        //关闭
        private void toolStripButtonCloseTab_Click(object sender, EventArgs e)
        {
            if (ActiveDocument != null)
                ActiveDocument.Close();
        }

        //关闭所有
        private void toolStripButtonCloseTabAll_Click(object sender, EventArgs e)
        {
            if (Attachment.isWelcomePageopen == "0")
            {
                IDockContent[] documents = dockPanel1.DocumentsToArray();

                foreach (IDockContent content in documents)
                {
                    content.DockHandler.Close();
                }
            }
        }

        private void toolStripMenuItemExportHTML_Click(object sender, EventArgs e)
        {
            if (ActiveDocument != null)
                ActiveDocument.ExportAsHtml();
        }

        private void toolStripMenuItemPrint_Click(object sender, EventArgs e)
        {
            if (ActiveDocument != null)
                ActiveDocument.Scintilla.Printing.Print();
        }

        private void toolStripMenuItemPv_Click(object sender, EventArgs e)
        {
            if (ActiveDocument != null)
                ActiveDocument.Scintilla.Printing.PrintPreview();
        }


        //授权，并且初始化用户信息
        private void toolStripMenuItemAuthor_Click(object sender, EventArgs e)
        {
            
        }


        //在目录窗口激活或者失去焦点时禁用菜单按钮
        public void HideShowMenu(Boolean sMode)
        {
              
              this.toolStripButtonNewText.Enabled = sMode;
              this.toolStripButtonNewDir.Enabled = sMode;
              this.toolStripButtonDel.Enabled = sMode;
              this.toolStripButtonProp.Enabled = sMode;
              this.toolStripButtonUp.Enabled = sMode;
              this.toolStripButtonDown.Enabled = sMode;
        }

        //窗体发生变化时触发
        private void dockPanel1_ActiveContentChanged(object sender, EventArgs e)
        {
            if (dockPanel1.ActiveContent != null)
            {
                try
                {
                    if (dockPanel1.ActiveContent.GetType() == typeof(FormTreeLeft))
                    {
                        HideShowMenu(true);
                    }
                    else if (dockPanel1.ActiveContent.GetType() == typeof(YouDaoTree)&&Attachment.IsTokeneffective==1)
                    {
                        HideShowMenu(true);
                    }
                    else
                    {
                        HideShowMenu(false);
                    }
                }
                catch (Exception ex)
                {

                    throw ex;
                }
            }
        }

        private void FormMain_Shown(object sender, EventArgs e)
        {
            //IniYouDaoAuthor();
        }

        //关于
        private void toolStripMenuItemAbout_Click(object sender, EventArgs e)
        {
            About ab = new About();
            ab.ShowDialog();
        }

        //保存
        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (ActiveDocument != null)
                ActiveDocument.Save();
        }

        //撤销
        private void toolStripButtonUndo_Click(object sender, EventArgs e)
        {
            if (ActiveDocument != null)
                ActiveDocument.Scintilla.UndoRedo.Undo();
        }

        //反撤销
        private void toolStripButtonRedo_Click(object sender, EventArgs e)
        {
            if (ActiveDocument != null)
                ActiveDocument.Scintilla.UndoRedo.Redo();
        }

        //剪切
        private void toolStripButtonCut_Click(object sender, EventArgs e)
        {
            if (ActiveDocument != null)
                ActiveDocument.Scintilla.Clipboard.Cut();
        }

        //复制
        private void toolStripButtonCopy_Click(object sender, EventArgs e)
        {
            if (ActiveDocument != null)
                ActiveDocument.Scintilla.Clipboard.Copy();
        }

        //粘贴
        private void toolStripButtonPaste_Click(object sender, EventArgs e)
        {
            if (ActiveDocument != null)
                ActiveDocument.Scintilla.Clipboard.Paste();
        }

        //授权
        private void toolStripMenuItem3_Click(object sender, EventArgs e)
        {
            FormAuthor frAuthor = new FormAuthor();
            DialogResult dr = frAuthor.ShowDialog();
            if (dr == DialogResult.OK)
            {
                //授权成功，禁用登陆按钮，初始化目录以及配置信息，显示有道云树
                this.toolStripMenuItemLogin.Visible = false;
                this.toolStripMenuItemUinfo.Visible = true;
                Text = "WeCode--已登录";
                //初始化目录以及配置信息
                AuthorAPI.CreatewecodeConfig();
                //从云端拉取目录配置到本地
                XMLAPI.Yun2XML();

                Attachment.IsTokeneffective = 1;
                //加载树
                frYoudaoTree.IniYoudaoTree();
                HideShowMenu(true);
            }
        }

        //查看用户信息
        private void toolStripMenuItemUinfo_Click(object sender, EventArgs e)
        {
            string sUserInfo = NoteAPI.GetUserInfo();
            if (sUserInfo=="")
                return;
            JObject jo = JObject.Parse(sUserInfo);
            float userdsize=(float)jo["used_size"];
            float totalsize = (float)jo["total_size"];
            string user = jo["user"].ToString();
            string info = "当前用户：" + user + "\n";
            info += "总共空间：" + totalsize / 1048576 + "MB\n";
            info += "已用空间：" + userdsize / 1048576 + "MB\n";
            MessageBox.Show(info,"用户信息");

        }

    }
}