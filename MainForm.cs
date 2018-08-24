using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Resources;
using System.Xml;
using System.Windows.Forms;

using ResxEditor.Properties;

namespace ResxEditor
{
    public partial class MainForm : Form
    {
        string xmlFileName = "";
        ResXResourceSet resourceSet;
        XmlDataDocument xmlDocument;

        bool isDocumentOpen = false;
        bool isDataModified = false;
        bool showComment = true;

        float defaultTextSize;

        #region Constructor

        public MainForm(string[] args)
        {
            InitializeComponent();
            
            xmlGridView.CurrentCellDirtyStateChanged += new EventHandler(xmlGridView_CurrentCellDirtyStateChanged);
            Text = Resources.Title;
            if (args.Length > 0)
            {
                OpenFile(args[0]);
            }
        }

        #endregion

        #region methods - main logic stored here
        
        private void OpenFile(string fileName)
        {
            FileStream file;
            try
            {
                xmlDocument = new XmlDataDocument();
                xmlDocument.DataSet.ReadXmlSchema(fileName);
                file = new FileStream(fileName, FileMode.Open, FileAccess.Read);
                xmlDocument.Load(file);
                file.Close();
                xmlGridView.DataSource = xmlDocument.DataSet;
                xmlGridView.DataMember = "data";

                //we need this to exclude non-text fields
                file = new FileStream(fileName, FileMode.Open, FileAccess.Read);
                resourceSet = new ResXResourceSet(file);
            }
            catch (Exception ex)
            {
                isDocumentOpen = false;
                ReportError(ex.Message);
                return;
            }

            xmlFileName = fileName;
            InitGrid();

            //hide non-text fields
            foreach (DictionaryEntry d in resourceSet)
            {
                if (d.Value == null || d.Value.GetType() != typeof(string))
                {
                    foreach (DataGridViewRow row in xmlGridView.Rows)
                    {
                        if (row.Cells["name"].Value.ToString() == d.Key.ToString())
                            row.Visible = false;
                    }
                }
            }
            //release resources
            resourceSet.Close();
            file.Close();
            
            isDataModified = false;
            isDocumentOpen = true;
            UpdateForm();
        }

        private void InitGrid()
        {
            xmlGridView.AutoGenerateColumns = true;
            xmlGridView.DefaultCellStyle.WrapMode = DataGridViewTriState.True;

            defaultTextSize = xmlGridView.DefaultCellStyle.Font.Size;

            //hiding all columns
            foreach (DataGridViewColumn col in xmlGridView.Columns)
            {
                col.Visible = false;
            }

            //show and set up the Value column
            xmlGridView.Columns["value"].Visible = true;
            xmlGridView.Columns["value"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            xmlGridView.Columns["value"].HeaderText = Resources.ValueHeader;
            xmlGridView.Columns["value"].SortMode = DataGridViewColumnSortMode.NotSortable;

            //show and set up the Comment column
            xmlGridView.Columns["comment"].Visible = showComment;
            xmlGridView.Columns["comment"].ReadOnly = true;
            xmlGridView.Columns["comment"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            xmlGridView.Columns["comment"].HeaderText = Resources.CommentHeader;
            xmlGridView.Columns["comment"].DefaultCellStyle.BackColor = Color.LightGray;
            xmlGridView.Columns["comment"].SortMode = DataGridViewColumnSortMode.NotSortable;

            //set up the Name column
            xmlGridView.Columns["name"].Visible = true;
            xmlGridView.Columns["name"].HeaderText = Resources.NameHeader;
            xmlGridView.Columns["name"].ReadOnly = true;
            xmlGridView.Columns["name"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            xmlGridView.Columns["name"].DefaultCellStyle.BackColor = Color.LightGray;
            xmlGridView.Columns["name"].SortMode = DataGridViewColumnSortMode.NotSortable;

            // auto-resize rows
            xmlGridView.AutoResizeRows(DataGridViewAutoSizeRowsMode.AllCells);
        }

        private void UpdateForm()
        {
            if (xmlFileName == null || xmlFileName == string.Empty)
            {
                Text = Resources.FormTitle;
            }
            else
            {
                string star = isDataModified ? "*" : "";

                int index = Math.Max(xmlFileName.LastIndexOf('\\') + 1, 0);
                Text = xmlFileName.Substring(index) + star + " - " + Resources.FormTitle;
            }

            textSizeMenuItem.Enabled = isDocumentOpen;
            toggleCommentMenuItem.Enabled = isDocumentOpen;
            saveMenuItem.Enabled = isDocumentOpen;
            saveAsMenuItem.Enabled = isDocumentOpen;
        }

        private void ReportError(string message)
        {
            MessageBox.Show(message, "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private bool ConfirmFileSave(string message)
        {
            DialogResult res;
            res = MessageBox.Show(message, Resources.UnsavedChangesCaption, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Exclamation);
            switch (res)
            {
                case DialogResult.Cancel:
                    return false;
                case DialogResult.No:
                    return true;
                default:// DialogResult.Yes:
                    SaveFile();
                    return true;
            }
        } 
        
        private void SaveFile()
        {
            Validate();
            xmlGridView.EndEdit();
            CurrencyManager cm = (CurrencyManager)xmlGridView.BindingContext[xmlGridView.DataSource, "data"];
            cm.EndCurrentEdit();
            xmlDocument.Save(xmlFileName);
            isDataModified = false;
            UpdateForm();
        } 

        #endregion

        #region event handlers

        void xmlGridView_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            isDataModified = true;
            return;
        } 
        
        protected override void OnClosing(CancelEventArgs e)
        {
            if (isDataModified)
            {
                if (!ConfirmFileSave(Resources.UnsavedChangesExit))
                {
                    e.Cancel = true;
                    return;
                }
            }
            base.OnClosing(e);
        }

        #endregion

        #region menu items
        
        private void openMenuItem_Click(object sender, EventArgs e)
        {
            if (isDataModified)
            {
                if (!ConfirmFileSave(Resources.UnsavedChanges))
                    return;
            }
            OpenFileDialog dialog = new OpenFileDialog();

            dialog.Filter = Resources.ResxFilesFilter + " (*.resx)|*.resx|"
                + Resources.AllFilesFilter + " (*.*)|*.*";

            dialog.FilterIndex = 1;
            dialog.RestoreDirectory = true;

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                OpenFile(dialog.FileName);
            }
        } 

        private void exitMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        } 

        private void saveMenuItem_Click(object sender, EventArgs e)
        {
            SaveFile();
        } 

        private void saveAsMenuItem_Click(object sender, EventArgs e)
        {
            Stream fileStream;
            SaveFileDialog dialog = new SaveFileDialog();

            dialog.Filter = Resources.ResxFilesFilter + " (*.resx)|*.resx|"
                + Resources.AllFilesFilter + " (*.*)|*.*";

            dialog.FilterIndex = 1;
            dialog.RestoreDirectory = true;

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                if ((fileStream = dialog.OpenFile()) != null)
                {
                    fileStream.Close();
                    xmlFileName = dialog.FileName;
                    SaveFile();
                }

                isDataModified = false;
                UpdateForm();
            }
        } 

        private void increaseTextSizeMenuItem_Click(object sender, EventArgs e)
        {
            xmlGridView.Font = new Font(xmlGridView.Font.FontFamily, xmlGridView.Font.Size * 1.2f);
            xmlGridView.AutoResizeRows(DataGridViewAutoSizeRowsMode.AllCells);
        }

        private void decreaseTextSizeMenuItem_Click(object sender, EventArgs e)
        {
            xmlGridView.Font = new Font(xmlGridView.Font.FontFamily, xmlGridView.Font.Size / 1.2f);
            xmlGridView.AutoResizeRows(DataGridViewAutoSizeRowsMode.AllCells);
        } 

        private void defaultTextSizeMenuItem_Click(object sender, EventArgs e)
        {
            xmlGridView.Font = new Font(xmlGridView.Font.FontFamily, defaultTextSize);
            xmlGridView.AutoResizeRows(DataGridViewAutoSizeRowsMode.AllCells);
        }

        #endregion

        private void toggleCommentMenuItem_Click(object sender, EventArgs e)
        {
            if (showComment)
            {
                toggleCommentMenuItem.Text = Resources.ShowCommentCommand;
                showComment = false;
            }
            else
            {
                toggleCommentMenuItem.Text = Resources.HideCommentCommand;
                showComment = true;
            }
            xmlGridView.Columns["comment"].Visible = showComment;
        }
    }
}