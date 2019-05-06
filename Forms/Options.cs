/**************************************************************************************
    Stampfer - Gothic Script Editor
    Copyright (C) 2008, 2009 Jpmon1, Alexander "Sumpfkrautjunkie" Ruppert

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
**************************************************************************************/
using System;
using System.Collections;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Xml;
using ICSharpCode.TextEditor.Document;

namespace Peter
{
    public partial class Options : Form
    {
        public ArrayList m_OptionPanels;
        private readonly MainForm m_MainForm;
        private bool m_EditorChanged;
        private bool newscriptspath = false;
        public Options(MainForm main)
        {
            InitializeComponent();
            this.m_MainForm = main;
            this.m_OptionPanels = new ArrayList
            {
                this.Allgemein,
                this.Editor,
                this.Gothic
            };
            var config = this.m_MainForm.Config;
            ApplyConfig(config);

            if (TBakPatch.Text != "")
            {
                ckbInFolderOnly.Enabled = true;
                nBakMin.Enabled = true;
            }
            else
            {
                ckbInFolderOnly.Enabled = false;
                nBakMin.Enabled = false;
                nBakMin.Value = 0;
                ckbInFolderOnly.Checked = false;
            }

            this.textEditorControl1.SetHighlighting("Java");
            this.textEditorControl1.Text = "public class Foo\r\n" +
                                            "{\r\n" +
                                            "	public int[] X = new int[]{1, 3, 5\r\n" +
                                            "		7, 9, 11};\r\n" +
                                            "\r\n" +
                                            "	public void foo(boolean a, int x,\r\n" +
                                            "	                int y, int z)\r\n" +
                                            "	{\r\n" +
                                            "		label1:\r\n" +
                                            "		do\r\n" +
                                            "		{\r\n" +
                                            "			try\r\n" +
                                            "			{\r\n" +
                                            "				if (x > 0)\r\n" +
                                            "				{\r\n" +
                                            "					int someVariable = a ?\r\n" +
                                            "						x :\r\n" +
                                            "						y;\r\n" +
                                            "				}\r\n" +
                                            "				else if (x < 0)\r\n" +
                                            "				{\r\n" +
                                            "					int someVariable = (y +\r\n" +
                                            "						z\r\n" +
                                            "					);\r\n" +
                                            "					someVariable = x =\r\n" +
                                            "						x +\r\n" +
                                            "							y;\r\n" +
                                            "				}\r\n" +
                                            "				else\r\n" +
                                            "				{\r\n" +
                                            "					label2:\r\n" +
                                            "					for (int i = 0;\r\n" +
                                            "					     i < 5;\r\n" +
                                            "					     i++)\r\n" +
                                            "						doSomething(i);\r\n" +
                                            "				}\r\n" +
                                            "				switch (a)\r\n" +
                                            "				{\r\n" +
                                            "					case 0:\r\n" +
                                            "						doCase0()\r\n;" +
                                            "						break;\r\n" +
                                            "					default:\r\n" +
                                            "						doDefault();\r\n" +
                                            "				}\r\n" +
                                            "			}\r\n" +
                                            "			catch (Exception e)\r\n" +
                                            "			{\r\n" +
                                            "				processException(e.getMessage(),\r\n" +
                                            "					x + y, z, a);\r\n" +
                                            "			\r\n}" +
                                            "			finally\r\n" +
                                            "			{\r\n" +
                                            "				processFinally();\r\n" +
                                            "			}\r\n" +
                                            "		}\r\n" +
                                            "		while (true);\r\n" +
                                            "\r\n" +
                                            "		if (2 < 3) return;\r\n" +
                                            "		if (3 < 4)\r\n" +
                                            "			return;\r\n" +
                                            "		do x++ while (x < 10000);\r\n" +
                                            "		while (x < 50000) x++;\r\n" +
                                            "		for (int i = 0; i < 5; i++) System.out.println(i);\r\n" +
                                            "	}\r\n" +
                                            "}";
            this.UpdateEditor();
            this.lstMain.Items[0].Selected = true;
            this.m_EditorChanged = false;
        }

        private void ApplyConfig(Classes.Configuration.PeterConfig config)
        {
            this.ckbSaveOnExt.Checked = config.Application.SaveOnExit;
            this.nudRecentFile.Value = config.Application.RecentFileCount;
            this.nudRecentProject.Value = config.Application.RecentProjectCount;

            this.ckbShowEol.Checked = config.Editor.ShowEOL;
            this.ckbShowInvalidLines.Checked = config.Editor.ShowInvalidLines;
            this.ckbShowSpaces.Checked = config.Editor.ShowSpaces;
            this.ckbShowTabs.Checked = config.Editor.ShowTabs;
            this.ckbShowMatchingBracket.Checked = config.Editor.ShowMatchBracket;
            this.ckbShowLineNumbes.Checked = config.Editor.ShowLineNumbers;
            this.ckbShowHRuler.Checked = config.Editor.ShowHRuler;
            this.ckbShowVRuler.Checked = config.Editor.ShowVRuler;
            this.ckbEnableCodeFolding.Checked = config.Editor.EnableCodeFolding;
            this.ckbConvertTabs.Checked = config.Editor.ConvertTabs;
            this.ckbUseAntiAlias.Checked = config.Editor.UseAntiAlias;
            this.ckbAllowCaretBeyondEol.Checked = config.Editor.AllowCaretBeyondEOL;
            this.ckbHighlightCurrentLine.Checked = config.Editor.HighlightCurrentLine;
            this.ckbAutoInsertBracket.Checked = config.Editor.AutoInsertBracket;
            this.nudTabIndent.Value = config.Editor.TabIndent;
            this.nudVRuler.Value = config.Editor.VerticalRulerCol;
            this.cmbIndentStyle.Text = config.Editor.IndentStyle;
            this.cmbBracketStyle.Text = config.Editor.BracketMatchingStyle;
            this.fontDialog1.Font = this.textEditorControl1.Font = config.Editor.FontInstance;

            this.TScriptsPatch.Text = config.Editor.Scripts;
            this.TBilderPatch.Text = config.Editor.Bilder;
            this.ckbMessageBox.Checked = config.Editor.Parser;
            this.ckbBackup.Checked = config.Editor.Backup;
            this.TBakPatch.Text = config.Editor.Backupfolder;
            this.nBakMin.Value = config.Editor.Backupeach;
            this.ckbInFolderOnly.Checked = config.Editor.Backupfolderonly;
            this.ckbAutoCompleteAuto.Checked = config.Editor.Autocomplete;
            this.ckbAutoBrackets.Checked = config.Editor.Autobrackets;
        }

        /// <summary>
        /// Adds an Option panel...
        /// </summary>
        /// <param name="panel">Panel to Add</param>
        public void AddOptionPanel(Control panel, Image image)
        {
            this.m_OptionPanels.Add(panel);
            var lvi = new ListViewItem(panel.Name);
            if (image != null)
            {
                var index = this.imgMain.Images.Add(image, Color.Transparent);
                lvi.ImageIndex = index;
            }
            this.lstMain.Items.Add(lvi);
        }

        private void lstMain_SelectedIndexChanged(object sender, EventArgs e)
        {

            if (this.lstMain.SelectedItems.Count == 1)
            {
                for (var a = 0; a < this.m_OptionPanels.Count; a++)
                {
                    var ctrl = (Control)this.m_OptionPanels[a];

                    if (ctrl.Name == lstMain.SelectedItems[0].Text)
                    {
                        this.splitContainer1.Panel2.Controls.Clear();
                        ctrl.Dock = DockStyle.Fill;
                        this.splitContainer1.Panel2.Controls.Add(ctrl);
                        break;
                    }
                }
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            this.btnApply_Click(sender, e);
            this.Close();
        }

        private void btnApply_Click(object sender, EventArgs e)
        {
            this.Cursor = Cursors.WaitCursor;
            this.m_MainForm.Trace("Übernehme allgemeine Einstellungen");

            var config = this.m_MainForm.Config;
            UpdateConfig(config);
            this.m_MainForm.Trace("Übernehme Einstellungen");
            this.m_MainForm.SaveConfig();
            this.m_MainForm.LoadConfigFile(true);
            this.Cursor = Cursors.Default;
            this.m_MainForm.Trace("");
            if (newscriptspath)
            {
                MessageBox.Show("Script - Pfad wurde geändert. Stampfer wird nun neu gestartet.", "Neustart", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.Close();
                Application.Restart();
            }
        }

        private void UpdateConfig(Classes.Configuration.PeterConfig config)
        {
            config.Application.SaveOnExit = this.ckbSaveOnExt.Checked;
            config.Application.RecentFileCount = (int)this.nudRecentFile.Value;
            config.Application.RecentProjectCount = (int)this.nudRecentProject.Value;
            config.Application.SaveOnExit = this.ckbSaveOnExt.Checked;
            var editorConfig = config.Editor;

            if (this.m_EditorChanged)
            {
                this.m_MainForm.Trace("Übernehme Editor Einstellungen");
                editorConfig.ShowEOL = this.ckbShowEol.Checked;
                editorConfig.ShowInvalidLines = this.ckbShowInvalidLines.Checked;
                editorConfig.ShowSpaces = this.ckbShowSpaces.Checked;
                editorConfig.ShowTabs = this.ckbShowTabs.Checked;
                editorConfig.ShowMatchBracket = this.ckbShowMatchingBracket.Checked;
                editorConfig.ShowLineNumbers = this.ckbShowLineNumbes.Checked;
                editorConfig.ShowHRuler = this.ckbShowHRuler.Checked;
                editorConfig.ShowVRuler = this.ckbShowVRuler.Checked;
                editorConfig.EnableCodeFolding = this.ckbEnableCodeFolding.Checked;
                editorConfig.ConvertTabs = this.ckbConvertTabs.Checked;
                editorConfig.UseAntiAlias = this.ckbUseAntiAlias.Checked;
                editorConfig.AllowCaretBeyondEOL = this.ckbAllowCaretBeyondEol.Checked;
                editorConfig.HighlightCurrentLine = this.ckbHighlightCurrentLine.Checked;
                editorConfig.AutoInsertBracket = this.ckbAutoInsertBracket.Checked;
                editorConfig.TabIndent = (int)this.nudTabIndent.Value;
                editorConfig.VerticalRulerCol = (int)this.nudVRuler.Value;
                editorConfig.IndentStyle = this.cmbIndentStyle.Text;
                editorConfig.BracketMatchingStyle = this.cmbBracketStyle.Text;
                editorConfig.Font = this.textEditorControl1.Font.FontFamily.Name + ";" + textEditorControl1.Font.Size.ToString();
                editorConfig.Scripts = this.TScriptsPatch.Text;
                editorConfig.Bilder = this.TBilderPatch.Text;
                editorConfig.Parser = this.ckbMessageBox.Checked;
                editorConfig.Backup = this.ckbBackup.Checked;
                editorConfig.Backupeach = (int)this.nBakMin.Value;
                editorConfig.Backupfolder = this.TBakPatch.Text;
                editorConfig.Backupfolderonly = this.ckbInFolderOnly.Checked;
                editorConfig.Autocomplete = this.ckbAutoCompleteAuto.Checked;
                editorConfig.Autobrackets = this.ckbAutoBrackets.Checked;
            }
        }

        private void BtnFont_Click(object sender, EventArgs e)
        {
            this.fontDialog1.Font = this.textEditorControl1.Font;
            if (this.fontDialog1.ShowDialog() == DialogResult.OK)
            {
                this.textEditorControl1.Font = this.fontDialog1.Font;
                this.m_EditorChanged = true;
            }
        }

        private void UpdateEditor()
        {
            this.textEditorControl1.ShowEOLMarkers = this.ckbShowEol.Checked;
            this.textEditorControl1.ShowInvalidLines = this.ckbShowInvalidLines.Checked;
            this.textEditorControl1.ShowSpaces = this.ckbShowSpaces.Checked;
            this.textEditorControl1.ShowTabs = this.ckbShowTabs.Checked;
            this.textEditorControl1.ShowMatchingBracket = this.ckbShowMatchingBracket.Checked;
            this.textEditorControl1.ShowLineNumbers = this.ckbShowLineNumbes.Checked;
            this.textEditorControl1.ShowHRuler = this.ckbShowHRuler.Checked;
            this.textEditorControl1.ShowVRuler = this.ckbShowVRuler.Checked;
            this.textEditorControl1.EnableFolding = this.ckbEnableCodeFolding.Checked;
            this.textEditorControl1.ConvertTabsToSpaces = this.ckbConvertTabs.Checked;
            //this.textEditorControl1.UseAntiAliasFont = this.ckbUseAntiAlias.Checked; // #develop 2
            this.textEditorControl1.AllowCaretBeyondEOL = this.ckbAllowCaretBeyondEol.Checked;
            this.textEditorControl1.TextEditorProperties.AutoInsertCurlyBracket = this.ckbAutoInsertBracket.Checked;
            this.textEditorControl1.TabIndent = Convert.ToInt32(this.nudTabIndent.Value);
            this.textEditorControl1.VRulerRow = Convert.ToInt32(this.nudVRuler.Value);

            this.textEditorControl1.LineViewerStyle = (this.ckbHighlightCurrentLine.Checked) ? LineViewerStyle.FullRow : LineViewerStyle.None;
            switch (this.cmbBracketStyle.Text.ToLower())
            {
                case "vorher":
                    this.textEditorControl1.BracketMatchingStyle = BracketMatchingStyle.Before;
                    break;
                case "nachher":
                    this.textEditorControl1.BracketMatchingStyle = BracketMatchingStyle.After;
                    break;
            }
            switch (this.cmbIndentStyle.Text.ToLower())
            {
                case "auto":
                    this.textEditorControl1.IndentStyle = IndentStyle.Auto;
                    break;
                case "none":
                    this.textEditorControl1.IndentStyle = IndentStyle.None;
                    break;
                case "smart":
                    this.textEditorControl1.IndentStyle = IndentStyle.Smart;
                    break;
            }
            this.m_EditorChanged = true;
        }

        private void AnyOptionCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            this.UpdateEditor();
        }

        public void BtBrowseScripts_Click(object sender, EventArgs e)
        {
            FBD = new FolderBrowserDialog();
            if (this.FBD.ShowDialog() == DialogResult.OK)
            {
                var s = FBD.SelectedPath.ToLower();
                if (Directory.Exists(s))
                {
                    if (!s.ToLower().EndsWith("scripts"))
                    {
                        MessageBox.Show("Der angegebene Pfad sollte mit 'Scripts' enden, sofern Sie die Gothic-Originalscripte und Eintellungen unverändert vorliegen haben.", "Pfad zu den Scripten", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    }
                    this.TScriptsPatch.Text = s;
                    this.m_EditorChanged = true;
                    newscriptspath = true;
                }
                else
                {
                    MessageBox.Show("Der angegebene Pfad existiert nicht.", "Ungültiger Pfad", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                }
            }

        }

        private void BtBrowseBilder_Click(object sender, EventArgs e)
        {
            FBD = new FolderBrowserDialog();
            if (this.FBD.ShowDialog() == DialogResult.OK)
            {

                this.TBilderPatch.Text = FBD.SelectedPath.ToLower();
                this.m_EditorChanged = true;
            }
        }

        private void ckbMessageBox_CheckedChanged(object sender, EventArgs e)
        {
            this.m_EditorChanged = true;
        }

        private void ckbBackup_CheckedChanged(object sender, EventArgs e)
        {
            this.m_EditorChanged = true;
        }

        private void ckbAutoCompleteAuto_CheckedChanged(object sender, EventArgs e)
        {
            this.m_EditorChanged = true;
        }

        private void BtBrowseBak_Click(object sender, EventArgs e)
        {
            FBD = new FolderBrowserDialog();
            if (this.FBD.ShowDialog() == DialogResult.OK)
            {

                this.TBakPatch.Text = FBD.SelectedPath.ToLower();
                this.m_EditorChanged = true;
            }
        }

        private void nBakMin_ValueChanged(object sender, EventArgs e)
        {
            this.m_EditorChanged = true;
        }

        private void ckbInFolderOnly_CheckedChanged(object sender, EventArgs e)
        {
            this.m_EditorChanged = true;
        }

        private void TBakPatch_TextChanged(object sender, EventArgs e)
        {
            if (TBakPatch.Text != "")
            {
                ckbInFolderOnly.Enabled = true;
                nBakMin.Enabled = true;

            }
            else
            {
                ckbInFolderOnly.Enabled = false;
                nBakMin.Enabled = false;
                nBakMin.Value = 0;
                ckbInFolderOnly.Checked = false;
            }
        }

        private void ckbAutoBrackets_CheckedChanged(object sender, EventArgs e)
        {
            this.m_EditorChanged = true;
        }


    }
}
