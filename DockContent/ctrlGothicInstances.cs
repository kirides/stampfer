/**************************************************************************************
    Stampfer - Gothic Script Editor
    Copyright (C) 2009 Alexander "Sumpfkrautjunkie" Ruppert

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
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using PeterInterface;
using WeifenLuo.WinFormsUI.Docking;

namespace Peter
{
    public partial class ctrlGothicInstances : DockContent, IPeterPluginTab
    {
        private readonly MainForm MainF;
        private readonly Regex NoSpaces = new Regex(@"  ");
        private readonly bool m_CanScroll;

        private readonly string ScriptsPath;

        private readonly TreeNode DialogTree = new TreeNode("Dialoge");
        private readonly TreeNode NPCTree = new TreeNode("NPCs");
        private readonly TreeNode ItemTree = new TreeNode("Items");

        private readonly TreeNode FuncTree = new TreeNode("Funktionen");
        private readonly TreeNode VarTree = new TreeNode("Variablen");
        public TreeNode ConstTree = new TreeNode("Konstanten");

        public Dictionary<string, Instance> FuncList = new Dictionary<string, Instance>();
        public Dictionary<string, Instance> VarList = new Dictionary<string, Instance>();
        public Dictionary<string, Instance> ConstList = new Dictionary<string, Instance>();
        public Dictionary<string, Instance> ItemList = new Dictionary<string, Instance>();
        public Dictionary<string, Instance> NPCList = new Dictionary<string, Instance>();
        public Dictionary<string, Instance> DialogList = new Dictionary<string, Instance>();
        private const int DIALOGIMG = 0;
        private const int NPCIMG = 1;
        private const int ITEMIMG = 2;
        private const int FUNCIMG = 3;
        private const int VARIMG = 4;
        private const int CONSTIMG = 5;

        public ctrlGothicInstances(MainForm m)
        {

            this.MainF = m;
            InitializeComponent();
            this.ScriptsPath = m.m_ScriptsPath;
            this.m_CanScroll = true;
            this.treeMain.ImageList = m.ImgList;
            this.TabText = "Gothic Bezeichner";
            this.treeMain.NodeMouseDoubleClick += new TreeNodeMouseClickEventHandler(treeMain_NodeMouseDoubleClick);
            if (this.ScriptsPath != "")
            {
                this.treeMain.BeginUpdate();
                ReadInstancesFromFile();

                this.treeMain.EndUpdate();
            }

            DialogTree.ImageIndex = DIALOGIMG;
            DialogTree.SelectedImageIndex = DialogTree.ImageIndex;

            NPCTree.ImageIndex = NPCIMG;
            NPCTree.SelectedImageIndex = NPCTree.ImageIndex;

            ItemTree.ImageIndex = ITEMIMG;
            ItemTree.SelectedImageIndex = ItemTree.ImageIndex;

            FuncTree.ImageIndex = FUNCIMG;
            FuncTree.SelectedImageIndex = FuncTree.ImageIndex;

            VarTree.ImageIndex = VARIMG;
            VarTree.SelectedImageIndex = VarTree.ImageIndex;

            ConstTree.ImageIndex = CONSTIMG;
            ConstTree.SelectedImageIndex = ConstTree.ImageIndex;

            DialogTree.Nodes.Add("fakenode");
            NPCTree.Nodes.Add("fakenode");
            ItemTree.Nodes.Add("fakenode");
            FuncTree.Nodes.Add("fakenode");
            VarTree.Nodes.Add("fakenode");
            ConstTree.Nodes.Add("fakenode");
            treeMain.Nodes.Add(DialogTree);
            treeMain.Nodes.Add(NPCTree);
            treeMain.Nodes.Add(ItemTree);
            treeMain.Nodes.Add(FuncTree);
            treeMain.Nodes.Add(VarTree);
            treeMain.Nodes.Add(ConstTree);
            treeMain.BeforeExpand += new TreeViewCancelEventHandler(treeMain_BeforeExpand);
            treeMain.BeforeCollapse += new TreeViewCancelEventHandler(treeMain_BeforeCollapse);
        }

        private void treeMain_BeforeCollapse(object sender, TreeViewCancelEventArgs e)
        {
            treeMain.BeginUpdate();
            if (e.Node.ImageIndex == DIALOGIMG)//Dialogtree
            {
                DialogTree.Nodes.Clear();
                DialogTree.Nodes.Add("fakenode");
            }
            else if (e.Node.ImageIndex == NPCIMG)//Dialogtree
            {
                NPCTree.Nodes.Clear();
                NPCTree.Nodes.Add("fakenode");
            }
            else if (e.Node.ImageIndex == ITEMIMG)//Dialogtree
            {
                ItemTree.Nodes.Clear();
                ItemTree.Nodes.Add("fakenode");
            }
            else if (e.Node.ImageIndex == FUNCIMG)//Dialogtree
            {
                FuncTree.Nodes.Clear();
                FuncTree.Nodes.Add("fakenode");
            }
            else if (e.Node.ImageIndex == VARIMG)//Dialogtree
            {
                VarTree.Nodes.Clear();
                VarTree.Nodes.Add("fakenode");
            }
            else if (e.Node.ImageIndex == CONSTIMG)//Dialogtree
            {
                ConstTree.Nodes.Clear();
                ConstTree.Nodes.Add("fakenode");
            }

            treeMain.EndUpdate();
        }

        private void treeMain_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            treeMain.BeginUpdate();
            if (e.Node.ImageIndex == DIALOGIMG)//Dialogtree
            {
                if (DialogList.Count == 0) { treeMain.EndUpdate(); return; }
                UpdateDialogTree();
            }
            else if (e.Node.ImageIndex == NPCIMG)//Dialogtree
            {
                if (NPCList.Count == 0) { treeMain.EndUpdate(); return; }
                UpdateNPCTree();
            }
            else if (e.Node.ImageIndex == ITEMIMG)//Dialogtree
            {
                if (ItemList.Count == 0) { treeMain.EndUpdate(); return; }
                UpdateItemTree();
            }
            else if (e.Node.ImageIndex == FUNCIMG)//Dialogtree
            {
                if (FuncList.Count == 0) { treeMain.EndUpdate(); return; }
                UpdateFuncTree();
            }
            else if (e.Node.ImageIndex == VARIMG)//Dialogtree
            {
                if (VarList.Count == 0) { treeMain.EndUpdate(); return; }
                UpdateVarTree();
            }
            else if (e.Node.ImageIndex == CONSTIMG)//Dialogtree
            {
                if (ConstList.Count == 0) { treeMain.EndUpdate(); return; }
                UpdateConstTree();
            }
            treeMain.EndUpdate();
        }

        #region -= Helpers =-

        /// <summary>
        /// After an item is selected, scroll to it...
        /// </summary>
        /// <param name="sender">TreeView</param>
        /// <param name="e">TreeViewEventArgs</param>

        private void treeMain_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {

            if (this.m_CanScroll)
            {
                if (e.Node.Parent != null)
                {
                    if (e.Node.Tag != null)
                    {
                        var file = (e.Node.Tag.ToString());
                        OpenFile(file, e.Node.Text);
                    }
                }
            }

        }
        public void OpenFile(string file, string txt)
        {
            if (File.Exists(file))
            {
                var searchstring = "";

                this.MainF.CreateEditor(file, Path.GetFileName(file));
                foreach (var c in txt)
                {
                    if (c == '=' || c == '(')
                    {
                        break;
                    }
                    searchstring += c.ToString();
                }
                if (txt.ToLower().StartsWith("void"))
                {
                    this.MainF.FindText(new Regex(@"void(\s)*" + RemoveType(ref searchstring, ref file), RegexOptions.IgnoreCase), true);
                }
                else if (txt.ToLower().StartsWith("int"))
                {
                    this.MainF.FindText(new Regex(@"int(\s)*" + RemoveType(ref searchstring, ref file), RegexOptions.IgnoreCase), true);
                }
                else if (txt.ToLower().StartsWith("string"))
                {
                    this.MainF.FindText(new Regex(@"string(\s)*" + RemoveType(ref searchstring, ref file), RegexOptions.IgnoreCase), true);
                }
                else if (txt.ToLower().StartsWith("c_npc"))
                {
                    this.MainF.FindText(new Regex(@"c_npc(\s)*" + RemoveType(ref searchstring, ref file), RegexOptions.IgnoreCase), true);
                }
                else if (txt.ToLower().StartsWith("c_item"))
                {
                    this.MainF.FindText(new Regex(@"c_item(\s)*" + RemoveType(ref searchstring, ref file), RegexOptions.IgnoreCase), true);
                }
                else if (txt.ToLower().StartsWith("float"))
                {
                    this.MainF.FindText(new Regex(@"float(\s)*" + RemoveType(ref searchstring, ref file), RegexOptions.IgnoreCase), true);
                }
                else
                {
                    this.MainF.FindText(new Regex(@"\s" + RemoveType(ref searchstring, ref file), RegexOptions.IgnoreCase), true);
                }
            }
        }

        #endregion

        #region IPeterPluginTab Members

        public void Save()
        {
        }

        public void SaveAs(string filePath)
        {
        }

        public void Cut()
        {
        }

        public void Copy()
        {
        }

        public void Paste()
        {
        }

        public void Undo()
        {
        }

        public void Redo()
        {
        }

        public void Delete()
        {
        }

        public void Duplicate()
        {
        }

        public void Print()
        {
        }

        public void SelectAll()
        {
        }

        public bool CloseTab()
        {
            this.Close();
            return true;
        }

        public void MarkAll(System.Text.RegularExpressions.Regex reg)
        {
        }

        public bool FindNext(System.Text.RegularExpressions.Regex reg, bool searchUp)
        {
            return false;
        }

        public void ReplaceNext(System.Text.RegularExpressions.Regex reg, string replaceWith, bool searchUp)
        {
        }

        public void ReplaceAll(System.Text.RegularExpressions.Regex reg, string replaceWith)
        {
        }

        public void SelectWord(int line, int offset, int wordLeng)
        {
        }

        public IPeterPluginHost Host { get; set; }

        public string FileName
        {
            get { return ""; }
        }

        public string Selection
        {
            get { return ""; }
        }

        public bool AbleToUndo
        {
            get { return false; }
        }

        public bool AbleToRedo
        {
            get { return false; }
        }

        public bool NeedsSaving
        {
            get { return false; }
        }

        public bool AbleToPaste
        {
            get { return false; }
        }

        public bool AbleToCut
        {
            get { return false; }
        }

        public bool AbleToCopy
        {
            get { return false; }
        }

        public bool AbleToSelectAll
        {
            get { return false; }
        }

        public bool AbleToSave
        {
            get { return false; }
        }

        public bool AbleToDelete
        {
            get { return false; }
        }

        #endregion

        #region -= Tool Bar =-

        private void tsbExpandAll_Click(object sender, EventArgs e)
        {
            this.treeMain.BeginUpdate();
            for (var a = 0; a < this.treeMain.Nodes.Count; a++)
            {
                this.treeMain.Nodes[a].ExpandAll();
            }
            this.treeMain.EndUpdate();
        }

        private void tsbCollapseAll_Click(object sender, EventArgs e)
        {
            this.treeMain.BeginUpdate();
            for (var a = 0; a < this.treeMain.Nodes.Count; a++)
            {
                this.treeMain.Nodes[a].Collapse();
            }
            this.treeMain.EndUpdate();
        }

        #endregion

        private void ClearArrays()
        {
            this.DialogTree.Nodes.Clear();
            this.NPCTree.Nodes.Clear();
            this.ItemTree.Nodes.Clear();
            this.FuncTree.Nodes.Clear();
            this.VarTree.Nodes.Clear();
            this.ConstTree.Nodes.Clear();
            ItemList.Clear();
            DialogList.Clear();
            NPCList.Clear();
            FuncList.Clear();
            VarList.Clear();
            ConstList.Clear();
        }

        private readonly Regex r = new Regex(@"((^)|(\s))instance ", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private readonly Regex r2 = new Regex(@"((^)|(\s))func ", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private readonly Regex r3 = new Regex(@"((^)|(\s))var ", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private readonly Regex r4 = new Regex(@"((^)|(\s))const ", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private const int SB_LENGTH = 256;
        public int GetItems(string path)
        {
            var s = File.ReadAllText(path, Encoding.Default);

            var m = r.Matches(s);
            StringBuilder sb1;
            var i = 0;
            var k = 0;
            foreach (Match match in m)
            {
                sb1 = new StringBuilder(SB_LENGTH);
                i = match.Index + match.Length;
                while ((i < s.Length) && (s[i] != '(') && (s[i] != ' ') && (s[i] != '\t'))
                {
                    sb1.Append(s[i]);

                    i++;
                }
                k |= AddItem(sb1.ToString(), path);
            }
            return k;
        }
        public int AddItem(string sb1, string path)
        {
            if (sb1.Length > 0
                    && !ItemList.ContainsKey(sb1.ToString()))
            {
                ItemList.Add(sb1, new Instance(sb1.ToString(), path));
                return 32;
            }
            return 0;
        }
        public int GetNPCs(string path)
        {
            var s = File.ReadAllText(path, Encoding.Default);
            var m = r.Matches(s);
            StringBuilder sb1;
            var i = 0;
            var k = 0;
            foreach (Match match in m)
            {
                sb1 = new StringBuilder(SB_LENGTH);
                i = match.Index + match.Length;
                while ((i < s.Length) && (s[i] != '(') && (s[i] != ' ') && (s[i] != '\t'))
                {
                    sb1.Append(s[i]);
                    i++;
                }
                k |= AddNPC(sb1.ToString(), path);
            }
            return k;
        }
        public int AddNPC(string sb1, string path)
        {
            if (sb1.Length > 0
                    && !NPCList.ContainsKey(sb1.ToString()))
            {

                NPCList.Add(sb1, new Instance(sb1.ToString(), path));
                return 16;
            }
            return 0;
        }
        public int GetDias(string path)
        {
            var s = File.ReadAllText(path, Encoding.Default);
            var m = r.Matches(s);
            StringBuilder sb1;
            var i = 0;
            var k = 0;
            foreach (Match match in m)
            {
                sb1 = new StringBuilder(SB_LENGTH);
                i = match.Index + match.Length;
                while ((i < s.Length) && (s[i] != '(') && (s[i] != ' ') && (s[i] != '\t'))
                {
                    sb1.Append(s[i]);
                    i++;
                }
                k |= AddDia(sb1.ToString(), path);
            }
            return k;
        }
        public int AddDia(string sb1, string path)
        {
            if (sb1.Length > 0 && !DialogList.ContainsKey(sb1.ToString()))
            {
                DialogList.Add(sb1, new Instance(sb1.ToString(), path));
                return 8;
            }
            return 0;
        }
        public int GetFuncs(string path)
        {
            var externals = Path.GetFileName(path).ToLower() == "externals.d";
            var s = File.ReadAllText(path, Encoding.Default);
            MatchCollection m;
            StringBuilder sb1;
            StringBuilder sb2;
            var k = 0;

            m = r2.Matches(s);
            int i;
            foreach (Match match in m)
            {
                sb1 = new StringBuilder(SB_LENGTH * 2);
                sb2 = new StringBuilder(SB_LENGTH * 2);
                i = match.Index + match.Length;
                while (s[i] != '{' && s[i] != '\n' && s[i] != '/')
                {
                    if (s[i] == '(')
                    {
                        sb1.Append(" " + s[i]);
                        i++;
                        continue;
                    }
                    if (s[i] == ',')
                    {
                        sb1.Append(s[i] + " ");
                        i++;
                        continue;
                    }

                    sb1.Append(s[i]);
                    if (s[i] == ')')
                        break;
                    i++;
                }
                k |= AddFunc(sb1.ToString(), path);
            }

            if (externals) return k;

            m = r3.Matches(s);
            foreach (Match match in m)
            {
                sb1 = new StringBuilder(512);
                i = match.Index + match.Length;
                while (s[i] != '\n' && s[i] != ')')
                {
                    if (s[i] == ';')
                        break;
                    sb1.Append(s[i]);
                    i++;
                }
                k |= AddVar(sb1.ToString(), path);
            }

            m = r4.Matches(s);
            foreach (Match match in m)
            {
                sb1 = new StringBuilder(512);
                i = match.Index + match.Length;
                while (s[i] != ';' && s[i] != '\n')
                {
                    if (s[i] == '=')
                    {
                        sb1.Append(" " + s[i] + " ");
                        i++;
                        continue;
                    }
                    sb1.Append(s[i]);
                    i++;
                }
                k |= AddConst(sb1.ToString(), path);
            }
            return k;
        }

        public int AddFunc(string sb1, string path)
        {
            if (sb1.Length > 0)
            {
                var tempstring = sb1.Trim().Replace('\t', ' ');
                string tempstring2;
                if (tempstring.Length == 0) return 0;
                tempstring = RemoveDoubleSpaces(tempstring);
                try
                {
                    var y = tempstring.IndexOf(" ");
                    if (y > 0)
                    {
                        var temp = tempstring.Substring(0, y);
                        tempstring = temp.ToLower() + tempstring.Substring(y);
                    }
                }
                catch { }
                tempstring2 = tempstring.ToLower();
                if (tempstring2.StartsWith("void") || tempstring2.StartsWith("int") || tempstring2.StartsWith("c_npc") || tempstring2.StartsWith("c_item") || tempstring2.StartsWith("string"))
                {
                    if (!FuncList.ContainsKey(tempstring))
                    {
                        FuncList.Add(tempstring, new Instance(tempstring, path));
                        return 4;
                    }
                }
            }
            return 0;
        }
        public int AddVar(string sb1, string path)
        {
            if (sb1.Length > 0)
            {
                var tempstring = sb1.Trim().Replace('\t', ' ');
                string temp;
                if (tempstring.Length == 0) return 0;
                tempstring = RemoveDoubleSpaces(tempstring);
                try
                {
                    var y = tempstring.IndexOf(" ");
                    if (y > 0)
                    {
                        temp = tempstring.Substring(0, y);
                        tempstring = temp.ToLower() + tempstring.Substring(y);
                    }
                }
                catch { }

                if (!VarList.ContainsKey(tempstring))
                {
                    VarList.Add(tempstring, new Instance(tempstring, path));
                    return 2;
                }
            }
            return 0;
        }
        public int AddConst(string sb1, string path)
        {
            if (sb1.Length > 0)
            {
                var tempstring = sb1.Trim().Replace('\t', ' ');
                string temp;
                if (tempstring.Length == 0) return 0;
                tempstring = RemoveDoubleSpaces(tempstring);
                try
                {
                    var y = tempstring.IndexOf(" ");
                    if (y > 0)
                    {
                        temp = tempstring.Substring(0, y);
                        tempstring = temp.ToLower() + tempstring.Substring(y);
                    }
                }
                catch { }
                if (!ConstList.ContainsKey(tempstring))
                {
                    ConstList.Add(tempstring, new Instance(tempstring, path));
                    return 1;
                }
            }
            return 0;
        }
        private void GetInstancesToFile(bool d, bool np, bool it, bool fu)
        {
            ClearTree(d, np, it, fu);

            this.MainF.Trace("Gothic-Bezeichner werden aktualisert (kann einige Sekunden dauern).");
            DirectoryInfo dig;// = new DirectoryInfo(Path.GetDirectoryName(this.ScriptsPath + @"\Content\Items\"));
            FileInfo[] rgFilesg;// = dig.GetFiles("*.d", SearchOption.AllDirectories);

            if (it)
            {
                dig = new DirectoryInfo(Path.GetDirectoryName(this.ScriptsPath + FilePaths.ContentItems));
                rgFilesg = dig.GetFiles("*.d", SearchOption.AllDirectories);
                if (rgFilesg.Length > 0)
                {
                    foreach (var fi in rgFilesg)
                    {
                        GetItems(fi.FullName);
                    }
                }
            }
            if (np)
            {
                dig = new DirectoryInfo(Path.GetDirectoryName(this.ScriptsPath + FilePaths.ContentNPC));
                rgFilesg = dig.GetFiles("*.d", SearchOption.AllDirectories);
                if (rgFilesg.Length > 0)
                {
                    foreach (var fi in rgFilesg)
                    {
                        GetNPCs(fi.FullName);
                    }
                }
            }
            if (d)
            {
                dig = new DirectoryInfo(Path.GetDirectoryName(this.ScriptsPath + FilePaths.ContentDialoge));
                rgFilesg = dig.GetFiles("*.d", SearchOption.AllDirectories);
                if (rgFilesg.Length > 0)
                {
                    foreach (var fi in rgFilesg)
                    {
                        GetDias(fi.FullName);
                    }
                }
            }

            if (fu)
            {
                dig = new DirectoryInfo(Path.GetDirectoryName(this.ScriptsPath + FilePaths.ContentDir));
                rgFilesg = dig.GetFiles("*.d", SearchOption.AllDirectories);
                if (rgFilesg.Length > 0)
                {
                    foreach (var fi in rgFilesg)
                    {
                        GetFuncs(fi.FullName);
                    }
                }
            }

            this.MainF.Trace("Gothic-Bezeichner wurden erfolgreich aktualisert.");
            SetAutoCompleteContent();
        }

        private void ClearTree(bool d, bool np, bool it, bool fu)
        {
            if (d)
            {
                DialogTree.Collapse();
                DialogList.Clear();
            }

            if (np)
            {
                NPCTree.Collapse();
                NPCList.Clear();
            }

            if (it)
            {
                ItemTree.Collapse();
                ItemList.Clear();
            }

            if (fu)
            {
                FuncTree.Collapse();
                VarTree.Collapse();
                ConstTree.Collapse();
                FuncList.Clear();
                VarList.Clear();
                ConstList.Clear();
            }
        }

        private void UpdateConstTree()
        {
            var t = new List<Instance>();
            t.AddRange(ConstList.Values);
            t.Sort();
            this.ConstTree.Nodes.Clear();
            foreach (var sl in t)
            {
                var node = new TreeNode(sl.ToString())
                {
                    ImageIndex = CONSTIMG
                };
                node.SelectedImageIndex = node.ImageIndex;
                node.Tag = sl.File;
                this.ConstTree.Nodes.Add(node);
            }
            t.Clear();
        }
        private void UpdateVarTree()
        {
            var t = new List<Instance>();
            t.AddRange(VarList.Values);
            t.Sort();
            this.VarTree.Nodes.Clear();
            foreach (var sl in t)
            {
                var node = new TreeNode(sl.ToString())
                {
                    ImageIndex = VARIMG
                };
                node.SelectedImageIndex = node.ImageIndex;
                node.Tag = sl.File;
                this.VarTree.Nodes.Add(node);
            }
            t.Clear();
        }
        private void UpdateFuncTree()
        {
            var t = new List<Instance>();
            t.AddRange(FuncList.Values);
            t.Sort();
            this.FuncTree.Nodes.Clear();
            foreach (var sl in t)
            {
                var node = new TreeNode(sl.ToString())
                {
                    ImageIndex = FUNCIMG
                };
                node.SelectedImageIndex = node.ImageIndex;
                node.Tag = sl.File;
                this.FuncTree.Nodes.Add(node);
            }
            t.Clear();
        }
        private void UpdateItemTree()
        {
            var t = new List<Instance>();
            t.AddRange(ItemList.Values);
            t.Sort();
            this.ItemTree.Nodes.Clear();
            foreach (var sl in t)
            {
                var node = new TreeNode(sl.ToString())
                {
                    ImageIndex = ITEMIMG
                };
                node.SelectedImageIndex = node.ImageIndex;
                node.Tag = sl.File;
                this.ItemTree.Nodes.Add(node);
            }
            t.Clear();
        }
        private void UpdateNPCTree()
        {
            var t = new List<Instance>();
            t.AddRange(NPCList.Values);
            t.Sort();
            this.NPCTree.Nodes.Clear();
            foreach (var sl in t)
            {
                var node = new TreeNode(sl.ToString())
                {
                    ImageIndex = NPCIMG
                };
                node.SelectedImageIndex = node.ImageIndex;
                node.Tag = sl.File;
                this.NPCTree.Nodes.Add(node);
            }
            t.Clear();
        }
        private void UpdateDialogTree()
        {
            var t = new List<Instance>();
            t.AddRange(DialogList.Values);
            t.Sort();
            this.DialogTree.Nodes.Clear();
            foreach (var sl in t)
            {
                var node = new TreeNode(sl.ToString())
                {
                    ImageIndex = DIALOGIMG
                };
                node.SelectedImageIndex = node.ImageIndex;
                node.Tag = sl.File;
                this.DialogTree.Nodes.Add(node);
            }
            t.Clear();
        }

        private void ReadInstancesFromFile()
        {
            ClearArrays();

            if (File.Exists(this.ScriptsPath + FilePaths.DIALOGE)
                && File.Exists(this.ScriptsPath + FilePaths.ITEMS)
                && File.Exists(this.ScriptsPath + FilePaths.NPCS)
                && File.Exists(this.ScriptsPath + FilePaths.FUNC)
                && File.Exists(this.ScriptsPath + FilePaths.VARS)
                && File.Exists(this.ScriptsPath + FilePaths.CONSTS))
            {
                try
                {
                    this.MainF.Trace("Gothic-Bezeichner werden ausgelesen.");
                    var line = "";
                    var line2 = "";

                    var sr = new StreamReader(this.ScriptsPath + FilePaths.DIALOGE, Encoding.Default);
                    while ((line = sr.ReadLine()) != null)
                    {
                        line2 = sr.ReadLine();
                        DialogList.Add(line, new Instance(line, line2));

                    }
                    sr.Close();

                    sr = new StreamReader(this.ScriptsPath + FilePaths.NPCS, Encoding.Default);
                    while ((line = sr.ReadLine()) != null)
                    {
                        line2 = sr.ReadLine();
                        NPCList.Add(line, new Instance(line, line2));

                    }
                    sr.Close();

                    sr = new StreamReader(this.ScriptsPath + FilePaths.ITEMS, Encoding.Default);
                    while ((line = sr.ReadLine()) != null)
                    {
                        line2 = sr.ReadLine();
                        ItemList.Add(line, new Instance(line, line2));
                    }
                    sr.Close();

                    using (sr = new StreamReader(this.ScriptsPath + FilePaths.FUNC, Encoding.Default))
                    {
                        while ((line = sr.ReadLine()) != null)
                        {
                            line2 = sr.ReadLine();
                            FuncList.Add(line, new Instance(line, line2));

                        }
                    }

                    sr = new StreamReader(this.ScriptsPath + FilePaths.VARS, Encoding.Default);
                    while ((line = sr.ReadLine()) != null)
                    {
                        line2 = sr.ReadLine();
                        VarList.Add(line, new Instance(line, line2));

                    }
                    sr.Close();

                    sr = new StreamReader(this.ScriptsPath + FilePaths.CONSTS, Encoding.Default);
                    while ((line = sr.ReadLine()) != null)
                    {
                        line2 = sr.ReadLine();
                        ConstList.Add(line, new Instance(line, line2));

                    }
                    sr.Close();
                    sr.Dispose();

                    this.MainF.Trace("Gothic-Bezeichner erfolgreich ausgelesen.");
                }
                catch
                {
                    GetInstancesToFile(true, true, true, true);
                }
            }
            else
            {
                GetInstancesToFile(true, true, true, true);
            }
            SetAutoCompleteContent();
        }
        public string RemoveDoubleSpaces(string s)
        {
            var ts = s.Split(' ');
            var sb = new StringBuilder(s.Length);

            foreach (var st in ts)
            {
                s = st.Trim();
                if (s.Length > 0)
                    sb.Append(s + " ");
            }
            return sb.ToString().Remove(sb.Length - 1);
        }

        public void SetAutoCompleteContent()
        {

            if (this.MainF.m_AutoComplete != null)
            {
                if (this.MainF.m_AutoComplete.Extension == ".d")
                {
                    this.MainF.m_AutoComplete.KW.Clear();
                    string s1, s2 = "";

                    foreach (var i in VarList.Values)
                    {
                        s1 = i.Name;
                        ConvertVarForAutoComplete(ref s1, ref s2);
                        var k = new Classes.KeyWord(s1, 4, s2, "Variable");

                        if (this.MainF.m_AutoComplete.KW.BinarySearch(k) >= 0)
                        {
                            continue;
                        }
                        this.MainF.m_AutoComplete.KW.Add(k);
                    }

                    foreach (var i in FuncList.Values)
                    {
                        s1 = i.Name;
                        ConvertFuncForAutoComplete(ref s1, ref s2);

                        var k = new Classes.KeyWord(s1, 3, s2, " ");

                        this.MainF.m_AutoComplete.KW.Add(k);
                    }

                    foreach (var i in ConstList.Values)
                    {
                        s1 = i.Name;
                        ConvertConstForAutoComplete(ref s1, ref s2);

                        var k = new Classes.KeyWord(s1, 5, s2, "Konstante");
                        if (this.MainF.m_AutoComplete.KW.BinarySearch(k) >= 0)
                        {
                            continue;
                        }
                        this.MainF.m_AutoComplete.KW.Add(k);
                    }
                    foreach (var i in DialogList.Values)
                    {
                        var k = new Classes.KeyWord(i.Name, 0, "Dialog", " ");
                        this.MainF.m_AutoComplete.KW.Add(k);
                    }

                    foreach (var i in ItemList.Values)
                    {
                        var k = new Classes.KeyWord(i.Name, 2, "Item", " ");
                        this.MainF.m_AutoComplete.KW.Add(k);
                    }

                    foreach (var i in NPCList.Values)
                    {
                        var k = new Classes.KeyWord(i.Name, 1, "NPC", " ");
                        this.MainF.m_AutoComplete.KW.Add(k);
                    }

                    this.MainF.m_AutoComplete.KW.AddRange(this.MainF.m_AutoComplete.Properties);
                    this.MainF.m_AutoComplete.KW.AddRange(this.MainF.m_AutoComplete.ShortFuncs);
                    this.MainF.m_AutoComplete.KW.Sort();
                }
            }
        }
        private string RemoveType(ref string ts, ref string t)
        {

            var y = ts.IndexOf(" ");
            if (y > 0)
            {
                t = ts.Substring(0, y);
                ts = ts.Substring(y + 1);
            }
            return ts;
        }
        public void ConvertFuncForAutoComplete(ref string s1, ref string s2)
        {
            string[] sa;
            try
            {
                if (!s1.Contains("("))
                {
                    s1 = RemoveType(ref s1, ref s2);
                }
                else
                {
                    sa = s1.Split('(');
                    sa[1] = "(" + sa[1];
                    s1 = RemoveType(ref sa[0], ref s2);
                    s2 = s2 + " " + sa[1];
                }
            }
            catch
            {
            }
        }
        public void ConvertVarForAutoComplete(ref string s1, ref string s2)
        {
            try
            {
                RemoveType(ref s1, ref s2);
            }
            catch
            {
            }
        }
        public void ConvertConstForAutoComplete(ref string s1, ref string s2)
        {
            try
            {
                if (!s1.Contains('='))
                {
                    s1 = RemoveType(ref s1, ref s2);
                }
                else
                {
                    string[] sa;

                    sa = s1.Split('=');
                    sa[1] = "=" + sa[1];
                    s1 = RemoveType(ref sa[0], ref s2).Trim();
                    s2 = s2 + " " + sa[1];
                }
            }
            catch
            {
            }
        }

        private readonly ArrayList TreeMatches = new ArrayList();
        private int currentMatch = 0;
        private void TxtSuchString_TextChanged(object sender, EventArgs e)
        {
            FindInTree(false);

        }

        private Regex rg;
        private void FindInTreeSub(TreeNode i, string Temp, bool mode)
        {
            int lokIndex;
            try
            {
                if (mode == false)
                {
                    if (i.Text.ToLower().Contains(Temp))
                    {
                        var tempnode = i.Parent;
                        lokIndex = i.Index;
                        while (tempnode.PrevNode != null)
                        {
                            lokIndex += tempnode.PrevNode.Nodes.Count;
                            tempnode = tempnode.PrevNode;
                        }
                        i.StateImageKey = Convert.ToString(lokIndex);
                        TreeMatches.Add(i);
                        i.BackColor = Color.Yellow;
                    }
                }
                else if (rg != null)
                {
                    if (rg.IsMatch(i.Text.ToLower()))
                    {
                        var tempnode = i.Parent;
                        lokIndex = i.Index;
                        while (tempnode.PrevNode != null)
                        {
                            lokIndex += tempnode.PrevNode.Nodes.Count;
                            tempnode = tempnode.PrevNode;
                        }
                        i.StateImageKey = Convert.ToString(lokIndex);
                        TreeMatches.Add(i);
                        i.BackColor = Color.Yellow;
                    }
                }
            }
            catch
            {
            }
        }
        private void FindInTree(bool mode)
        {
            foreach (TreeNode k in TreeMatches)
            {
                k.BackColor = treeMain.BackColor;
            }
            TreeMatches.Clear();
            currentMatch = 0;
            if (mode == false && TxtSuchString.Text.Length < 3)
            {
                LbFound.Text = "0";
                return;
            }
            var Temp = TxtSuchString.Text.ToLower();
            try
            {
                rg = new Regex(Temp);
            }
            catch
            {
            }
            if (ItemTree.IsExpanded)
            {
                foreach (TreeNode i in ItemTree.Nodes)
                {
                    FindInTreeSub(i, Temp, mode);
                }
            }
            if (DialogTree.IsExpanded)
            {

                foreach (TreeNode i in DialogTree.Nodes)
                {
                    FindInTreeSub(i, Temp, mode);
                }
            }
            if (NPCTree.IsExpanded)
            {
                foreach (TreeNode i in NPCTree.Nodes)
                {
                    FindInTreeSub(i, Temp, mode);
                }
            }
            if (FuncTree.IsExpanded)
            {
                foreach (TreeNode i in FuncTree.Nodes)
                {
                    FindInTreeSub(i, Temp, mode);
                }
            }
            if (VarTree.IsExpanded)
            {
                foreach (TreeNode i in VarTree.Nodes)
                {
                    FindInTreeSub(i, Temp, mode);
                }
            }
            if (ConstTree.IsExpanded)
            {
                foreach (TreeNode i in ConstTree.Nodes)
                {
                    FindInTreeSub(i, Temp, mode);
                }
            }
            if (TreeMatches.Count > 0)
            {
                treeMain.SelectedNode = (TreeNode)TreeMatches[0];
                TxtFoundIndex.Text = "1";
            }
            else
            {
                TxtFoundIndex.Text = "";
            }
            LbFound.Text = TreeMatches.Count.ToString();
        }

        private void treeMain_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && treeMain.SelectedNode != null)
            {
                treeMain_NodeMouseDoubleClick(null, new TreeNodeMouseClickEventArgs(treeMain.SelectedNode, MouseButtons.Left, 0, 0, 0));
            }
        }

        private void treeMain_AfterSelect(object sender, TreeViewEventArgs e)
        {
            e.Node.EnsureVisible();
        }

        private void BtLeft_Click(object sender, EventArgs e)
        {
            if (TreeMatches.Count > 0)
            {
                if (treeMain.SelectedNode != null)
                {
                    var globIndex = treeMain.SelectedNode.Index;
                    var tempnode = treeMain.SelectedNode.Parent;

                    while (tempnode.PrevNode != null)
                    {

                        globIndex += tempnode.PrevNode.Nodes.Count;
                        tempnode = tempnode.PrevNode;
                    }

                    int z;
                    if ((globIndex > Convert.ToInt32(((TreeNode)TreeMatches[currentMatch]).StateImageKey)))
                    {
                        z = TreeMatches.Count - 1;
                    }
                    else
                    {
                        z = currentMatch;
                    }
                    for (var i = z; i > -1; i--)
                    {
                        if (globIndex > Convert.ToInt32(((TreeNode)TreeMatches[i]).StateImageKey))
                        {
                            currentMatch = i;
                            break;
                        }
                        else
                        {
                            currentMatch = TreeMatches.Count - 1;
                        }
                    }

                }
                TxtFoundIndex.Text = (currentMatch + 1).ToString();
                treeMain.SelectedNode = (TreeNode)TreeMatches[currentMatch];
                treeMain.Focus();

            }
        }

        private void BtRight_Click(object sender, EventArgs e)
        {
            if (TreeMatches.Count > 0)
            {
                if (treeMain.SelectedNode != null)
                {
                    var globIndex = treeMain.SelectedNode.Index;
                    var tempnode = treeMain.SelectedNode.Parent;

                    while (tempnode.PrevNode != null)
                    {

                        globIndex += tempnode.PrevNode.Nodes.Count;
                        tempnode = tempnode.PrevNode;
                    }

                    int z;
                    if ((globIndex < Convert.ToInt32(((TreeNode)TreeMatches[currentMatch]).StateImageKey)))
                    {
                        z = 0;

                    }
                    else
                    {
                        z = currentMatch;
                    }
                    for (var i = z; i < TreeMatches.Count; i++)
                    {

                        if (globIndex < Convert.ToInt32(((TreeNode)TreeMatches[i]).StateImageKey))
                        {
                            currentMatch = i;
                            break;
                        }
                        else
                        {
                            currentMatch = 0;
                        }
                    }

                }
                TxtFoundIndex.Text = (currentMatch + 1).ToString();
                treeMain.SelectedNode = (TreeNode)TreeMatches[currentMatch];
                treeMain.Focus();

            }
        }

        private Regex DigitOnly = new Regex(@"\d{1}");

        private void TxtFoundIndex_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (TreeMatches.Count > 0)
                {
                    DigitOnly = new Regex(@"\d{" + TxtFoundIndex.Text.Length + "}");
                    if (!DigitOnly.IsMatch(TxtFoundIndex.Text))
                    {
                        return;
                    }
                    var FoundIndex = Convert.ToInt32(TxtFoundIndex.Text);
                    if (FoundIndex < 1)
                    {
                        FoundIndex = 1;
                    }
                    else if (FoundIndex > TreeMatches.Count)
                    {
                        FoundIndex = TreeMatches.Count;
                    }
                    TxtFoundIndex.Text = FoundIndex.ToString();
                    currentMatch = FoundIndex - 1;
                    treeMain.SelectedNode = (TreeNode)TreeMatches[currentMatch];
                    treeMain.Focus();
                }
            }
        }

        private void BtCopyTreeItem_Click(object sender, EventArgs e)
        {
            if (treeMain.SelectedNode != null)
            {
                Clipboard.SetText(treeMain.SelectedNode.Text);
            }
        }

        private void treeMain_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                if (e.Node != null)
                {
                    var Temp = "";
                    var s2 = "";
                    Temp = e.Node.Text;
                    if (e.Node.Parent == FuncTree)
                    {
                        ConvertFuncForAutoComplete(ref Temp, ref s2);
                    }
                    else if (e.Node.Parent == VarTree)
                    {
                        ConvertVarForAutoComplete(ref Temp, ref s2);
                    }
                    else if (e.Node.Parent == ConstTree)
                    {
                        ConvertConstForAutoComplete(ref Temp, ref s2);
                    }

                    Clipboard.SetText(Temp);
                }
            }
        }

        private void TxtSuchString_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.V)
            {
                TxtSuchString.Text = Clipboard.GetText();
            }

            if (e.Control && e.Alt && e.KeyCode == Keys.D9)
            {
                TxtSuchString.Text += "]";
            }
            if (e.Control && e.Alt && e.KeyCode == Keys.OemBackslash)
            {
                TxtSuchString.Text += "\\";
            }
        }

        private void mRrefresh_all_Click(object sender, EventArgs e)
        {
            GetInstancesToFile(true, true, true, true);
        }

        private void mRrefresh_dia_Click(object sender, EventArgs e)
        {
            GetInstancesToFile(true, false, false, false);
        }

        private void mRrefresh_npc_Click(object sender, EventArgs e)
        {
            GetInstancesToFile(false, true, false, false);
        }

        private void mRrefresh_items_Click(object sender, EventArgs e)
        {
            GetInstancesToFile(false, false, true, false);
        }

        private void mRrefresh_Func_Click(object sender, EventArgs e)
        {
            GetInstancesToFile(false, true, false, true);
        }

        private void BtRegex_Click(object sender, EventArgs e)
        {
            FindInTree(true);
        }
    }
    [Serializable()]
    public class Instance : IComparable, IComparable<Instance>
    {
        public string Name;
        public string File;
        public string Params;
        public int Line;
        public Instance(string s1, string s2)
        {
            Name = s1;
            File = s2;

        }

        public Instance(string s1, int i1)
        {
            Name = s1;
            Line = i1;

        }
        public override string ToString()
        {
            return Name;
        }
        int IComparable.CompareTo(object obj)
        {
            var p = obj as Instance;
            return CompareTo(p);
        }

        public int CompareTo(Instance other)
        {
            return string.Compare(this.ToString(), other.ToString(), true);
        }
    }
}
