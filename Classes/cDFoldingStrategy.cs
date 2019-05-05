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
using System.Collections.Generic;
using System.Text.RegularExpressions;
using ICSharpCode.TextEditor.Document;
namespace Peter
{
    internal class cDFoldingStrategy : IFoldingStrategy
    {
        private Regex lbrace = new Regex(@"{");
        private Regex rbrace = new Regex(@"}");

        public List<FoldMarker> GenerateFoldMarkers(
            IDocument document, string fileName, object parseInformation)
        {
            var markers = new List<FoldMarker>();
            var startMarkers = new Stack<FoldStartMarker>();
            try
            {
                var MatchListO = new List<int>();
                var MatchListC = new List<int>();
                string text;
                string comment;
                var temp = 0;
                var auf = false;
                for (var i = 0; i < document.TotalNumberOfLines; i++)
                {
                    text = document.GetText(document.GetLineSegment(i));

                    if (!auf && (text.Contains("/*")))
                    {
                        temp = text.IndexOf("/*");
                        if ((text.Contains("//"))
                        && (text.IndexOf("//") < temp))
                        {
                        }
                        else
                        {
                            auf = true;
                            MatchListO.Add(temp + document.GetLineSegment(i).Offset);
                        }
                    }
                    if (auf && (text.Contains("*/")))
                    {
                        temp = text.IndexOf("*/");
                        if ((temp <= 0 || text[temp - 1] != '/'))
                        {
                            auf = false;
                            MatchListC.Add(temp + document.GetLineSegment(i).Offset);
                        }
                    }
                }

                // Create foldmarkers for the whole document, enumerate through every line.
                for (var i = 0; i < document.TotalNumberOfLines; i++)
                {
                    // Get the text of current line.
                    text = document.GetText(document.GetLineSegment(i));
                    comment = "";

                    var ml = lbrace.Matches(text);
                    foreach (Match m in ml)
                    {
                        if (!IsInComment(document, i, "{", MatchListO, MatchListC, m))
                        {

                            startMarkers.Push(
                                CreateStartMarker(document, i, "{", "", m.Index));
                        }
                    }

                    if (text.Contains("/*")) // Look for method starts
                    {
                        temp = text.IndexOf("/*");
                        comment = text.Substring(temp + 2);
                        if (!((text.Contains("//"))
                            && (text.IndexOf("//") < temp)))
                        {
                            startMarkers.Push(
                                CreateStartMarker(document, i, "/*", comment, temp));
                        }
                    }
                    if (text.Contains("//:+")) // Look for method starts
                    {
                        temp = text.IndexOf("//:+");
                        comment = text.Substring(temp + 4);

                        startMarkers.Push(
                            CreateStartMarker(document, i, "//:+", comment, temp));
                    }
                    if (text.Contains("//:-")) // Look for method starts
                    {
                        var startMarker = startMarkers.Pop();
                        var foldMarker = CreateFoldMarker(document, i, startMarker, text.IndexOf("//:-"), "//:-", false);
                        if (foldMarker != null)
                        {
                            markers.Add(foldMarker);
                        }
                    }
                    if (text.Contains("*/")) // Look for method starts
                    {
                        temp = text.IndexOf("*/");
                        if (!((text.Contains("//"))
                            && (text.IndexOf("//") < temp)))
                        {
                            if ((text.IndexOf("*/") <= 0 || text[temp - 1] != '/'))
                            {

                                var startMarker = startMarkers.Pop();
                                var foldMarker = CreateFoldMarker(document, i, startMarker, temp, "*/", false);
                                if (foldMarker != null)
                                {
                                    markers.Add(foldMarker);
                                }
                            }
                        }
                    }

                    var mr = rbrace.Matches(text);
                    foreach (Match m in mr)
                    {
                        if (!IsInComment(document, i, "}", MatchListO, MatchListC, m))
                        {
                            var startMarker = startMarkers.Pop();
                            var foldMarker = CreateFoldMarker(document, i, startMarker, m.Index, "}", true);
                            if (foldMarker != null)
                            {
                                markers.Add(foldMarker);
                            }
                        }
                    }
                }
            }
            catch
            {
            }
            return markers;
        }

        private bool IsInComment(IDocument document, int i, string s, List<int> mo, List<int> mc, Match m)
        {
            var text = document.GetText(document.GetLineSegment(i));

            if (text.Contains("//"))
            {
                if (text.IndexOf("//") < m.Index)
                {
                    return true;
                }
            }

            var l = m.Index + document.GetLineSegment(i).Offset;
            var k = 0;

            while (k < mc.Count)
            {
                if (mc[k] > l)
                {
                    if (mo[k] < l)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                k++;
            }

            return false;
        }

        private FoldStartMarker CreateStartMarker(IDocument document, int i, string s, string t, int pos)
        {
            //string text = document.GetText(document.GetLineSegment(i));
            var startMarker = new FoldStartMarker(
                pos /*+ s.Length*/, i,
                t, "");
            return startMarker;
        }

        private FoldMarker CreateFoldMarker(IDocument document, int i,
            FoldStartMarker startMarker, int pos, string s2, bool b)
        {
            var text = document.GetText(document.GetLineSegment(i));
            var endLineNumber = i;
            FoldMarker marker = null;
            if (endLineNumber > startMarker.LineNumber)
            {
                marker = new FoldMarker(document, startMarker.LineNumber,
                    startMarker.Column, endLineNumber, text.Length, b ? FoldType.TypeBody : FoldType.Region, startMarker.FoldText);
            }
            return marker;
        }

        private struct FoldStartMarker
        {
            private string _name;
            private string _prefix;

            public int Column { get; }
            public int LineNumber { get; }

            public string FoldText
            {
                get
                {
                    return (_name.Length > 0 ? (_name) : "...");
                }
            }

            public FoldStartMarker(int column, int lineNumber,
                string name, string prefix)
            {
                Column = column;
                LineNumber = lineNumber;
                _name = name;
                _prefix = prefix;
            }
        }
    }

}
