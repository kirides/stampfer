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
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Peter.Classes
{
    public class AutoComplete : Label
    {
        private const string KEYWORD_DATEIPFAD = "KeyWords";
        private const string KEYWORDDATEI = "KeyWords";
        private const string KEYWORDDATEI_PREFIX = "KeyWords.";
        private const string PROPDATEI = "KeyWords\\Properties";
        private const string FUNCDIR = "DialogCreator\\Funcs.txt";
        public List<KeyWord> KeyWordsList = new List<KeyWord>();
        public List<KeyWord> KeyWords = new List<KeyWord>();
        private readonly ToolTip _toolTip;
        public AutoCompleteListView listView1;
        private const int ItemHeight = 16;
        public int LastItemIndex;

        public int CaretPos = 0;
        private Editor currentEditor = null;
        public Hashtable ShortFuncList = new Hashtable();
        public List<KeyWord> ShortFuncs = new List<KeyWord>();
        public List<KeyWord> Properties = new List<KeyWord>();
        private readonly Dictionary<string, string> DokuTable = new Dictionary<string, string>();
        public string Extension = ".d";
        private readonly ImageList ImgList;
        private readonly ToolStripStatusLabel Trace;
        public string ScriptsPath;

        public AutoComplete(ImageList img, ToolStripStatusLabel tlt)
        {
            listView1 = new AutoCompleteListView(this);
            _toolTip = new ToolTip();

            ImgList = img;
            Trace = tlt;
            LastItemIndex = 0;
            _toolTip.Hide(this);
            M_AutoCompleteDa.Active = true;
            UpdateContent();
            this.Width = Sizes.Width;

            this.Height = Sizes.Height;
            this.SizeChanged += AutoComplete_SizeChanged;
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.DoubleBuffer, true);
            SetupListview(KeyWords.Count);
            Read_KeyWords();
        }

        public void SelectedChanged()
        {
            listView1_SelectedIndexChanged(null, new EventArgs());
        }
        public void MouseInsert()
        {

            if (listView1.SelectedIndices.Count > 0
                && currentEditor != null)
            {
                currentEditor.InsertCompletion();
                HidePopup();
            }
        }
        public void MouseRemove()
        {
            currentEditor.m_Editor.Focus();
            HidePopup();
        }

        private void AutoComplete_SizeChanged(object sender, EventArgs e)
        {
            if (this.listView1 != null)
            {
                this.listView1.Height = this.Height;
            }
        }

        private void listView_RetrieveVirtualItem(object sender, RetrieveVirtualItemEventArgs e)
        {
            var item = new ListViewItem(KeyWords[e.ItemIndex].ToString(), KeyWords[e.ItemIndex].Type);
            e.Item = item;
        }
        private void SetupListview(int lenght)
        {
            if (listView1 != null && this.Controls.Contains(listView1))
            {
                this.Controls.Remove(listView1);
            }
            this.listView1 = new AutoCompleteListView(this);
            listView1.SmallImageList = ImgList;

            this.listView1.UseCompatibleStateImageBehavior = false;
            this.listView1.ShowGroups = true;

            this.listView1.Dock = DockStyle.Fill;
            this.listView1.RetrieveVirtualItem +=
                    new RetrieveVirtualItemEventHandler(
                    listView_RetrieveVirtualItem);
            this.listView1.VirtualListSize = lenght;
            this.listView1.VirtualMode = true;
            this.listView1.HideSelection = false;

            this.Controls.Add(this.listView1);
            this.listView1.BringToFront();
            this.listView1.MultiSelect = false;
            listView1.View = View.Details;
            listView1.HeaderStyle = ColumnHeaderStyle.None;
            var h = new ColumnHeader
            {
                Width = listView1.ClientSize.Width - SystemInformation.VerticalScrollBarWidth
            };
            listView1.Columns.Add(h);
        }
        private void SetupListview2(int lenght)
        {
            this.listView1.VirtualListSize = lenght;
        }
        public void NewKeyWordFile(string extension)
        {
            KeyWords.Clear();
            _toolTip.Hide(this);
            var keywordFilePath = Path.Combine(KEYWORD_DATEIPFAD, KEYWORDDATEI + Extension);
            if (File.Exists(keywordFilePath))
            {
                Extension = extension;
                LastItemIndex = 0;
                listView1_SelectedIndexChanged(this, EventArgs.Empty);
            }
        }
        public void UpdatePos(Editor e)
        {
            this.Left = 10 + e.GetCaretPos().X;
            this.Top = e.GetCaretPos().Y + (int)(e.Font.Size * 4f);
            if (e.DiaC != null)
            {
                if (e.DiaC.Ed_Active == 1)
                {
                    this.Top += e.DiaC.pCondition.Top + e.DiaC._grprCondition.Top;
                    this.Left += e.DiaC.pCondition.Left + e.DiaC._grprCondition.Left;
                }
                else
                {
                    this.Top += e.DiaC.pInfo.Top + e.DiaC._grprInfo.Top;
                    this.Left += e.DiaC.pInfo.Left + e.DiaC._grprInfo.Left;
                }
            }
            listView1_SelectedIndexChanged(this, EventArgs.Empty);
        }
        public void UpdateSize()
        {
            if (KeyWords.Count * 16 < Sizes.Height)
            {
                this.Height = KeyWords.Count * 16 + 16;
            }
            else
            {
                this.Height = Sizes.Height;
            }
        }

        public int GetFistKeyWordMatch(int i1, int i2, string s)
        {
            var center = (i1 + i2) / 2;
            if ((i2 - i1) > 3)
            {
                var ck = KeyWordsList[center].Name;
                if (ck.Length > s.Length)
                {
                    ck = ck.Substring(0, s.Length);
                }
                if (string.Compare(ck, s, true) > 0)//Wenn KW kleiner s
                {
                    return GetFistKeyWordMatch(i1, center + 1, s);
                }
                else if (string.Compare(ck, s, true) < 0)//Wenn KW größer s
                {
                    return GetFistKeyWordMatch(center - 1, i2, s);
                }
                else
                {
                    return center;
                }
            }
            else
            {
                for (var i = i1; i < i2; i++)
                {
                    if (KeyWordsList[i].Name.StartsWith(s, StringComparison.OrdinalIgnoreCase))
                    {
                        return i;
                    }
                }
                return -1;
            }
        }

        public Point FindKeyWords(string s)
        {
            var p = new Point(-1, -1);
            var p2 = new Point(-1, -1);
            var p3 = new Point(-1, -1);
            int l1;

            l1 = GetFistKeyWordMatch(0, KeyWordsList.Count - 1, s);

            if (l1 >= 0)
            {
                p = new Point(l1, l1);
                if (l1 > 0)
                {
                    p2.X = p.X;
                    while (p2.X > 0)
                    {
                        p2.X = GetFistKeyWordMatch(0, p2.X, s);
                        if (p2.X >= 0)
                        {
                            p3.X = p2.X;
                        }
                    }
                    p2.X = p3.X;
                }
                if (l1 < KeyWordsList.Count - 1)
                {
                    p2.Y = p.Y;
                    while (p2.Y >= 0 && p2.Y < KeyWordsList.Count - 1)
                    {
                        p2.Y = GetFistKeyWordMatch(p2.Y + 1, KeyWordsList.Count - 1, s);

                        if (p2.Y >= 0)
                        {
                            p3.Y = p2.Y;
                        }
                    }
                    p2.Y = p3.Y;
                }
            }
            if (p2.X >= 0)
            {
                p.X = p2.X;
            }
            if (p2.Y >= 0)
            {
                p.Y = p2.Y;
            }
            return p;
        }
        public void UpdateContent(string s)
        {
            var StartEnd = new Point(-1, -1);
            this.KeyWords.Clear();
            this.listView1.BeginUpdate();
            if (s.Trim().Length > 0)
            {
                StartEnd = FindKeyWords(s);
            }
            if (StartEnd.X >= 0 && StartEnd.Y >= 0)
            {
                for (var z = StartEnd.X; z <= StartEnd.Y; z++)
                {
                    this.KeyWords.Add(KeyWordsList[z]);
                }
            }
            SetupListview2(KeyWords.Count);
            UpdateSize();
            this.listView1.EndUpdate();

            if (this.listView1 != null)
            {
                if (this.KeyWords.Count > 0)
                {

                    this.listView1.Items[0].Selected = true;
                    this.listView1.TopItem = listView1.Items[0];
                    listView1_SelectedIndexChanged(null, new EventArgs());
                }
                else
                {
                    _toolTip.Hide(this);
                }
            }
        }
        public void UpdateContent()
        {
            if (this.listView1 != null)
            {
                if (this.KeyWords.Count > 0)
                {
                    this.listView1.Items[0].Selected = true;
                    this.listView1.TopItem = listView1.Items[0];
                    listView1_SelectedIndexChanged(this, EventArgs.Empty);
                }
            }
        }
        public void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if ((this.KeyWords.Count <= 0)
                || (this.listView1.SelectedIndices.Count <= 0)
                || (this.KeyWords[listView1.SelectedIndices[0]] == null))
            {
                return;
            }

            var keyWord = this.KeyWords[this.listView1.SelectedIndices[0]];
            string s;
            s = keyWord.Name.ToLower();
            if (DokuTable.TryGetValue(s, out var text) && text != null)
            {
                keyWord.Text2 = Convert.ToString(text);
            }

            _toolTip.ToolTipTitle = keyWord.Text1;
            if (this.Left + (this.Width / 2) < Screen.PrimaryScreen.WorkingArea.Width / 2)
            {
                _toolTip.Show(keyWord.Text2, this, this.Width, listView1.Items[listView1.SelectedIndices[0]].Position.Y);
            }
            else
            {
                var Templabel = new Label();
                var Templabel2 = new Label();
                this.Controls.Add(Templabel);
                this.Controls.Add(Templabel2);
                Templabel.AutoSize = true;
                Templabel2.AutoSize = true;

                Templabel.Font = new System.Drawing.Font(Templabel.Font, System.Drawing.FontStyle.Bold);
                Templabel.Text = keyWord.Text1;

                Templabel2.Text = keyWord.Text2;
                if (Templabel.Width > Templabel2.Width)
                {
                    _toolTip.Show((keyWord).Text2, this, -(Templabel.Width) - 25, listView1.Items[listView1.SelectedIndices[0]].Position.Y);
                }
                else
                {
                    _toolTip.Show((keyWord).Text2, this, -(Templabel2.Width) - 25, listView1.Items[listView1.SelectedIndices[0]].Position.Y);
                }

                this.Controls.Remove(Templabel);
                this.Controls.Remove(Templabel2);
                Templabel.Dispose();
                Templabel2.Dispose();
            }

            LastItemIndex = this.listView1.SelectedIndices[0];
            var TraceWord = KeyWords[this.listView1.SelectedIndices[0]];
            if (TraceWord.Type == 3)
            {
                Trace.Text = TraceWord.Name + "   " + TraceWord.Text1;
            }
            else if (TraceWord.Type == 6)
            {
                Trace.Text = TraceWord.Name + "   " + TraceWord.Text2.Replace("\n", " ");
            }
        }
        private KeyWord Deconstruct_Line(string l)
        {
            var kw = new KeyWord();
            var s = l.Split('@');
            kw.Name = s[0];
            kw.Text2 = " ";
            for (var i = 1; i < s.Length; i++)
            {
                if (i == s.Length - 1)
                {
                    kw.Text1 += s[i];
                }
                else
                {
                    kw.Text1 += s[i] + "\n";
                }
            }
            return kw;
        }
        private void Read_KeyWords()
        {
            var doku = new List<KeyWord>();
            this.listView1.Clear();
            this.KeyWords.Clear();
            this.KeyWordsList.Clear();

            var keywordFilePath = Path.Combine(KEYWORD_DATEIPFAD, KEYWORDDATEI + Extension);
            if (Extension == ".d")
            {
                if (File.Exists(keywordFilePath))
                {
                    foreach (var keyWordFile in new[] { keywordFilePath }.Concat(Directory.GetFiles(KEYWORD_DATEIPFAD, KEYWORDDATEI_PREFIX + '*' + Extension)))
                    {
                        using (var sr = new StreamReader(keyWordFile, Encoding.Default))
                        {
                            string line;
                            while ((line = sr.ReadLine()) != null)
                            {
                                var k = Deconstruct_Line(line);
                                doku.Add(k);
                            }
                        }
                    }
                }
                
                if (File.Exists(Path.GetDirectoryName(Application.ExecutablePath) + "\\" + PROPDATEI + Extension))
                {
                    using (var sr = new StreamReader(Path.GetDirectoryName(Application.ExecutablePath) + "\\" + PROPDATEI + Extension, Encoding.Default))
                    {
                        string line;
                        while ((line = sr.ReadLine()) != null)
                        {
                            var k = Deconstruct_Line(line);

                            k.Type = 7;
                            Properties.Add(k);
                        }
                    }
                }

                DokuTable.Clear();
                foreach (var l in doku)
                {
                    DokuTable[l.Name.ToLower()] = l.Text1;
                }

                ReadShortFunc();
            }

            keywordFilePath = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), KEYWORD_DATEIPFAD, KEYWORDDATEI + Extension);
            if (File.Exists(keywordFilePath))
            {
                foreach (var keyWordFile in new[] { keywordFilePath }.Concat(Directory.GetFiles(Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), KEYWORD_DATEIPFAD), KEYWORDDATEI_PREFIX + '*' + Extension)))
                {
                    using (var sr = new StreamReader(keyWordFile, Encoding.Default))
                    {
                        string line;
                        while ((line = sr.ReadLine()) != null)
                        {
                            var k = Deconstruct_Line(line);
                            this.KeyWordsList.Add(k);
                        }
                    }
                }
                SetupListview(KeyWords.Count);
            }
            else
            {
                MessageBox.Show("Die Datei " + keywordFilePath + " konnte nicht gefunden werden!", "Fehler!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        public void ReadShortFunc()
        {
            if (File.Exists(FUNCDIR))
            {
                string line;
                var mode = 0;
                var i = 0;
                var isloop = false;
                var startloop = -1;
                var endloop = -1;

                var name = "";
                using (var sr = new StreamReader(FUNCDIR, Encoding.Default))
                {
                    while ((line = sr.ReadLine()) != null)
                    {
                        var parameterListe = new List<Parameter>();
                        while (i < line.Length)
                        {
                            if (mode == 0)
                            {
                                if (line[0] != '@')
                                {
                                    name += line[0];
                                }
                                else
                                {
                                    mode = 1;
                                    if (0 < line.Length && line[0 + 1] == '@')
                                    {
                                        isloop = true;
                                    }
                                    line = line.Remove(0, name.Length + (isloop ? 2 : 1));
                                    i = 0;
                                    break;
                                }
                            }
                            i++;
                        }
                        if (isloop)
                        {
                            i = 0;
                            while (i < line.Length)
                            {
                                if (line[0] == '|')
                                {
                                    if (startloop == -1)
                                    {
                                        startloop = 0;
                                    }
                                    else if (endloop == -1)
                                    {
                                        endloop = 0 - 1;
                                        line = line.Remove(startloop, 1);
                                        line = line.Remove(endloop, 1);
                                        break;
                                    }
                                }
                                i++;
                            }
                        }
                        i = 0;
                        while (i < line.Length)
                        {

                            if (line[0] == '#')
                            {
                                i++;
                                var temp = "";
                                while (0 < line.Length)
                                {
                                    if (char.IsDigit(line[0]))
                                    {
                                        temp += line[0];
                                    }
                                    else
                                    {
                                        break;
                                    }
                                    i++;
                                }
                                if (isloop)
                                {
                                    if (0 < startloop)
                                    {
                                        startloop -= temp.Length + 1;
                                    }
                                    if (0 < endloop)
                                    {
                                        endloop -= temp.Length + 1;
                                    }
                                    ;
                                }
                                var found = false;
                                var p1 = new Parameter();
                                var pl = new Parameter();

                                foreach (var p in parameterListe)
                                {
                                    if (p.ParamName == temp)
                                    {
                                        found = true;
                                        p1 = p;
                                        break;
                                    }
                                }

                                if (found == true)
                                {
                                    p1.ParamPos.Add(0 - temp.Length - 1);
                                }
                                else
                                {
                                    p1.ParamName = temp;
                                    p1.ParamPos.Add(0 - temp.Length - 1);
                                    parameterListe.Add(p1);
                                }

                                line = line.Remove(0 - (temp.Length + 1), temp.Length + 1);
                                i = 0 - (temp.Length + 1);
                            }
                            i++;
                        }

                        line = line.Replace("§", "\n");
                        var sfu = new ShortFunc
                        {
                            Short = name,
                            FuncText = line,
                            loopstart = startloop,
                            loopend = endloop
                        };

                        foreach (var p in parameterListe)
                        {
                            sfu.Params.Add(p);
                        }

                        if (!ShortFuncList.Contains(name))
                        {
                            ShortFuncList.Add(name, sfu);
                        }

                        var k = new KeyWord
                        {
                            Name = "#" + name
                        };
                        if (startloop >= 0)
                        {
                            k.Text1 = "Loop-Kurzfunktion";
                        }
                        else
                        {
                            k.Text1 = "Kurzfunktion";
                        }
                        k.Text2 = line;
                        k.Type = 6;
                        ShortFuncs.Add(k);
                        mode = 0;
                        i = 0;
                    }
                }
            }
        }

        private int i2;
        private string s2;

        public string TransformShortFunc(Editor e)
        {
            var s = e.m_Editor.ActiveTextAreaControl.Document.TextContent;
            var oldcaretpos = e.m_Editor.ActiveTextAreaControl.Caret.Line;
            s = TransformShortFunc(s);
            e.m_Editor.ActiveTextAreaControl.Caret.Line = oldcaretpos;
            return s;
        }
        public string TransformShortFunc(string s)
        {
            var r = new Regex("#");
            Match m;
            int i, l, l2, l3, n;
            var loopbuilder = "";
            var parameterListe = new List<string>();
            var parameterPosListe = new List<ParameterPos>();
            ParameterPos ppos;
            n = 0;
            while ((m = r.Match(s, n)).Success)
            {
                i = m.Index + 1;
                var temp = "";
                while (i < s.Length)
                {
                    if (!char.IsWhiteSpace(s[i]))
                    {
                        temp += s[i];
                        i++;
                    }
                    else
                    {
                        i++;
                        break;
                    }
                }
                temp = temp.ToLower();
                if (ShortFuncList.Contains(temp))
                {
                    var sf = ((ShortFunc)ShortFuncList[temp]);
                    parameterListe.Clear();
                    temp = "";
                    while (i < s.Length)
                    {
                        if (!char.IsWhiteSpace(s[i]) /*&& s[i] != ';' && s[i] != '='*/)
                        {
                            if (s[i] == ',')
                            {
                                parameterListe.Add(temp.Replace("§", "\n"));
                                temp = "";
                            }
                            else if (i == s.Length - 1)
                            {
                                temp += s[i];
                                parameterListe.Add(temp.Replace("§", "\n"));
                                temp = "";
                            }
                            else
                            {
                                temp += s[i];
                            }
                            i++;
                        }
                        else
                        {
                            parameterListe.Add(temp);
                            i++;
                            break;
                        }
                    }
                    var isloop = sf.loopstart != -1;
                    var builder = sf.FuncText;

                    l = 0;
                    l2 = 0;
                    l3 = 0;
                    parameterPosListe.Clear();
                    foreach (Parameter p in sf.Params)
                    {
                        foreach (int Pos in p.ParamPos)
                        {
                            try
                            {
                                ppos.pos = Pos;
                                ppos.parameter = parameterListe[Convert.ToInt32(p.ParamName)].ToString();
                                parameterPosListe.Add(ppos);
                            }
                            catch
                            {
                            }
                        }
                    }

                    parameterPosListe.Sort();
                    foreach (var p in parameterPosListe)
                    {
                        builder = builder.Insert(p.pos + l, p.parameter);

                        if (p.pos <= sf.loopstart)
                        {
                            l2 += p.parameter.Length;
                        }
                        else if (p.pos <= sf.loopend)
                        {
                            l3 += p.parameter.Length;
                        }

                        l += p.parameter.Length;
                    }
                    if (isloop)
                    {
                        loopbuilder = builder.Substring(sf.loopstart + l2, (sf.loopend + l2 + l3) - (sf.loopstart + l2));
                        builder = builder.Remove(sf.loopstart + l2, (sf.loopend + +l2 + l3) - (sf.loopstart + l2));
                    }

                    i2 = m.Index;
                    s2 = "";
                    while (i2 > 0)
                    {
                        if (s[i2] == '\t')
                        {
                            s2 += "\t";
                        }
                        else if (s[i2] == ' ')
                        {
                            s2 += " ";
                        }
                        else if (s[i2] == '\n')
                        {
                            break;
                        }
                        i2--;
                    }
                    temp = "";
                    if (isloop && parameterListe.Count > 1)
                    {
                        try
                        {
                            for (var y = 0; y < Convert.ToInt32(parameterListe[parameterListe.Count - 2].ToString()); y++)
                            {
                                temp += loopbuilder.Replace("%", (y + Convert.ToInt32(parameterListe[parameterListe.Count - 1])).ToString());
                            }
                            loopbuilder = temp;
                        }
                        catch
                        {
                        }
                    }

                    if (isloop)
                    {
                        builder = builder.Insert(sf.loopstart + l2, loopbuilder);
                    }
                    builder = builder.Replace("\n", "\n" + s2);
                    n = m.Index + builder.Length;
                    s = s.Remove(m.Index, i - m.Index);
                    s = s.Insert(m.Index, builder);
                }
                else
                {
                    n = m.Index + 1;
                }
                if (n > s.Length) break;

            }
            return s;
        }

        public void HidePopup()
        {
            this.Visible = false;
            try { _toolTip.Hide(this); }
            catch { }
            M_AutoCompleteDa.Active = false;
            LastItemIndex = 0;
        }
        public void ShowPopup(Editor e)
        {
            currentEditor = e;
            UpdatePos(e);
            CaretPos = e.m_Editor.ActiveTextAreaControl.Caret.Offset;
            this.Visible = true;
            M_AutoCompleteDa.Active = true;
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.ResumeLayout(false);
        }
        public void SaveKW()
        {
            var kwsave = new KWSave
            {
                KW = new KeyWord[KeyWordsList.Count]
            };
            KeyWordsList.CopyTo(kwsave.KW);
            using (var myStream = new FileStream(ScriptsPath + Global.KW, FileMode.Create))
            {
                var binFormatter = new BinaryFormatter();
                binFormatter.Serialize(myStream, kwsave);
            }
            return;
        }
    }

    [Serializable()]
    public class KeyWord : IComparable
    {
        public string Name;
        public string Text1;
        public string Text2;
        public int Type = 0;
        public KeyWord() { }
        public KeyWord(string n, int typ, string t1, string t2)
        {
            Name = n;
            Type = typ;
            Text1 = t1;
            Text2 = t2;
        }
        public override string ToString()
        {
            return Name;
        }
        int IComparable.CompareTo(object obj)
        {
            var p = obj as KeyWord;
            return string.Compare(Name, p.Name, true);
        }
    }
    public class AutoCompleteListView : ListView
    {
        public AutoComplete auto;
        public AutoCompleteListView(AutoComplete a)
        {
            auto = a;
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.DoubleBuffer, true);
            this.Height = Sizes.Height;
            this.Width = Sizes.Width;
        }
        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            base.OnMouseDoubleClick(e);
            auto.MouseInsert();
        }
        protected override void OnSelectedIndexChanged(EventArgs e)
        {
            base.OnSelectedIndexChanged(e);
            auto.SelectedChanged();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Escape)
            {
                auto.MouseRemove();
            }
            else if (e.KeyCode == Keys.Enter)
            {
                auto.MouseInsert();
            }
        }
    }

    internal static class Sizes
    {
        public const int Height = 300;
        public const int Width = 250;
    }
    [Serializable()]
    public class KWSave
    {
        public KeyWord[] KW;
        public KWSave()
        {
        }
    }
}
