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
using ICSharpCode.TextEditor.Document;
using PeterInterface;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Serialization;
using WeifenLuo.WinFormsUI.Docking;

namespace Peter
{
    public partial class MainForm : Form, IPeterPluginHost
    {
        private const string CONFIG_FILE = "Config.dat";
        private const string DOCK_CONFIG_FILE = "DockConfig.dat";
        private const string SCHEME_FOLDER = "HighLightSchemes\\";
        private const string PLUGIN_FOLDER = "Plugins\\";
        private const string VERSION = "1.0";
        private const int WM_COPYDATA = 0x4A;

        private int m_NewCount;
        private int m_RecentProjectCount;
        private int m_RecentFileCount;
        private bool m_SaveonExit;
        public string m_ScriptsPath;
        public string m_BilderPath;
        public bool m_ParserMessageBox;
        public string m_BackupFolder;
        public int m_BackupEach;
        public bool m_BackupFolderOnly;
        public bool m_AutoBrackets;
        public Classes.AutoComplete m_AutoComplete; //= new Peter.Classes.AutoComplete();

        public Classes.Configuration.Editor m_EditorConfig;
        private cPluginCollection m_Plugins;
        private Find m_FindControl;
        private GoToLine m_GotoControl;
        private ProjectManager m_ProjMan;
        private ctrlCodeStructure m_CodeStructure;
        public ctrlGothicInstances m_GothicStructure;
        private ctrlQuestManager m_QuestManager;
        public bool TabCloseBlock = false;
        private readonly SplashScreen ss = new SplashScreen(false);
        public Editor MyActiveEditor;
        public bool initialized = false;
        public string autocompletemenuauto = "Auto-Ergänzung > auto";
        public string autocompletemenumanu = "Auto-Ergänzung > manuell";
        public Classes.Configuration.PeterConfig Config { get; private set; }

        #region -= Constructor =-

        public MainForm(string[] args)
        {
            ss.Show();
            ss.Update();
            Init(args);
        }

        #endregion
        public void Init(string[] args)
        {
            this.m_SaveonExit = true;

            InitializeComponent();
            LoadTools();
            m_AutoComplete = new Peter.Classes.AutoComplete(this.ImgList, sslMain);
            this.Deactivate += new EventHandler(MainForm_Deactivate);

            // Set the Config File...
            this.DockConfigFile = Path.GetDirectoryName(Application.ExecutablePath) + "\\" + DOCK_CONFIG_FILE;
            this.ConfigFile = Path.GetDirectoryName(Application.ExecutablePath) + "\\" + CONFIG_FILE;
            this.m_EditorConfig = new Classes.Configuration.Editor();

            // Load Any Configuration from Config File...
            if (!File.Exists(this.ConfigFile))
            {
                var serializer = new XmlSerializer(typeof(Peter.Classes.Configuration.PeterConfig));
                using (var fs = File.Create(this.ConfigFile))
                {
                    serializer.Serialize(fs, Classes.Configuration.PeterConfig.Default);
                }
            }
            // Load Config File...
            this.Config = LoadConfigFile(false);
            if (string.IsNullOrEmpty(Config.Editor.Scripts))
            {
                var fbd = new FolderBrowserDialog
                {
                    Description = @"Wähle dein Script-Verzeichnis aus. (z.B. C:\Gothic II\_work\Data\Scripts)",
                    ShowNewFolderButton = false,
                };
                do
                {
                    if (fbd.ShowDialog() == DialogResult.OK)
                    {
                        Config.Editor.Scripts = fbd.SelectedPath;
                        break;
                    }
                } while (MessageBox.Show("Es wurde kein Script-Verzeichnis ausgewählt", "Fehler", MessageBoxButtons.RetryCancel) == DialogResult.Retry);
                if (string.IsNullOrEmpty(Config.Editor.Scripts))
                {
                    Environment.Exit(1);
                }
            }
            m_ScriptsPath = Config.Editor.Scripts;
            // Set Variabales...
            this.m_NewCount = 0;
            this.m_Plugins = new cPluginCollection();
            this.mnuHighlighting.Enabled = false;
            this.bookMarksToolStripMenuItem.Enabled = false;
            this.mnuCode.Enabled = false;
            this.ActiveContent = null;

            // Set up Find Control...
            this.m_FindControl = new Find(this)
            {
                Host = this,

                //this.m_GotoControl = new GoToLine(this);
                // this.m_GotoControl.Host = this;
                Icon = Icon.FromHandle(((Bitmap)this.GetInternalImage("Find")).GetHicon())
            };

            // Set up Project Manager...
            this.m_ProjMan = new ProjectManager(this)
            {
                Host = this,
                TabPageContextMenuStrip = this.ctxTab,
                Icon = Icon.FromHandle(((Bitmap)this.GetInternalImage("Project")).GetHicon())
            };

            // Set up Code Structure...
            this.m_CodeStructure = new ctrlCodeStructure(this)
            {
                Host = this,
                Icon = Icon.FromHandle(((Bitmap)this.GetInternalImage("Code")).GetHicon())
            };

            //Set up GothicInstances
            this.m_GothicStructure = new ctrlGothicInstances(this)
            {
                Host = this
            };
            this.m_QuestManager = new ctrlQuestManager(this)
            {
                Host = this
            };
            // Set Events...
            this.ctxEditor.Opening += new CancelEventHandler(ctxEditor_Opening);
            this.ctxTab.Opening += new CancelEventHandler(ctxTab_Opening);
            this.mnuEdit.DropDownOpening += new EventHandler(mnuEdit_DropDownOpening);
            this.fileToolStripMenuItem.DropDownOpening += new EventHandler(fileToolStripMenuItem_DropDownOpening);
            this.txtFindNext.KeyDown += new KeyEventHandler(txtFindNext_KeyDown);

            // Setup The Dock Panel...
            this.DockMain.ShowDocumentIcon = false;

            //Skin
            var skin = this.DockMain.Theme.Skin;
            skin.DockPaneStripSkin.DocumentGradient.ActiveTabGradient.LinearGradientMode = System.Drawing.Drawing2D.LinearGradientMode.Vertical;
            skin.DockPaneStripSkin.DocumentGradient.ActiveTabGradient.StartColor = Color.White;
            skin.DockPaneStripSkin.DocumentGradient.ActiveTabGradient.EndColor = Color.FromArgb(215, 235, 255);

            skin.DockPaneStripSkin.DocumentGradient.InactiveTabGradient.LinearGradientMode = System.Drawing.Drawing2D.LinearGradientMode.Vertical;
            skin.DockPaneStripSkin.DocumentGradient.InactiveTabGradient.StartColor = Color.White;
            skin.DockPaneStripSkin.DocumentGradient.InactiveTabGradient.EndColor = Color.FromArgb(152, 180, 210);

            skin.DockPaneStripSkin.DocumentGradient.DockStripGradient.LinearGradientMode = System.Drawing.Drawing2D.LinearGradientMode.Vertical;
            skin.DockPaneStripSkin.DocumentGradient.DockStripGradient.StartColor = Color.White;
            skin.DockPaneStripSkin.DocumentGradient.DockStripGradient.EndColor = SystemColors.ControlDark;

            skin.DockPaneStripSkin.ToolWindowGradient.ActiveCaptionGradient.LinearGradientMode = System.Drawing.Drawing2D.LinearGradientMode.Vertical;
            skin.DockPaneStripSkin.ToolWindowGradient.ActiveCaptionGradient.StartColor = Color.FromArgb(215, 235, 255);
            skin.DockPaneStripSkin.ToolWindowGradient.ActiveCaptionGradient.EndColor = Color.White;

            skin.DockPaneStripSkin.ToolWindowGradient.InactiveCaptionGradient.LinearGradientMode = System.Drawing.Drawing2D.LinearGradientMode.Vertical;
            skin.DockPaneStripSkin.ToolWindowGradient.InactiveCaptionGradient.StartColor = Color.FromArgb(152, 180, 210);
            skin.DockPaneStripSkin.ToolWindowGradient.InactiveCaptionGradient.EndColor = Color.White;

            skin.DockPaneStripSkin.ToolWindowGradient.DockStripGradient.LinearGradientMode = System.Drawing.Drawing2D.LinearGradientMode.Vertical;
            skin.DockPaneStripSkin.ToolWindowGradient.DockStripGradient.StartColor = SystemColors.ControlDark;
            skin.DockPaneStripSkin.ToolWindowGradient.DockStripGradient.EndColor = Color.White;

            skin.DockPaneStripSkin.ToolWindowGradient.ActiveTabGradient.LinearGradientMode = System.Drawing.Drawing2D.LinearGradientMode.Vertical;
            skin.DockPaneStripSkin.ToolWindowGradient.ActiveTabGradient.StartColor = Color.FromArgb(215, 235, 255);
            skin.DockPaneStripSkin.ToolWindowGradient.ActiveTabGradient.EndColor = Color.White;

            skin.DockPaneStripSkin.ToolWindowGradient.InactiveTabGradient.LinearGradientMode = System.Drawing.Drawing2D.LinearGradientMode.Vertical;
            skin.DockPaneStripSkin.ToolWindowGradient.InactiveTabGradient.StartColor = Color.FromArgb(152, 180, 210);
            skin.DockPaneStripSkin.ToolWindowGradient.InactiveTabGradient.EndColor = Color.White;

            skin.AutoHideStripSkin.TabGradient.LinearGradientMode = System.Drawing.Drawing2D.LinearGradientMode.Vertical;
            skin.AutoHideStripSkin.TabGradient.StartColor = Color.White;
            skin.AutoHideStripSkin.TabGradient.EndColor = Color.FromArgb(152, 180, 210);
            skin.AutoHideStripSkin.TabGradient.TextColor = SystemColors.ControlText;

            //Endskin


            this.DockMain.ActiveContentChanged += new EventHandler(DockMain_ActiveContentChanged);
            this.DockMain.ContentRemoved += DockMain_ContentRemoved;

            // Drag N Drop...
            this.DockMain.AllowDrop = true;
            this.DockMain.DragEnter += new DragEventHandler(DockMain_DragEnter);
            this.DockMain.DragDrop += new DragEventHandler(DockMain_DragDrop);

            // Load Highlighting Files...
            this.LoadHighlighting();

            // Load Plugins...
            this.LoadPlugins();

            // Load Configuration...
            if (File.Exists(this.DockConfigFile))
            {
                this.DockMain.LoadFromXml(this.DockConfigFile, new DeserializeDockContent(this.GetContent));
            }

            // Load Files passed by arguments...

            foreach (var s in args)
            {
                if (File.Exists(s))
                {
                    if (Path.GetExtension(s).ToLower().Equals(".pproj"))
                    {
                        this.OpenProject(s);
                    }
                    else
                    {
                        this.CreateEditor(s, Path.GetFileName(s), Common.GetFileIcon(s, false));
                    }
                }
            }
            var edl = new List<Editor>();
            var docs = DockMain.Documents;
            if (docs != null)
            {
                foreach (Editor ed in DockMain.Documents)
                {
                    edl.Add(ed);
                }
                if (docs.Any())
                {
                    MyActiveEditor = edl[0];
                    if (edl.Count > 0) edl[edl.Count - 1].Focus();
                }
            }

            m_AutoComplete.ScriptsPath = m_ScriptsPath;
            Application.Idle += new EventHandler(OnIdle);
        }

        public void OpenFilesInEditor(IEnumerable<string> files)
        {
            foreach (var f in files)
            {
                CreateEditor(f, Path.GetFileName(f), Common.GetFileIcon(f, false));
            }
        }

        protected void OnIdle(object sender, EventArgs e)
        {
            Application.Idle -= new EventHandler(OnIdle);
            initialized = true;
            if (m_ScriptsPath.Length == 0)
            {
                MessageBox.Show("Der Pfad zu den Scripten ist noch nicht gesetzt, bitte stellen Sie den Pfad zum Ordner '_work\\Data\\Scripts' in den Einstellungen ein und starten Sie anschließend Stampfer neu.", "Pfad zu den Scripten unbekannt", MessageBoxButtons.OK, MessageBoxIcon.Information);
                var frm = new Options(this);
                frm.Show();

                for (var a = 0; a < frm.m_OptionPanels.Count; a++)
                {
                    var ctrl = (Control)frm.m_OptionPanels[a];

                    if (ctrl.Name == "Gothic")
                    {
                        frm.splitContainer1.Panel2.Controls.Clear();
                        ctrl.Dock = DockStyle.Fill;
                        frm.splitContainer1.Panel2.Controls.Add(ctrl);
                        break;
                    }
                }
                frm.BtBrowseScripts_Click(null, new EventArgs());
            }

            //Easteregg!!!
            if (MousePosition.X < 10
                && MousePosition.Y < 10)
            {
                var fg = new FighterGame();
                fg.Show();
            }
        }
        public int ToolSort(FileInfo obj1, FileInfo obj2)
        {
            return string.Compare(obj1.Name, obj2.Name, true);
        }

        private void LoadTools()
        {
            var fl = Path.GetDirectoryName(Application.ExecutablePath);
            fl += @"\Tools";
            if (Directory.Exists(fl))
            {
                var di = new DirectoryInfo(fl);
                var finfo = new List<FileInfo>();
                finfo.AddRange(di.GetFiles("*.lnk", SearchOption.AllDirectories));
                finfo.AddRange(di.GetFiles("*.exe", SearchOption.AllDirectories));
                finfo.Sort(new Comparison<FileInfo>(ToolSort));
                foreach (var f in finfo)
                {
                    var m = new ToolStripMenuItem(f.Name.Remove(f.Name.LastIndexOf(".")), Common.GetFileIcon(f.FullName, false).ToBitmap(), new EventHandler(SelectProgram), f.FullName);
                    toolFolderToolStripMenuItem.DropDownItems.Add(m);
                }
            }
        }

        private void SelectProgram(object sender, EventArgs e)
        {
            var s = (ToolStripMenuItem)sender;
            var n = new System.Diagnostics.Process();
            n.StartInfo.FileName = s.Name;
            n.Start();
        }

        private void MainForm_Deactivate(object sender, EventArgs e)
        {
            m_AutoComplete.HidePopup();
        }


        #region -= Load Config file =-

        /// <summary>
        /// Loads the Configuration File...
        /// </summary>
        /// <param name="reload">Reloaded or Not.</param>
        public Classes.Configuration.PeterConfig LoadConfigFile(bool reload)
        {
            var serializer = new XmlSerializer(typeof(Classes.Configuration.PeterConfig));
            Classes.Configuration.PeterConfig configFile;
            using (var fs = File.OpenRead(this.ConfigFile))
            {
                configFile = (Classes.Configuration.PeterConfig)serializer.Deserialize(fs);
            }
            this.m_EditorConfig = configFile.Editor;
            if (!reload)
            {
                this.Top = configFile.Application.Top;
                this.Left = configFile.Application.Left;
                // Re-Position if off Screen...
                var w = 0;
                foreach (var screen in Screen.AllScreens)
                {
                    w += screen.Bounds.Width;
                }
                if (this.Left > w)
                {
                    this.Left -= w;
                }
                this.Width = configFile.Application.Width;
                this.Height = configFile.Application.Height;
                this.m_SaveonExit = configFile.Application.SaveOnExit;
                this.m_RecentFileCount = configFile.Application.RecentFileCount;
                this.m_RecentProjectCount = configFile.Application.RecentProjectCount;


                if (configFile.RecentProjects?.Project != null)
                {
                    foreach (var f in configFile.RecentProjects.Project)
                    {
                        var tsmi = new ToolStripMenuItem
                        {
                            Text = f.Name,
                            Name = f.File
                        };

                        tsmi.Click += new EventHandler(ReopenProject);
                        this.mnuProjectReopen.DropDownItems.Add(tsmi);
                    }
                }
                if (configFile.RecentFiles?.File != null)
                {
                    foreach (var f in configFile.RecentFiles.File)
                    {
                        var tsmi = new ToolStripMenuItem(f);

                        tsmi.Click += new EventHandler(ReopenFile);
                        this.mnuFileOpenRecent.DropDownItems.Add(tsmi);
                    }
                }

            }

            SetAutocompleteMenu();

            if (reload)
            {
                for (var a = 0; a < this.DockMain.Contents.Count; a++)
                {
                    if (this.DockMain.Contents[a].GetType() == typeof(Editor))
                    {
                        ((Editor)this.DockMain.Contents[a]).SetupEditor(this.m_EditorConfig);
                    }
                }

                foreach (IPeterPlugin plugin in this.m_Plugins)
                {
                    plugin.ApplyOptions();
                }

            }
            //Set Autosave
            if (m_BackupEach > 0)
            {
                tSaveTimer.Enabled = true;
                tSaveTimer.Interval = 60000 * m_BackupEach;
            }
            else
            {
                tSaveTimer.Enabled = false;
            }

            return configFile;
        }

        private void SetAutocompleteMenu()
        {
            if (m_EditorConfig.Autocomplete)
            {
                autocompletemenu.Text = autocompletemenuauto;
            }
            else
            {
                autocompletemenu.Text = autocompletemenumanu;
            }
        }

        #endregion

        #region -= Properties =-

        /// <summary>
        /// Gets the path the Application started in...
        /// </summary>
        public string ApplicationExeStartPath => Path.GetDirectoryName(Application.ExecutablePath);

        /// <summary>
        /// Gets the Active Tab Interface...
        /// </summary>
        private IPeterPluginTab ActiveTab => (IPeterPluginTab)this.ActiveContent;

        /// <summary>
        /// Gets the Active Editor...
        /// </summary>
        public Editor ActiveEditor
        {
            get
            {
                if (this.ActiveTab != null)
                {
                    if (this.ActiveContent.GetType() == typeof(Editor))
                    {
                        return (Editor)this.ActiveContent;
                    }
                }
                return null;
            }
        }

        /// <summary>
        /// Gets the Active Content...
        /// </summary>
        public IDockContent ActiveContent { get; private set; }

        /// <summary>
        /// Gets the Type for a Editor in string format (typeof(Editor))...
        /// </summary>
        public string EditorType => typeof(Editor).ToString();

        /// <summary>
        /// Gets the Location of the Application Config File...
        /// </summary>
        public string ConfigFile { get; private set; }

        /// <summary>
        /// Gets the location of the Dock Config File...
        /// </summary>
        public string DockConfigFile { get; private set; }

        #endregion

        #region -= Add Dock Content =-

        /// <summary>
        /// Adds the given Dock Content to the form...
        /// </summary>
        /// <param name="content">Content to Add.</param>
        /// <param name="state">State of Content</param>
        public void AddDockContent(IDockContent content, DockState state)
        {
            if (this.CheckContent(content))
            {
                content.DockHandler.Show(this.DockMain, state);
                content.DockHandler.TabPageContextMenuStrip = this.ctxTab;
            }
        }

        /// <summary>
        /// Checks the given content to see if it implements the IPeterPluginTab Interface...
        /// </summary>
        /// <param name="content">DockContent</param>
        /// <returns>True or False</returns>
        private bool CheckContent(IDockContent content)
        {
            var types = content.GetType().GetInterfaces();
            foreach (var t in types)
            {
                if (t == typeof(IPeterPluginTab))
                {
                    return true;
                }
            }

            MessageBox.Show("'" + content.GetType().ToString() + " kann nicht hinzugefügt werden.' Es implementiert nicht das IPeterPluginTab Interface",
                "Stampfer", MessageBoxButtons.OK, MessageBoxIcon.Error);

            return false;
        }

        #endregion

        #region -= Get Content =-

        /// <summary>
        /// Sets up the Content from last Session...
        /// </summary>
        /// <param name="contentString">Content String</param>
        /// <returns>IDockContent</returns>
        private IDockContent GetContent(string contentString)
        {

            // Find Results...
            if (contentString == typeof(FindResults).ToString())
            {
                this.m_FindControl.Results.TabPageContextMenuStrip = this.ctxTab;
                return this.m_FindControl.Results;
            }

            // Command Prompt...
            if (contentString == typeof(CommandPrompt).ToString())
            {
                var cmd = new CommandPrompt
                {
                    Icon = Icon.FromHandle(((Bitmap)this.GetInternalImage("cmd")).GetHicon()),
                    TabPageContextMenuStrip = this.ctxTab
                };
                return cmd;
            }

            // File Difference...
            if (contentString == typeof(FileDifference).ToString())
            {
                var diff = this.GetNewFileDifference();
                diff.TabPageContextMenuStrip = this.ctxTab;
                return diff;
            }
            if (contentString == typeof(DialogCreator).ToString())
            {
                var diff = new DialogCreator(this)
                {
                    TabPageContextMenuStrip = this.ctxTab
                };
                return diff;
            }

            // Code Structure...
            if (contentString == typeof(ctrlCodeStructure).ToString())
            {
                return this.m_CodeStructure;
            }
            if (contentString == typeof(ctrlGothicInstances).ToString())
            {
                return this.m_GothicStructure;
            }
            if (contentString == typeof(ctrlQuestManager).ToString())
            {
                return this.m_QuestManager;
            }


            // Editor...
            var pSplit = contentString.Split('|');
            if (pSplit.Length == 5)
            {
                if (pSplit[0] == typeof(Editor).ToString())
                {
                    if (File.Exists(pSplit[2]))
                    {
                        var e = this.CreateNewEditor(pSplit[1]);
                        e.LoadFile(pSplit[2]);
                        this.UpdateRecentFileList(pSplit[2]);
                        // We Should'nt need to check for Duplicates...
                        e.Icon = Common.GetFileIcon(pSplit[2], false);
                        e.ScrollTo(Convert.ToInt32(pSplit[3]));
                        e.Project = pSplit[4];
                        return e;
                    }
                    return this.CreateNewEditor(pSplit[1]);
                }
            }

            if (pSplit.Length == 2)
            {
                // File Explorer
                if (pSplit[0] == typeof(ctrlFileExplorer).ToString())
                {
                    var fe = new ctrlFileExplorer(this)
                    {
                        Icon = Icon.FromHandle(((Bitmap)this.GetInternalImage("FEIcon")).GetHicon())
                    };
                    fe.LoadTree(pSplit[1]);
                    fe.TabPageContextMenuStrip = this.ctxTab;
                    return fe;
                }

                // Project Manager...
                if (pSplit[0] == typeof(ProjectManager).ToString())
                {
                    var projs = pSplit[1].Split(';');
                    foreach (var proj in projs)
                    {
                        this.OpenProject(proj);
                    }
                    return this.m_ProjMan;
                }
            }

            // Plugin...
            foreach (IPeterPlugin plugin in this.m_Plugins)
            {

                if (plugin.CheckContentString(contentString))
                {
                    var dc = (DockContent)plugin.GetContent(contentString);

                    dc.TabPageContextMenuStrip = this.ctxTab;
                    return dc;
                }
            }

            // If we return null, the program will crash, so just create an editor...
            this.m_NewCount++;
            return this.CreateNewEditor("Neu" + Convert.ToString(this.m_NewCount - 1));
        }



        #endregion

        #region -= Plugins =-

        /// <summary>
        /// Loads the Plugins in the Plugin Directory...
        /// </summary>
        private void LoadPlugins()
        {
            var pluginFolderPath = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), PLUGIN_FOLDER);
            Directory.CreateDirectory(pluginFolderPath);
            var files = Directory.GetFiles(pluginFolderPath, "*.dll");
            foreach (var file in files)
            {
                this.LoadPlugin(file);
            }
        }

        /// <summary>
        /// Loads a Plugin...
        /// </summary>
        /// <param name="pluginPath">Full Path to Plugin</param>
        /// <returns>True if Plugin Loaded, otherwise false</returns>
        public bool LoadPlugin(string pluginPath)
        {
            Assembly asm;

            if (!File.Exists(pluginPath))
            {
                return false;
            }

            asm = Assembly.LoadFile(pluginPath);
            if (asm != null)
            {
                foreach (var type in asm.GetTypes())
                {
                    if (type.IsAbstract)
                        continue;
                    var attrs = type.GetCustomAttributes(typeof(PeterPluginAttribute), true);
                    if (attrs.Length > 0)
                    {
                        var plugin = Activator.CreateInstance(type) as IPeterPlugin;
                        plugin.Host = this;
                        if (plugin.HasMenu)
                        {
                            this.mnuPlugins.DropDownItems.Add(plugin.GetMenu());
                        }

                        if (plugin.HasTabMenu)
                        {
                            this.ctxTab.Items.Add(new ToolStripSeparator());
                            foreach (var tsmi in plugin.GetTabMenu())
                            {
                                this.ctxTab.Items.Add(tsmi);
                            }
                        }

                        if (plugin.HasContextMenu)
                        {
                            this.ctxEditor.Items.Add(new ToolStripSeparator());
                            foreach (var tsmi in plugin.GetContextMenu())
                            {
                                this.ctxEditor.Items.Add(tsmi);
                            }
                        }
                        this.m_Plugins.Add(plugin);
                        plugin.Start();
                    }
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        #endregion

        #region -= Active Content Changed =-

        /// <summary>
        /// Occurs when the Active Contents has Changed...
        /// </summary>
        /// <param name="sender">Content</param>
        /// <param name="e">Events</param>

        private void DockMain_ActiveContentChanged(object sender, EventArgs e)
        {
            this.mnuHighlighting.Enabled = false;
            this.bookMarksToolStripMenuItem.Enabled = false;
            this.mnuCode.Enabled = false;

            if (this.DockMain.ActiveContent != null)
            {
                // Set the Active content...
                this.ActiveContent = this.DockMain.ActiveContent;

                if (this.DockMain.ActiveContent is Editor editor)
                {
                    MyActiveEditor = editor;
                    this.RemoveHighlightChecks();
                    this.mnuHighlighting.Enabled = true;
                    this.mnuCode.Enabled = true;
                    this.bookMarksToolStripMenuItem.Enabled = true;
                    for (var a = 0; a < this.mnuHighlighting.DropDown.Items.Count; a++)
                    {
                        var tsmi = (ToolStripMenuItem)this.mnuHighlighting.DropDown.Items[a];
                        if (tsmi.Text == editor.Highlighting)
                        {
                            tsmi.Checked = true;
                            break;
                        }
                    }
                    editor.UpdateCaretPos();
                }
                else
                {
                    if (this.DockMain.ActiveDocument == null || this.DockMain.ActiveDocument.GetType() != typeof(Editor))
                    {
                        this.UpdateCaretPos(0, 0, 0, null);
                    }
                }

                foreach (IPeterPlugin plugin in this.m_Plugins)
                {
                    plugin.ActiveContentChanged(this.DockMain.ActiveContent);
                }

                this.UpdateToolBar();
            }
            else
            {
                this.UpdateCaretPos(0, 0, 0, null);
            }
            this.UpdateTitleBar();
        }

        public void UpdateTitleBar()
        {
            if (this.ActiveContent is Editor editor)
            {
                this.Text = editor.TabText + " - Stampfer";
                if (this.m_CodeStructure.TEXT != this.Text && "*" + this.m_CodeStructure.TEXT != this.Text)
                {
                    try
                    {
                        this.m_CodeStructure.TEXT = this.Text;
                        this.m_CodeStructure.Clear();
                        this.m_CodeStructure.ActiveContentChanged(editor, true);
                    }
                    catch (Exception ex)
                    {
                        Log.Line(ex.Message);
                    }
                }
            }
            else
            {
                this.Text = "Stampfer";
            }
        }

        private void DockMain_ContentRemoved(object sender, DockContentEventArgs e)
        {
            if (e?.Content is IPeterPluginTab tab)
            {
                tab.CloseTab();
            }
            this.UpdateTitleBar();
        }

        #endregion

        #region -= Highlighting =-

        /// <summary>
        /// Loads the Highlighting Files...
        /// </summary>
        private void LoadHighlighting()
        {
            var path = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), SCHEME_FOLDER);
            Directory.CreateDirectory(path);
            if (Directory.Exists(path) && Directory.EnumerateFiles(path, "*.xshd").Any())
            {
                HighlightingManager.Manager.AddSyntaxModeFileProvider(new FileSyntaxModeProvider(path));

                var keys = HighlightingManager.Manager.HighlightingDefinitions.Keys;
                var keyArray = new string[keys.Count];
                keys.CopyTo(keyArray, 0);
                Array.Sort(keyArray);
                this.mnuHighlighting.DropDownItems.Clear();
                foreach (var key in keyArray)
                {
                    var tsi = new ToolStripMenuItem(key);
                    tsi.Click += new EventHandler(Highlighter_Click);
                    this.mnuHighlighting.DropDown.Items.Add(tsi);
                }
            }
            else
            {
                MessageBox.Show("Highlighting Schemes können nicht geladen werden!\nEs sind keine Highlighter hinterlegt.", "Stampfer", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Removes all the Check from the Highlighting Menu...
        /// </summary>
        private void RemoveHighlightChecks()
        {
            for (var a = 0; a < this.mnuHighlighting.DropDown.Items.Count; a++)
            {
                var tsmi = (ToolStripMenuItem)this.mnuHighlighting.DropDown.Items[a];
                tsmi.Checked = false;
            }
        }

        /// <summary>
        /// Highlighting menu selection...
        /// </summary>
        /// <param name="sender">Highlighting Menu ToolStripMenuItem</param>
        /// <param name="e">Events</param>
        private void Highlighter_Click(object sender, EventArgs e)
        {
            this.RemoveHighlightChecks();
            if (this.ActiveContent != null)
            {
                if (this.ActiveContent.GetType() == typeof(Editor))
                {
                    var tsmi = sender as ToolStripMenuItem;
                    tsmi.Checked = true;
                    var edit = (Editor)this.ActiveContent;
                    edit.Highlighting = tsmi.Text;
                }
            }
        }

        #endregion

        #region -= Drag N Drop =-

        /// <summary>
        /// Enables files to be dropped in the dock window...
        /// </summary>
        /// <param name="sender">DockPanel</param>
        /// <param name="e">Events</param>
        private void DockMain_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop, false) == true)
            {
                e.Effect = DragDropEffects.All;
            }
        }

        /// <summary>
        /// Grabs the files dropped in the Dock Window...
        /// </summary>
        /// <param name="sender">DockPanel</param>
        /// <param name="e">Events</param>
        private void DockMain_DragDrop(object sender, DragEventArgs e)
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (var file in files)
            {
                this.CreateEditor(file, Path.GetFileName(file));
            }
        }

        #endregion

        #region -= New Document =-

        /// <summary>
        /// Creates a new blank editor...
        /// </summary>
        public void NewDocument()
        {
            this.m_NewCount++;
            var e = this.CreateNewEditor("Neu" + this.m_NewCount.ToString());
            e.Show(this.DockMain);

        }






        #endregion

        #region -= Open =-

        /// <summary>
        /// Displays the Open file Dialog to get files to edit...
        /// </summary>
        private void Open()
        {
            try { this.ofdMain.FileName = ((Editor)this.DockMain.ActiveContent).FileName; }
            catch { };
            if (this.ofdMain.ShowDialog() == DialogResult.OK)
            {
                foreach (var file in this.ofdMain.FileNames)
                {
                    this.CreateEditor(file, Path.GetFileName(file));
                }
            }
        }

        #endregion

        #region -= Save =-

        /// <summary>
        /// Save the Current Pane...
        /// </summary>
        private void Save()
        {
            Editor current = null;
            var tab = this.ActiveTab;
            if (tab.FileName == null)
            {
                this.SaveAs(tab);
                this.UpdateTitleBar();
                return;
            }
            else
            {
                tab.Save();

            }
            if (this.m_CodeStructure != null) m_CodeStructure.treeMain.BeginUpdate();
            //::m_GothicStructure.BeginTreeUpdate();
            var l = UpdateInstances(tab, ref current);
            if (current != null && this.m_CodeStructure != null)
            {
                this.m_CodeStructure.ActiveContentChanged(current, true);
                m_CodeStructure.treeMain.EndUpdate();
            }

            if ((this.m_GothicStructure != null))
            {

                //::m_GothicStructure.EndTreeUpdate();
                if (l > 0)
                    this.m_GothicStructure.SetAutoCompleteContent();

            }


            this.UpdateTitleBar();
        }

        /// <summary>
        /// Saves the Given Content As...
        /// </summary>
        /// <param name="tab">Content to Save</param>
        public void SaveAs(IPeterPluginTab tab)
        {
            Editor current = null;
            if (this.sfdMain.ShowDialog() == DialogResult.OK)
            {
                tab.SaveAs(this.sfdMain.FileName);
            }
            //::m_GothicStructure.BeginTreeUpdate();
            if (this.m_CodeStructure != null) m_CodeStructure.treeMain.BeginUpdate();
            var l = UpdateInstances(tab, ref current);
            if (current != null && this.m_CodeStructure != null)
            {

                this.m_CodeStructure.ActiveContentChanged(current, true);
                m_CodeStructure.treeMain.EndUpdate();
            }

            if ((this.m_GothicStructure != null))
            {
                //m_GothicStructure.UpdateAllTrees(k);
                //::m_GothicStructure.EndTreeUpdate();
                if (l > 0)
                    this.m_GothicStructure.SetAutoCompleteContent();

            }

            this.UpdateTitleBar();

        }

        private int UpdateInstances(IPeterPluginTab tab, ref Editor current)
        {
            var k = 0;
            if (this.m_CodeStructure != null
                    && this.m_GothicStructure != null
                    && tab is Editor)
            {
                this.m_CodeStructure.Clear();

                if (this.MyActiveEditor.TabText == tab.TabText)
                //(((Editor)(this.ActiveContent)).TabText == ((Editor)(tab)).TabText))
                {

                    current = (Editor)tab;
                }
                if (tab.FileName == null) return 0;
                this.m_CodeStructure.ActiveContentChanged((Editor)tab, false);
                if (tab.FileName.Contains(FilePaths.ContentItems))
                {
                    foreach (var i in m_CodeStructure.lInstances)
                    {
                        k |= m_GothicStructure.AddItem(i.Name, tab.FileName);
                        //::m_GothicStructure.AddToItemTree(i.Name, tab.FileName, k);
                    }

                }
                else if (tab.FileName.Contains(FilePaths.ContentNPC))
                {
                    foreach (var i in m_CodeStructure.lInstances)
                    {
                        k |= m_GothicStructure.AddNPC(i.Name, tab.FileName);
                        //::m_GothicStructure.AddToNPCTree(i.Name, tab.FileName, k);
                    }
                }
                else if (tab.FileName.Contains(FilePaths.ContentDialoge))
                {
                    foreach (var i in m_CodeStructure.lInstances)
                    {
                        k |= m_GothicStructure.AddDia(i.Name, tab.FileName);
                        //::m_GothicStructure.AddToDialogTree(i.Name, tab.FileName, k);
                    }
                }

                foreach (var i in m_CodeStructure.lFuncs)//TODO
                {
                    k |= m_GothicStructure.AddFunc(i.Name, tab.FileName);
                    //::m_GothicStructure.AddToFuncTree(i.Name + "()", tab.FileName, k);
                }
                foreach (var i in m_CodeStructure.lVars)
                {
                    k |= m_GothicStructure.AddVar(i.Name, tab.FileName);
                    //::m_GothicStructure.AddToVarTree(i.Name, tab.FileName, k);
                }
                //MessageBox.Show(m_CodeStructure.lConsts.Count.ToString());
                foreach (var i in m_CodeStructure.lConsts)
                {
                    k |= m_GothicStructure.AddConst(i.Name, tab.FileName);
                    //::m_GothicStructure.AddToConstTree(i.Name, tab.FileName, k);
                }


            }
            return k;
        }
        /// <summary>
        /// Saves all of the Contents...
        /// </summary>
        private void SaveAll()
        {

            var l = 0;
            Editor current = null;
            //::if (this.m_GothicStructure != null) m_GothicStructure.BeginTreeUpdate();
            if (this.m_CodeStructure != null) m_CodeStructure.treeMain.BeginUpdate();

            for (var a = 0; a < this.DockMain.Contents.Count; a++)
            {
                var tab = (IPeterPluginTab)this.DockMain.Contents[a];

                if (tab.FileName == null)
                {
                    this.SaveAs(tab);
                }
                else
                {
                    tab.Save();
                }

                l |= UpdateInstances(tab, ref current);


                /* if (m_GothicStructure != null
                     && tab is Editor)
                 {
                     //MessageBox.Show(tab.FileName);
                     k |= m_GothicStructure.GetAll(tab.FileName);                    
                 }*/
            }
            if (current != null)
            {
                this.m_CodeStructure.ActiveContentChanged(current, true);
            }

            if (this.m_CodeStructure != null) m_CodeStructure.treeMain.EndUpdate();

            if ((this.m_GothicStructure != null))
            {
                //m_GothicStructure.UpdateAllTrees(k);
                //::m_GothicStructure.EndTreeUpdate();
                if (l > 0)
                    this.m_GothicStructure.SetAutoCompleteContent();

            }
            //MessageBox.Show(k.ToString());
            /*
                        if (m_GothicStructure != null)
                        {
                            m_GothicStructure.UpdateAllTrees(k);
                        }
                        MessageBox.Show("!");*/
            /*if (this.m_CodeStructure !=null)
            {
                this.m_CodeStructure.Clear();
                this.m_CodeStructure.ActiveContentChanged((Editor)this.ActiveContent);
            }*/



        }

        #endregion

        #region -= Edit =-

        /// <summary>
        /// Clipboard Cut Action...
        /// </summary>
        private void Cut()
        {
            this.ActiveTab.Cut();
        }

        /// <summary>
        /// Clipboard Copy Action...
        /// </summary>
        private void Copy()
        {
            this.ActiveTab.Copy();
        }

        /// <summary>
        /// Clipboard Paste Action...
        /// </summary>
        private void Paste()
        {

            this.ActiveTab.Paste();
        }

        /// <summary>
        /// Clipboard Delete Action...
        /// </summary>
        private void Delete()
        {
            this.ActiveTab.Delete();
        }

        /// <summary>
        /// Select All Action...
        /// </summary>
        private void SelectAll()
        {
            this.ActiveTab.SelectAll();
        }

        /// <summary>
        /// Edit Undo Action...
        /// </summary>
        private void Undo()
        {
            this.ActiveTab.Undo();
        }

        /// <summary>
        /// Edit Redo Action...
        /// </summary>
        private void Redo()
        {
            this.ActiveTab.Redo();
        }

        #endregion

        #region -= Create Editor =-

        /// <summary>
        /// Creates a new Editor with the given file...
        /// </summary>
        /// <param name="fileName">File to load in Editor.</param>
        /// <param name="tabName">Name of Tab.</param>
        public void CreateEditor(string fileName, string tabName)
        {
            this.CreateEditor(fileName, tabName, Common.GetFileIcon(fileName, false));
        }

        /// <summary>
        /// Creates a new Editor with the given file...
        /// </summary>
        /// <param name="fileName">File to load in Editor.</param>
        /// <param name="tabName">Name of Tab.</param>
        /// <param name="image">Icon for Tab.</param>
        public void CreateEditor(string fileName, string tabName, Icon image)
        {
            this.CreateEditor(fileName, tabName, image, null);
        }

        /// <summary>
        /// Creates a new Editor with the given file...
        /// </summary>
        /// <param name="fileName">File to load in Editor.</param>
        /// <param name="tabName">Name of Tab.</param>
        /// <param name="image">Icon for Tab.</param>
        public void CreateEditor(string fileName, string tabName, Icon image, IDockContent addToContent)
        {
            try
            {
                if (File.Exists(fileName))
                {
                    // Add to Recent Files...
                    this.UpdateRecentFileList(fileName);

                    // Let the plugins try to load the file first...
                    foreach (IPeterPlugin plugin in this.m_Plugins)
                    {
                        if (plugin.AbleToLoadFiles)
                        {
                            if (plugin.LoadFile(fileName))
                            {
                                return;
                            }
                        }
                    }

                    // No plugins want the file, we can load it...
                    if (!this.IsFileOpen(fileName))
                    {
                        var e = this.CreateNewEditor(tabName);
                        e.ShowIcon = true;
                        e.Icon = image;
                        e.LoadFile(fileName);
                        if (addToContent == null)
                        {
                            if (this.DockMain.ActiveDocumentPane != null)
                            {
                                e.Show(this.DockMain.ActiveDocumentPane, null);
                            }
                            else
                            {
                                e.Show(this.DockMain);
                            }
                        }
                        else
                        {
                            e.Show(addToContent.DockHandler.Pane, null);
                        }
                        e.Activate();
                    }
                    else
                    {
                        for (var a = this.DockMain.Contents.Count - 1; a >= 0; a--)
                        {
                            var tab = (IPeterPluginTab)this.DockMain.Contents[a];
                            if (tab.FileName == fileName)
                            {
                                this.DockMain.Contents[a].DockHandler.Show();
                            }
                        }
                    }
                }
                else
                {
                    MessageBox.Show(fileName + " existiert nicht", "Stampfer", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (IOException ex)
            {
                MessageBox.Show(ex.Message, "Stampfer", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public Editor GetEditor(string fileName)
        {
            for (var a = this.DockMain.Contents.Count - 1; a >= 0; a--)
            {
                var tab = (IPeterPluginTab)this.DockMain.Contents[a];
                if (tab.FileName == fileName)
                {
                    return (Editor)tab;
                }
            }

            return null;
        }

        /// <summary>
        /// Checks to see if a file is alread open...
        /// </summary>
        /// <param name="file">File to check.</param>
        /// <returns>True if open, else false</returns>
        private bool IsFileOpen(string file)
        {
            for (var a = 0; a < this.DockMain.Contents.Count; a++)
            {
                var tab = (IPeterPluginTab)this.DockMain.Contents[a];
                if (file.Equals(tab.FileName, StringComparison.CurrentCultureIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        private void UpdateRecentFileList(string filePath)
        {
            // Add Menu Item if Needed...
            for (var a = 0; a < this.mnuFileOpenRecent.DropDownItems.Count; a++)
            {
                if (this.mnuFileOpenRecent.DropDownItems[a].Text == filePath)
                {
                    // File is already in list...
                    return;
                }
            }

            var tsmi = new ToolStripMenuItem(filePath);
            tsmi.Click += new EventHandler(ReopenFile);
            this.mnuFileOpenRecent.DropDownItems.Add(tsmi);

            // Update Config file...
            if (Config.RecentFiles.File.Contains(filePath))
            {
                Config.RecentFiles.File.Remove(filePath);
            }
            Config.RecentFiles.File.Add(filePath);
            while (Config.RecentFiles.File.Count > Config.Application.RecentFileCount)
            {
                Config.RecentFiles.File.RemoveAt(0);
            }
            SaveConfig();
        }

        #endregion

        #region -= Create New Editor =-

        /// <summary>
        /// Creates a new Editor with the given tab Name...
        /// </summary>
        /// <param name="tabName">Name to put on tab.</param>
        /// <returns>Newly created Editor.</returns>
        public Editor CreateNewEditor(string tabName)
        {
            var e = new Editor(tabName, this)
            {
                Host = this,
                TabPageContextMenuStrip = this.ctxTab
            };
            e.SetContextMenuStrip(this.ctxEditor);
            e.SetupEditor(this.m_EditorConfig);


            return e;
        }

        #endregion

        #region -= Get File Icon =-

        /// <summary>
        /// Gets the Shell Icon for the given file...
        /// </summary>
        /// <param name="filePath">Path to File.</param>
        /// <param name="linkOverlay">Link Overlay or not.</param>
        /// <returns>Shell Icon for File.</returns>
        public Icon GetFileIcon(string filePath, bool linkOverlay)
        {
            return Common.GetFileIcon(filePath, linkOverlay);
        }

        #endregion

        #region -= Get Internal Image =-

        /// <summary>
        /// Gets an Image from the InternalImages resource file...
        /// </summary>
        /// <param name="imageName">Name of Image</param>
        /// <returns>Image</returns>
        public Image GetInternalImage(string imageName)
        {
            var mngr = new System.Resources.ResourceManager("Peter.InternalImages", this.GetType().Assembly);
            return (Image)mngr.GetObject(imageName);
        }

        #endregion

        #region -= Trace =-

        /// <summary>
        /// Writes the given text in the status bar...
        /// </summary>
        /// <param name="text">Text to Write.</param>
        public void Trace(string text)
        {
            if (!InvokeRequired)
                this.sslMain.Text = text;
            else
                this.Invoke((Action)(() => Trace(text)));
        }

        #endregion

        #region -= Tool Bar =-

        /// <summary>
        /// Creates a new Blank Editor...
        /// </summary>
        /// <param name="sender">ToolStripButton</param>
        /// <param name="e">Events</param>
        private void TsbNew_Click(object sender, EventArgs e)
        {
            this.NewDocument();
        }

        /// <summary>
        /// Opens a Documnet...
        /// </summary>
        /// <param name="sender">ToolStripButton</param>
        /// <param name="e">Events</param>
        private void TsbOpen_Click(object sender, EventArgs e)
        {
            this.Open();
        }

        /// <summary>
        /// Save the Current Document...
        /// </summary>
        /// <param name="sender">ToolStripButton</param>
        /// <param name="e">Events</param>
        private void TsbSave_Click(object sender, EventArgs e)
        {

            this.Save();
        }

        /// <summary>
        /// Saves all of the Open Documents...
        /// </summary>
        /// <param name="sender">ToolStripButton</param>
        /// <param name="e">Events</param>
        private void TsbSaveAll_Click(object sender, EventArgs e)
        {
            this.SaveAll();
        }

        /// <summary>
        /// Clipboard Cut Action...
        /// </summary>
        /// <param name="sender">ToolStripButton</param>
        /// <param name="e">Events</param>
        private void TsbCut_Click(object sender, EventArgs e)
        {
            this.Cut();
        }

        /// <summary>
        /// Clipboard Copy Action...
        /// </summary>
        /// <param name="sender">ToolStripButton</param>
        /// <param name="e">Events</param>
        private void TsbCopy_Click(object sender, EventArgs e)
        {
            this.Copy();
        }

        /// <summary>
        /// Clipboard Paste Action...
        /// </summary>
        /// <param name="sender">ToolStripButton</param>
        /// <param name="e">Events</param>
        private void TsbPaste_Click(object sender, EventArgs e)
        {
            this.Paste();
        }

        /// <summary>
        /// Edit Undo Action...
        /// </summary>
        /// <param name="sender">ToolStripButton</param>
        /// <param name="e">Events</param>
        private void TsbUndo_Click(object sender, EventArgs e)
        {
            this.Undo();
        }

        /// <summary>
        /// Edit Redo Action...
        /// </summary>
        /// <param name="sender">ToolStripButton</param>
        /// <param name="e">Events</param>
        private void TsbRedo_Click(object sender, EventArgs e)
        {
            this.Redo();
        }

        /// <summary>
        /// Prints the Active Content...
        /// </summary>
        /// <param name="sender">ToolStripButton</param>
        /// <param name="e">Events</param>
        private void TsbPrint_Click(object sender, EventArgs e)
        {
            this.ActiveTab.Print();
        }

        #endregion

        #region -= File Menu =-

        /// <summary>
        /// Show the file Difference Dialog...
        /// </summary>
        /// <param name="sender">ToolStripMenuItem</param>
        /// <param name="e">Events</param>
        private void mnuFileDifference_Click(object sender, EventArgs e)
        {

            this.AddDockContent(this.GetNewFileDifference(), DockState.Document);
        }

        /// <summary>
        /// Creates a new Difference Content...
        /// </summary>
        /// <returns>New Difference Content.</returns>
        private FileDifference GetNewFileDifference()
        {
            var fileList = "";
            var selectedFile = (this.ActiveTab != null) ? (!string.IsNullOrEmpty(this.ActiveTab.FileName)) ? this.ActiveTab.FileName : "" : "";
            for (var a = 0; a < this.DockMain.Contents.Count; a++)
            {
                var tab = (IPeterPluginTab)this.DockMain.Contents[a];
                if (!string.IsNullOrEmpty(tab.FileName))
                {
                    fileList += tab.FileName + ";";
                }
            }

            var diff = new FileDifference(fileList.Split(';'), selectedFile)
            {
                Icon = Icon.FromHandle(((Bitmap)this.GetInternalImage("Diff")).GetHicon())
            };

            return diff;
        }

        /// <summary>
        /// Creates a new Blank Editor...
        /// </summary>
        /// <param name="sender">ToolStripMenuItem</param>
        /// <param name="e">Events</param>
        private void mnuFileNew_Click(object sender, EventArgs e)
        {
            this.NewDocument();
        }

        /// <summary>
        /// Opens a Documnet...
        /// </summary>
        /// <param name="sender">ToolStripMenuItem</param>
        /// <param name="e">Events</param>
        private void mnuFileOpen_Click(object sender, EventArgs e)
        {
            this.Open();
        }

        /// <summary>
        /// Save the Current Document...
        /// </summary>
        /// <param name="sender">ToolStripMenuItem</param>
        /// <param name="e">Events</param>
        private void mnuFileSave_Click(object sender, EventArgs e)
        {
            this.Save();
        }

        /// <summary>
        /// Save the Current Document As...
        /// </summary>
        /// <param name="sender">ToolStripMenuItem</param>
        /// <param name="e">Events</param>
        private void mnuFileSaveAs_Click(object sender, EventArgs e)
        {
            this.SaveAs((IPeterPluginTab)this.ActiveContent);
        }

        /// <summary>
        /// Saves all of the Open Documents...
        /// </summary>
        /// <param name="sender">ToolStripMenuItem</param>
        /// <param name="e">Events</param>
        private void mnuFileSaveAll_Click(object sender, EventArgs e)
        {
            this.SaveAll();
        }

        /// <summary>
        /// Exits the Program
        /// </summary>
        /// <param name="sender">ToolStripMenuItem</param>
        /// <param name="e">Events</param>
        private void mnuFileExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// Reopens the Given File...
        /// </summary>
        /// <param name="sender">ToolStripMenuItem</param>
        /// <param name="e">Events</param>
        private void ReopenFile(object sender, EventArgs e)
        {

            var tsmi = sender as ToolStripMenuItem;
            this.CreateEditor(tsmi.Text, Path.GetFileName(tsmi.Text));
        }

        #endregion

        #region -= Edit Menu =-

        /// <summary>
        /// Edit Undo Action...
        /// </summary>
        /// <param name="sender">ToolStripMenuItem</param>
        /// <param name="e">Events</param>
        private void mnuEditUndo_Click(object sender, EventArgs e)
        {
            this.Undo();
        }

        /// <summary>
        /// Edit Redo Action...
        /// </summary>
        /// <param name="sender">ToolStripMenuItem</param>
        /// <param name="e">Events</param>
        private void mnuEditRedo_Click(object sender, EventArgs e)
        {
            this.Redo();
        }

        /// <summary>
        /// Clipboard Cut Action...
        /// </summary>
        /// <param name="sender">ToolStripMenuItem</param>
        /// <param name="e">Events</param>
        private void mnuEditCut_Click(object sender, EventArgs e)
        {
            this.Cut();
        }

        /// <summary>
        /// Clipboard Copy Action...
        /// </summary>
        /// <param name="sender">ToolStripMenuItem</param>
        /// <param name="e">Events</param>
        private void mnuEditCopy_Click(object sender, EventArgs e)
        {
            this.Copy();
        }

        /// <summary>
        /// Clipboard Paste Action...
        /// </summary>
        /// <param name="sender">ToolStripMenuItem</param>
        /// <param name="e">Events</param>
        private void mnuEditPaste_Click(object sender, EventArgs e)
        {
            this.Paste();
        }

        /// <summary>
        /// Duplicates the current selection...
        /// </summary>
        /// <param name="sender">ToolStripMenuItem</param>
        /// <param name="e">Events</param>
        private void duplicateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.ActiveTab != null)
                this.ActiveTab.Duplicate();
        }

        /// <summary>
        /// Clipboard Delete Action...
        /// </summary>
        /// <param name="sender">ToolStripMenuItem</param>
        /// <param name="e">Events</param>
        private void mnuEditDelete_Click(object sender, EventArgs e)
        {
            this.Delete();
        }

        /// <summary>
        /// Selects All of the Text in the Current Document...
        /// </summary>
        /// <param name="sender">ToolStripMenuItem</param>
        /// <param name="e">Events</param>
        private void mnuEditSelectAll_Click(object sender, EventArgs e)
        {
            this.SelectAll();
        }

        #endregion

        #region -= Editor Context Menu =-

        /// <summary>
        /// Edit Undo Action...
        /// </summary>
        /// <param name="sender">ToolStirpMenuItem</param>
        /// <param name="e">Events</param>
        private void ctxUndo_Click(object sender, EventArgs e)
        {
            this.Undo();
        }

        /// <summary>
        /// Edit Redo Action...
        /// </summary>
        /// <param name="sender">ToolStirpMenuItem</param>
        /// <param name="e">Events</param>
        private void ctxRedo_Click(object sender, EventArgs e)
        {
            this.Redo();
        }

        /// <summary>
        /// Clipboard Cut Action...
        /// </summary>
        /// <param name="sender">ToolStirpMenuItem</param>
        /// <param name="e">Events</param>
        private void ctxCut_Click(object sender, EventArgs e)
        {
            this.Cut();
        }

        /// <summary>
        /// Clipboard Copy Action...
        /// </summary>
        /// <param name="sender">ToolStirpMenuItem</param>
        /// <param name="e">Events</param>
        private void ctxCopy_Click(object sender, EventArgs e)
        {
            this.Copy();
        }

        /// <summary>
        /// Clipboard Paste Action...
        /// </summary>
        /// <param name="sender">ToolStirpMenuItem</param>
        /// <param name="e">Events</param>
        private void ctxPaste_Click(object sender, EventArgs e)
        {

            this.Paste();
        }

        /// <summary>
        /// Clipboard Delete Action...
        /// </summary>
        /// <param name="sender">ToolStirpMenuItem</param>
        /// <param name="e">Events</param>
        private void ctxDelete_Click(object sender, EventArgs e)
        {
            this.Delete();
        }

        /// <summary>
        /// Selects All Text in the Current Document...
        /// </summary>
        /// <param name="sender">ToolStirpMenuItem</param>
        /// <param name="e">Events</param>
        private void ctxSelectAll_Click(object sender, EventArgs e)
        {
            this.SelectAll();
        }

        /// <summary>
        /// Action before Editor Menu Opens...
        /// </summary>
        /// <param name="sender">Editor Context Menu</param>
        /// <param name="e">Events</param>
        private void ctxEditor_Opening(object sender, CancelEventArgs e)
        {
            /* this.ctxCut.Enabled = ((this.ActiveTab.Selection.Length > 0) && this.ActiveTab.AbleToCut);
             this.ctxCopy.Enabled = ((this.ActiveTab.Selection.Length > 0) && this.ActiveTab.AbleToCopy);
             this.ctxDelete.Enabled = ((this.ActiveTab.Selection.Length > 0) && this.ActiveTab.AbleToDelete);
             this.ctxPaste.Enabled = (Clipboard.ContainsText() && this.ActiveTab.AbleToPaste);
             this.ctxRedo.Enabled = this.ActiveTab.AbleToRedo;
             this.ctxUndo.Enabled = this.ActiveTab.AbleToUndo;
             this.ctxSelectAll.Enabled = this.ActiveTab.AbleToSelectAll;*/
        }

        /// <summary>
        /// Action before Edit Menu Opens...
        /// </summary>
        /// <param name="sender">Edit Menu</param>
        /// <param name="e">Events</param>
        private void mnuEdit_DropDownOpening(object sender, EventArgs e)
        {
            if (this.ActiveContent != null)
            {
                this.mnuEditCut.Enabled = ((this.ActiveTab.Selection.Length > 0) && this.ActiveTab.AbleToCut);
                this.mnuEditCopy.Enabled = ((this.ActiveTab.Selection.Length > 0) && this.ActiveTab.AbleToCopy);
                this.mnuEditDelete.Enabled = ((this.ActiveTab.Selection.Length > 0) && this.ActiveTab.AbleToDelete);
                this.mnuEditPaste.Enabled = this.ActiveTab.AbleToPaste;
                this.mnuEditRedo.Enabled = this.ActiveTab.AbleToRedo;
                this.mnuEditUndo.Enabled = this.ActiveTab.AbleToUndo;
                this.mnuEditSelectAll.Enabled = this.ActiveTab.AbleToSelectAll;
            }
            else
            {
                this.mnuEditCut.Enabled = this.mnuEditCopy.Enabled = this.mnuEditDelete.Enabled = false;
                this.mnuEditPaste.Enabled = false;
                this.mnuEditRedo.Enabled = false;
                this.mnuEditUndo.Enabled = false;
                this.mnuEditSelectAll.Enabled = false;
            }
        }

        /// <summary>
        /// Action before File Menu Opens...
        /// </summary>
        /// <param name="sender">File Menu</param>
        /// <param name="e">Events</param>
        private void fileToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            if (this.ActiveTab != null)
            {
                this.mnuFileSave.Enabled = this.mnuFileSaveAs.Enabled = this.ActiveTab.AbleToSave;
            }
            else
            {
                this.mnuFileSave.Enabled = this.mnuFileSaveAs.Enabled = false;
            }
        }

        #endregion

        #region -= Tab Context Menu =-

        /// <summary>
        /// Saves the Current Document...
        /// </summary>
        /// <param name="sender">ToolStripMenuItem</param>
        /// <param name="e">Events</param>
        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Save();
        }

        /// <summary>
        /// Closes the Current Document...
        /// </summary>
        /// <param name="sender">ToolStripMenuItem</param>
        /// <param name="e">Events</param>
        private void ctsClose_Click(object sender, EventArgs e)
        {

            this.ActiveTab.CloseTab();
        }

        /// <summary>
        /// Closes all but the Current Document...
        /// </summary>
        /// <param name="sender">ToolStripMenuItem</param>
        /// <param name="e">Events</param>
        private void ctxCloseAllBut_Click(object sender, EventArgs e)
        {
            for (var a = this.DockMain.Contents.Count - 1; a >= 0; a--)
            {
                if (this.DockMain.Contents[a].DockHandler.DockState == DockState.Document)
                {
                    var tab = (IPeterPluginTab)this.DockMain.Contents[a];
                    if (tab != this.ActiveTab)
                    {
                        tab.CloseTab();
                    }
                }
            }
        }

        /// <summary>
        /// Copys the path of the Current Document...
        /// </summary>
        /// <param name="sender">ToolStripMenuItem</param>
        /// <param name="e">Events</param>
        private void copyPathToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.ActiveTab.FileName != null && this.ActiveTab.FileName != string.Empty)
            {
                Clipboard.SetText(this.ActiveTab.FileName);
            }
        }

        /// <summary>
        /// Opens the Folder containing the Current Document...
        /// </summary>
        /// <param name="sender">ToolStripMenuItem</param>
        /// <param name="e">Events</param>
        private void openFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.ActiveTab.FileName != null && this.ActiveTab.FileName != string.Empty)
            {
                System.Diagnostics.Process.Start("explorer.exe",
                    Path.GetDirectoryName(this.ActiveTab.FileName));
            }
        }

        /// <summary>
        /// Event Before Tab Context Menu is opened
        /// </summary>
        /// <param name="sender">Tab Context Menu Strip</param>
        /// <param name="e">Events</param>
        private void ctxTab_Opening(object sender, CancelEventArgs e)
        {
            this.copyPathToolStripMenuItem.Enabled =
                this.openFolderToolStripMenuItem.Enabled = (this.ActiveTab.FileName != null);
        }

        #endregion

        #region -= On Closing =-

        /// <summary>
        /// Intercepts the Closing Action to do some clean up items...
        /// </summary>
        /// <param name="e">Cancel Events</param>
        protected override void OnClosing(CancelEventArgs e)
        {
            foreach (IPeterPluginTab tab in DockMain.Contents.Reverse().ToList())
            {
                if (tab.AbleToSave && tab.NeedsSaving)
                {
                    e.Cancel = !tab.CloseTab();
                }
            }

            if (!e.Cancel)
            {
                // Save Dock Layout...
                if (this.m_SaveonExit)
                {
                    this.DockMain.SaveAsXml(this.DockConfigFile);
                }
                else
                {
                    if (File.Exists(this.DockConfigFile))
                    {
                        File.Delete(this.DockConfigFile);
                    }
                }

                // Save Location...
                if (File.Exists(this.ConfigFile) && this.WindowState != FormWindowState.Minimized)
                {
                    Config.Application.Top = Top;
                    Config.Application.Left = Left;
                    Config.Application.Width = Width;
                    Config.Application.Height = Height;
                    Config.Application.SaveOnExit = m_SaveonExit;
                    SaveConfig();
                }

                foreach (IPeterPlugin plugin in this.m_Plugins)
                {
                    plugin.Close();
                }
            }
            m_AutoComplete.SaveKW();
            base.OnClosing(e);
        }

        public void SaveConfig()
        {
            var serializer = new XmlSerializer(typeof(Classes.Configuration.PeterConfig));
            using (var fs = File.Create(ConfigFile))
            {
                serializer.Serialize(fs, Config);
            }
        }

        #endregion

        #region -= Update Tool Bar =-

        /// <summary>
        /// Updates the Buttons on the tool bar...
        /// </summary>
        public void UpdateToolBar()
        {
            this.tsbSave.Enabled = this.ActiveTab.AbleToSave;
            this.tsbCut.Enabled = this.ActiveTab.AbleToCut;
            this.tsbCopy.Enabled = this.ActiveTab.AbleToCopy;
            this.tsbPaste.Enabled = this.ActiveTab.AbleToPaste;
        }

        #endregion

        #region -= Book Mark Menu =-

        /// <summary>
        /// Toggles a book mark on the active editor...
        /// </summary>
        /// <param name="sender">ToolStripMenuItem</param>
        /// <param name="e">Events</param>
        private void mnuBookMarkToggle_Click(object sender, EventArgs e)
        {
            if (this.ActiveEditor != null)
            {
                this.ActiveEditor.ToggleMark();
            }
        }

        /// <summary>
        /// Removes all book marks on the active editor...
        /// </summary>
        /// <param name="sender">ToolStripMenuItem</param>
        /// <param name="e">Events</param>
        private void mnuBookMarkRemoveAll_Click(object sender, EventArgs e)
        {
            if (this.ActiveEditor != null)
            {
                this.ActiveEditor.RemoveAllMarks();
            }
        }

        /// <summary>
        /// Goes to the next book mark on the active editor...
        /// </summary>
        /// <param name="sender">ToolStripMenuItem</param>
        /// <param name="e">Events</param>
        private void mnuBookMarkNext_Click(object sender, EventArgs e)
        {
            if (this.ActiveEditor != null)
            {
                this.ActiveEditor.GotoMark(true);
            }
        }

        /// <summary>
        /// Goes to the Previous book mark on the active editor...
        /// </summary>
        /// <param name="sender">ToolStripMenuItem</param>
        /// <param name="e">Events</param>
        private void mnuBookMarkPrevious_Click(object sender, EventArgs e)
        {
            if (this.ActiveEditor != null)
            {
                this.ActiveEditor.GotoMark(false);
            }
        }

        #endregion

        #region -= Help Menu =-

        /// <summary>
        /// Shows the About Form...
        /// </summary>
        /// <param name="sender">ToolStipMenuItem</param>
        /// <param name="e">Events</param>
        private void mnuHelpAbout_Click(object sender, EventArgs e)
        {
            //SplashScreen ss = new SplashScreen(true);
            //ss.ShowDialog();
            var i = new Info();
            i.ShowDialog();
        }

        #endregion

        #region -= Find/Replace =-

        /// <summary>
        /// Activates the Find Dialog...
        /// </summary>
        /// <param name="sender">ToolStripMenuItem</param>
        /// <param name="e">Events</param>
        private void mnuSearchFind_Click(object sender, EventArgs e)
        {
            // Set Dialog for Find...
            this.ShowFindDialog();
            this.m_FindControl.SetFind(false);
        }

        /// <summary>
        /// Finds the Next Occurance of the given Pattern
        /// </summary>
        /// <param name="sender">ToolStripMenuItem</param>
        /// <param name="e">Events</param>
        private void mnuSearchFindNext_Click(object sender, EventArgs e)
        {
            this.FindNext(false);
        }

        private void findPreviousToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.FindNext(true);
        }

        /// <summary>
        /// Finds the Next Occurance of the given Pattern
        /// </summary>
        /// <param name="sender">ToolStripMenuItem</param>
        /// <param name="e">Events</param>
        private void findInFilesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Set Dialog for Find...
            this.ShowFindDialog();
            this.m_FindControl.SetFind(true);
        }

        /// <summary>
        /// Finds the Next Occurance of the given Pattern
        /// </summary>
        /// <param name="sender">ToolStripMenuItem</param>
        /// <param name="e">Events</param>
        private void mnuSearchReplace_Click(object sender, EventArgs e)
        {
            this.ShowFindDialog();
            this.m_FindControl.SetReplace(false);
        }

        /// <summary>
        /// Finds the Next Occurance of the given Pattern
        /// </summary>
        /// <param name="sender">ToolStripMenuItem</param>
        /// <param name="e">Events</param>
        private void replaceNextToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.ReplaceNext();
        }

        /// <summary>
        /// Finds the Next Occurance of the given Pattern
        /// </summary>
        /// <param name="sender">ToolStripMenuItem</param>
        /// <param name="e">Events</param>
        private void replaceInFilesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.ShowFindDialog();
            this.m_FindControl.SetReplace(true);
        }

        /// <summary>
        /// Activates the Find Next Method...
        /// </summary>
        /// <param name="sender">ToolStripButton</param>
        /// <param name="e">Events</param>
        private void tsbFind_Click(object sender, EventArgs e)
        {
            //Easteregg!!!!
            if (!string.IsNullOrEmpty(this.txtFindNext.Text))
            {
                if (this.txtFindNext.Text == "Kakulukiam")
                {
                    var gm = new Game();
                    gm.Show();
                    gm.BringToFront();
                    gm.Select();
                    return;
                }
                this.m_FindControl.SetFind(false);
                this.m_FindControl.FindText = this.txtFindNext.Text;
                this.FindNext(false);
            }
        }

        /// <summary>
        /// Check for enter being pressing in find box...
        /// </summary>
        /// <param name="sender">ToolStripTextBox</param>
        /// <param name="e">Events</param>
        private void txtFindNext_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                tsbFind_Click(null, null);
            }
        }

        /// <summary>
        /// Makes the find Dialog Visible...
        /// </summary>
        private void ShowFindDialog()
        {
            // Grab the Selection...
            if (this.ActiveTab != null)
                if (this.ActiveTab.Selection.Length > 0)
                    this.m_FindControl.FindText = this.ActiveTab.Selection;

            if (!this.m_FindControl.Visible)
            {
                this.m_FindControl.Show(this);
            }
        }

        /// <summary>
        /// Finds the Next Occurance of the given Pattern in the Active Document...
        /// </summary>
        public bool FindNext(bool findUp)
        {

            if (this.ActiveContent != null)
            {

                if (this.ActiveContent.GetType() == typeof(DialogCreator))
                {

                    var DC = (DialogCreator)this.ActiveContent;
                    if (DC.Ed_Active == 1)
                    {
                        return DC.EdCond.FindNext(this.m_FindControl.GetRegEx(), findUp);
                    }
                    else if (DC.Ed_Active == 2)
                    {
                        return DC.EdInfo.FindNext(this.m_FindControl.GetRegEx(), findUp);
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    var tab = (IPeterPluginTab)this.ActiveContent;
                    return tab.FindNext(this.m_FindControl.GetRegEx(), findUp);
                }

            }
            return false;
        }
        public bool FindText(Regex regex, bool findUp)
        {
            var activeEditor = this.ActiveEditor;
            if (activeEditor != null)
            {
                return activeEditor.FindText(regex);
            }
            return false;
        }

        /// <summary>
        /// Finds the given Pattern in all of the Open Files...
        /// </summary>
        public void FindInOpenFiles()
        {
            for (var a = 0; a < this.DockMain.Contents.Count; a++)
            {
                var tab = (IPeterPluginTab)this.DockMain.Contents[a];
                if (!string.IsNullOrEmpty(tab.FileName))
                {
                    this.m_FindControl.FindInFile(new FileInfo(tab.FileName), this.m_FindControl.FindText, this.m_FindControl.GetRegEx());
                }
            }
        }

        /// <summary>
        /// Replaces the Next Occurance of the given Pattern in the Active Document...
        /// </summary>
        public void ReplaceNext()
        {
            if (this.ActiveContent != null)
            {
                if (this.ActiveContent.GetType() == typeof(DialogCreator))
                {

                    var DC = (DialogCreator)this.ActiveContent;
                    if (DC.Ed_Active == 1)
                    {
                        DC.EdCond.ReplaceNext(this.m_FindControl.GetRegEx(), this.m_FindControl.ReplaceText, this.m_FindControl.FindUp);
                    }
                    else if (DC.Ed_Active == 2)
                    {
                        DC.EdInfo.ReplaceNext(this.m_FindControl.GetRegEx(), this.m_FindControl.ReplaceText, this.m_FindControl.FindUp);
                    }
                    else
                    {
                        return;
                    }
                }
                else
                {
                    var tab = (IPeterPluginTab)this.ActiveContent;
                    tab.ReplaceNext(this.m_FindControl.GetRegEx(), this.m_FindControl.ReplaceText, this.m_FindControl.FindUp);
                }
            }
        }
        /// <summary>
        /// Ersetzt markiertes
        /// </summary>
        public void ReplaceAllMarked()
        {
            if (this.ActiveContent != null)
            {

                if (this.ActiveContent.GetType() == typeof(DialogCreator))
                {

                    var DC = (DialogCreator)this.ActiveContent;
                    if (DC.Ed_Active == 1)
                    {
                        DC.EdCond.ReplaceAllMarked(this.m_FindControl.GetRegEx(), this.m_FindControl.ReplaceText);
                    }
                    else if (DC.Ed_Active == 2)
                    {
                        DC.EdInfo.ReplaceAllMarked(this.m_FindControl.GetRegEx(), this.m_FindControl.ReplaceText);
                    }
                    else
                    {
                        return;
                    }
                }
                else
                {
                    this.ActiveEditor.ReplaceAllMarked(this.m_FindControl.GetRegEx(), this.m_FindControl.ReplaceText);
                }



            }
        }

        /// <summary>
        /// Replaces all the Occurance of the given Pattern in the Active Document...
        /// </summary>
        public void ReplaceAll()
        {
            if (this.ActiveContent != null)
            {
                if (this.ActiveContent.GetType() == typeof(DialogCreator))
                {

                    var DC = (DialogCreator)this.ActiveContent;
                    if (DC.Ed_Active == 1)
                    {
                        DC.EdCond.ReplaceAll(this.m_FindControl.GetRegEx(), this.m_FindControl.ReplaceText);
                    }
                    else if (DC.Ed_Active == 2)
                    {
                        DC.EdInfo.ReplaceAll(this.m_FindControl.GetRegEx(), this.m_FindControl.ReplaceText);
                    }
                    else
                    {
                        return;
                    }
                }
                else
                {
                    var tab = (IPeterPluginTab)this.ActiveContent;
                    tab.ReplaceAll(this.m_FindControl.GetRegEx(), this.m_FindControl.ReplaceText);
                }
            }
        }

        /// <summary>
        /// Replaces all the Occurance of the given Pattern in all the Documents...
        /// </summary>
        public void ReplaceInOpenFiles()
        {
            for (var a = 0; a < this.DockMain.Contents.Count; a++)
            {
                var tab = (IPeterPluginTab)this.DockMain.Contents[a];
                tab.ReplaceAll(this.m_FindControl.GetRegEx(), this.m_FindControl.ReplaceText);
            }
        }

        /// <summary>
        /// Marks all occurances of the given Pattern in the active Document...
        /// </summary>
        public void MarkAll()
        {
            if (this.ActiveContent != null)
            {
                var tab = (IPeterPluginTab)this.ActiveContent;
                tab.MarkAll(this.m_FindControl.GetRegEx());
            }
        }

        #endregion

        #region -= Select Word =-

        /// <summary>
        /// Selects text at the offset with the give length...
        /// </summary>
        /// <param name="line">Line text is on.</param>
        /// <param name="offset">Offset Text is At.</param>
        /// <param name="wordLeng">Length of Text.</param>
        public void SelectWord(int line, int offset, int wordLeng)
        {
            this.ActiveTab.SelectWord(line, offset, wordLeng);
        }

        #endregion

        #region -= Project Menu =-

        /// <summary>
        /// Shows the project manager...
        /// </summary>
        /// <param name="sender">ToolStripMenuItem</param>
        /// <param name="e">Events</param>
        private void mnuProjectShow_Click(object sender, EventArgs e)
        {
            if (this.m_ProjMan.DockState == DockState.Unknown)
            {
                this.m_ProjMan.Show(this.DockMain, DockState.DockLeft);
            }
            else
            {
                this.m_ProjMan.Show();
            }
        }

        /// <summary>
        /// Opens a project...
        /// </summary>
        /// <param name="sender">ToolStripMenuItem</param>
        /// <param name="e">Events</param>
        private void mnuProjectOpen_Click(object sender, EventArgs e)
        {
            this.ofdMain.Multiselect = false;
            this.ofdMain.Filter = "Peter Project File (*.pproj)|*.pproj";
            if (this.ofdMain.ShowDialog() == DialogResult.OK)
            {
                OpenProject(this.ofdMain.FileName);
            }
            this.ofdMain.Multiselect = true;
            this.ofdMain.Filter = "";
        }

        /// <summary>
        /// Opens the given project...
        /// </summary>
        /// <param name="fileName">Path to project file.</param>
        public void OpenProject(string fileName)
        {
            // Is this a project file...
            if (Path.GetExtension(fileName).ToLower() == ".pproj")
            {
                this.mnuProjectShow_Click(null, null);
                // Load a Project...
                var proj = this.m_ProjMan.LoadFile(fileName);

                if (proj == null)
                {
                    // The project was already open...
                    return;
                }

                // Add Menu Item if Needed...
                if (!this.mnuProjectReopen.DropDownItems.ContainsKey(fileName))
                {
                    var tsmi = new ToolStripMenuItem(proj);
                    tsmi.Click += new EventHandler(ReopenProject);
                    //tsmi.Tag = fileName;
                    tsmi.Name = fileName;

                    this.mnuProjectReopen.DropDownItems.Add(tsmi);
                }

                // Update Config file...
                if (Config.RecentProjects.Project.Count > 0)
                {
                    var existing = Config.RecentProjects.Project.FirstOrDefault(x => x.File == fileName);
                    if (existing is null)
                    {
                        Config.RecentProjects.Project.RemoveAt(0);
                        // Add the new project...
                        Config.RecentProjects.Project.Add(new Classes.Configuration.Project
                        {
                            File = fileName,
                            Name = proj,
                        });
                    }
                }
                else
                {
                    // Add the new project...
                    Config.RecentProjects.Project.Add(new Classes.Configuration.Project
                    {
                        File = fileName,
                        Name = proj,
                    });
                }
                while (Config.RecentProjects.Project.Count > Config.Application.RecentProjectCount)
                {
                    Config.RecentProjects.Project.RemoveAt(0);
                }
                SaveConfig();
            }
        }

        /// <summary>
        /// Reopens a project...
        /// </summary>
        /// <param name="sender">ToolStripMenuItem</param>
        /// <param name="e">Events</param>
        private void ReopenProject(object sender, EventArgs e)
        {
            var tsmi = sender as ToolStripMenuItem;
            this.mnuProjectShow_Click(null, null);
            // Load a Project...
            this.m_ProjMan.LoadFile(tsmi.Name);
        }

        /// <summary>
        /// Creates a new Project...
        /// </summary>
        /// <param name="sender">ToolStripMenuItem</param>
        /// <param name="e">Events</param>
        private void mnuProjectNew_Click(object sender, EventArgs e)
        {
            var prj = new Project();
            prj.ShowDialog();
            if (prj.ProjectFile != null)
            {
                this.OpenProject(prj.ProjectFile);
            }
            if (!prj.IsDisposed)
            {
                prj.Close();
            }
        }

        #endregion

        #region -= Options =-

        /// <summary>
        /// Shows the Options Dialog...
        /// </summary>
        /// <param name="sender">ToolStripMenuItem</param>
        /// <param name="e">Events</param>
        private void mnuOptions_Click(object sender, EventArgs e)
        {
            var frm = new Options(this);
            foreach (IPeterPlugin plugin in this.m_Plugins)
            {
                if (plugin.OptionPanel != null)
                {
                    frm.AddOptionPanel(plugin.OptionPanel, plugin.PluginImage);
                }
            }
            frm.ShowDialog();
        }

        #endregion

        #region -= Run Menu =-

        /// <summary>
        /// Starts a command Prompt...
        /// </summary>
        /// <param name="sender">ToolStripMenuItem</param>
        /// <param name="e">Events</param>
        private void mnuRunCMD_Click(object sender, EventArgs e)
        {
            var cmd = new CommandPrompt
            {
                Icon = Icon.FromHandle(((Bitmap)this.GetInternalImage("cmd")).GetHicon())
            };
            //this.AddDockContent(new CommandPrompt(), DockState.DockBottom);
            this.AddDockContent(cmd, DockState.DockBottom);
        }

        /// <summary>
        /// Runs a given command...
        /// </summary>
        /// <param name="command">Command to Run.</param>
        public void RunCommand(string command)
        {
            var cmd = this.GetCMD();
            cmd.RunCommand(command);
        }

        /// <summary>
        /// Runs the given Script in the given Directory...
        /// </summary>
        /// <param name="script">Script to Run (Commands are separated by new lines).</param>
        /// <param name="workingDir">Directory to run script.</param>
        public void RunScript(string script, string workingDir)
        {
            var cmd = this.GetCMD();
            cmd.RunScript(script, workingDir);
        }

        /// <summary>
        /// Finds an open Command Prompt, if none creates one.
        /// </summary>
        /// <returns>CommandPrompt</returns>
        public CommandPrompt GetCMD()
        {
            CommandPrompt cmd = null;
            foreach (var dc in this.DockMain.Contents)
            {
                if (dc.GetType() == typeof(CommandPrompt))
                {
                    cmd = (CommandPrompt)dc;
                    cmd.Show();
                    break;
                }
            }
            if (cmd == null)
            {
                this.mnuRunCMD_Click(null, null);
                cmd = (CommandPrompt)this.DockMain.Contents[this.DockMain.Contents.Count - 1];
            }

            return cmd;
        }

        #endregion

        #region -= Tools Menu =-

        /// <summary>
        /// Creates a new File Explorer...
        /// </summary>
        /// <param name="sender">ToolStripMenuItem</param>
        /// <param name="e">Events</param>
        private void mnuToolsFileExplorer_Click(object sender, EventArgs e)
        {
            var fe = new ctrlFileExplorer(this)
            {
                Icon = Icon.FromHandle(((Bitmap)this.GetInternalImage("FEIcon")).GetHicon())
            };
            this.AddDockContent(fe, DockState.DockLeft);
        }

        /// <summary>
        /// Shows the Code Structure...
        /// </summary>
        /// <param name="sender">ToolStripMenuItem</param>
        /// <param name="e">Events</param>
        private void tsmiCodeStructure_Click(object sender, EventArgs e)
        {

            if (m_CodeStructure.VisibleState == DockState.Unknown)
            {
                var m_CodeStructure2 = new ctrlCodeStructure(this);
                this.AddDockContent(m_CodeStructure2, DockState.DockRight);
                this.m_CodeStructure = m_CodeStructure2;
            }

        }

        #endregion

        #region -= Code Menu =-

        private void mnuCodeLineComment_Click(object sender, EventArgs e)
        {
            var text = this.ActiveEditor.GetText();
            text.Split('\n');
        }

        private void xMLToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.ActiveEditor != null)
            {
                //will hold formatted xml
                var sb = new StringBuilder();
                //does the formatting
                XmlTextWriter xtw = null;
                try
                {
                    /*
                    & - &amp; 
                    < - &lt; 
                    > - &gt; 
                    " - &quot; 
                    ' - &#39; 
                    */
                    var xml = this.ActiveEditor.GetText();
                    xml = xml.Replace("&", "&amp;");

                    //load unformatted xml into a dom
                    var xd = new XmlDocument();
                    xd.LoadXml(xml);

                    //pumps the formatted xml into the StringBuilder above
                    var sw = new StringWriter(sb);

                    //point the xtw at the StringWriter
                    xtw = new XmlTextWriter(sw)
                    {

                        //we want the output formatted
                        Formatting = Formatting.Indented,
                        Indentation = 4
                    };

                    //get the dom to dump its contents into the xtw 
                    xd.WriteTo(xtw);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "Fehler beim Parsen der XML.\n" + ex.Message, "Stampfer", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    //clean up even if error
                    if (xtw != null)
                        xtw.Close();
                }

                //return the formatted xml
                if (!string.IsNullOrEmpty(sb.ToString()))
                    this.ActiveEditor.SetTextChanged(sb.ToString().Replace("&amp;", "&"));
            }
        }

        #endregion

        /// <summary>
        /// Updates the Status Bar's Caret Info...
        /// </summary>
        /// <param name="offset">Offset of Caret.</param>
        /// <param name="line">Line Carret is on.</param>
        /// <param name="col">Column Carret is at.</param>
        /// <param name="mode">Mode of Carret.</param>
        public void UpdateCaretPos(int offset, int line, int col, string mode)
        {
            if (string.IsNullOrEmpty(mode))
            {
                this.sslLine.Text = "";
                this.sslOther.Text = "";
                this.sslInsert.Text = "";
                this.sslColumn.Text = "";
            }
            else
            {
                this.sslLine.Text = "Zeile: " + line.ToString();
                this.sslOther.Text = mode;
                this.sslInsert.Text = "Zeichen: " + offset.ToString();
                this.sslColumn.Text = "Spalte: " + col.ToString();
            }
        }

        private void geheZuZeileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.m_GotoControl = new GoToLine(this);

            this.m_GotoControl.Show(this);
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            mnuCode.Visible = false;
            ss.Close();
            this.WindowState = FormWindowState.Maximized;
        }

        private void gothicInstanzenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (m_GothicStructure.VisibleState == DockState.Unknown)
            {
                var m_GothicStructure2 = new ctrlGothicInstances(this);

                this.AddDockContent(m_GothicStructure2, DockState.DockRight);
                this.m_GothicStructure = m_GothicStructure2;
            }
        }

        private void zeilenkommentarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.ActiveEditor != null)
            {
                this.ActiveEditor.Enclose(@"//", "");
                this.ActiveEditor.Refresh();
            }
        }

        private void blockkommentarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.ActiveEditor != null)
            {
                this.ActiveEditor.Enclose(@"/*", @"*/");
                this.ActiveEditor.Refresh();
            }
        }

        private void kleinesKommentarfeldToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.ActiveEditor != null)
            {
                this.ActiveEditor.Enclose("\r\n" + @"/*===================================================================" + "\r\n", "\r\n" + @"==================================================================*/" + "\r\n");
                this.ActiveEditor.Refresh();
            }
        }

        private void großesKommentarfeldToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.ActiveEditor != null)
            {
                this.ActiveEditor.Enclose("\r\n" + "\r\n" + @"/*#################################################################" + "\r\n" + "###################################################################" + "\r\n", "\r\n" + "###################################################################" + "\r\n" + @"#################################################################*/" + "\r\n" + "\r\n");
                this.ActiveEditor.Refresh();
            }
        }

        private void questManagerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (m_QuestManager.VisibleState == DockState.Unknown)
            {
                var m_QuestManager2 = new ctrlQuestManager(this);
                this.AddDockContent(m_QuestManager2, DockState.DockRight);
                this.m_QuestManager = m_QuestManager2;
            }
        }

        private void dialogAssistentToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var DC = new DialogCreator(this);
            this.AddDockContent(DC, DockState.Document);
        }

        private void BtShortFunc_Click(object sender, EventArgs e)
        {
            if (m_AutoComplete != null && ActiveEditor != null)
            {

                ActiveEditor.m_Editor.ActiveTextAreaControl.Document.Replace(0, ActiveEditor.m_Editor.ActiveTextAreaControl.Document.TextContent.Length, m_AutoComplete.TransformShortFunc(ActiveEditor));

                ActiveEditor.UpdateFolding();
            }
        }

        private void kurzfunktionenUmwandelnToolStripMenuItem_Click(object sender, EventArgs e)
        {
            BtShortFunc_Click(null, new EventArgs());
        }

        private void tSaveTimer_Tick(object sender, EventArgs e)
        {
            if (m_BackupFolder != ""
                && Directory.Exists(m_BackupFolder))
            {
                Trace("Backup erstellt");
                var Current = m_BackupFolder + "\\Current\\";
                var filename = "";
                if (!Directory.Exists(Current))
                {
                    Directory.CreateDirectory(Current);
                }
                StreamWriter stream;
                Editor tab;
                for (var a = 0; a < this.DockMain.Contents.Count; a++)
                {
                    try
                    {
                        tab = (Editor)this.DockMain.Contents[a];
                    }
                    catch
                    {
                        continue;
                    }

                    if (tab.FileName == null)
                    {
                        this.SaveAs(tab);
                    }
                    else if (Path.GetExtension(tab.FileName) != ".d")
                    {
                        continue;
                    }
                    else
                    {
                        filename = tab.FileName;
                        filename = Current + Path.GetFileName(filename);
                        try
                        {
                            stream = new StreamWriter(filename, false, Encoding.GetEncoding(1252));
                            stream.Write(tab.m_Editor.Document.TextContent);
                            stream.Close();
                        }
                        catch
                        { }
                    }
                }
            }
        }

        private void mnuExpandAll_Click(object sender, EventArgs e)
        {
            if (ActiveEditor != null)
            {
                ActiveEditor.FoldingExpand();
            }
        }

        private void mnuCollapseAll_Click(object sender, EventArgs e)
        {
            if (ActiveEditor != null)
            {
                ActiveEditor.FoldingCollapse();
            }
        }

        private void autoEinrückenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ActiveEditor != null)
            {
                ActiveEditor.Indent();
            }
        }

        private void regionenAusklappenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ActiveEditor != null)
            {
                ActiveEditor.RegionsExpand();
            }
        }

        private void regionenEinklappenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ActiveEditor != null)
            {
                ActiveEditor.RegionsCollapse();
            }
        }

        private void autocompletemenu_Click(object sender, EventArgs e)
        {
            if (autocompletemenu.Text == autocompletemenuauto)
            {
                m_EditorConfig.Autocomplete = false;
                autocompletemenu.Text = autocompletemenumanu;
                for (var x = 0; x < this.DockMain.Contents.Count; x++)
                {
                    if (this.DockMain.Contents[x] is Editor)
                    {
                        ((Editor)this.DockMain.Contents[x]).AutoCompleteAuto = false;
                    }
                    else if (this.DockMain.Contents[x] is DialogCreator)
                    {
                        ((DialogCreator)this.DockMain.Contents[x]).EdCond.AutoCompleteAuto = false;
                        ((DialogCreator)this.DockMain.Contents[x]).EdInfo.AutoCompleteAuto = false;
                    }
                }
            }
            else
            {
                m_EditorConfig.Autocomplete = true;
                autocompletemenu.Text = autocompletemenuauto;
                for (var x = 0; x < this.DockMain.Contents.Count; x++)
                {
                    if (this.DockMain.Contents[x] is Editor)
                    {
                        ((Editor)this.DockMain.Contents[x]).AutoCompleteAuto = true;
                    }
                    else if (this.DockMain.Contents[x] is DialogCreator)
                    {
                        ((DialogCreator)this.DockMain.Contents[x]).EdCond.AutoCompleteAuto = true;
                        ((DialogCreator)this.DockMain.Contents[x]).EdInfo.AutoCompleteAuto = true;
                    }
                }
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            //Speicherung der Instanzen
            initialized = false;
            if (m_GothicStructure != null)
            {
                var handlers = new Dictionary<string, List<Instance>>
                {
                    { FilePaths.ITEMS, m_GothicStructure.ItemList.Values.ToList() },
                    { FilePaths.NPCS, m_GothicStructure.NPCList.Values.ToList() },
                    { FilePaths.DIALOGE, m_GothicStructure.DialogList.Values.ToList() },
                    { FilePaths.FUNC, m_GothicStructure.FuncList.Values.ToList() },
                    { FilePaths.VARS, m_GothicStructure.VarList.Values.ToList() },
                    { FilePaths.CONSTS, m_GothicStructure.ConstList.Values.ToList() },
                };
                foreach (var handler in handlers)
                {
                    using (var fs = File.Create(Path.Combine(this.m_ScriptsPath, handler.Key)))
                    using (var sw = new StreamWriter(fs, Encoding.Default))
                    {
                        foreach (var instance in handler.Value)
                        {
                            sw.WriteLine(instance.ToString() + Environment.NewLine + instance.File);
                        }
                    }
                }
            }
        }

        private void druckenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.ActiveTab.Print();
        }

        private void txtFindNext_Enter(object sender, EventArgs e)
        {
            this.Focus();
            ((ToolStripTextBox)sender).Focus();
        }

        void IPeterPluginHost.AddDockContent(IDockContent content) { }
        void IPeterPluginHost.AddDockContent(IDockContent content, Rectangle floatingRec) { }
    }
}
