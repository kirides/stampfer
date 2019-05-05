/**************************************************************************************
    Stampfer - Gothic Script Editor
    Copyright (C) 2008  Jpmon1

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
using System.Collections.Generic;
using System.IO;
using System.Xml;
using ICSharpCode.TextEditor.Document;

namespace Peter
{
    internal class cXmlFoldingStrategy : IFoldingStrategy
    {
        public List<FoldMarker> GenerateFoldMarkers(
            IDocument document, string fileName, object parseInformation)
        {
            var markers = new List<FoldMarker>();
            var startMarkers = new Stack<FoldStartMarker>();
            try
            {
                using (var xmlReader = new XmlTextReader(
                    new StringReader(document.TextContent)))
                {
                    while (xmlReader.Read())
                    {
                        if (xmlReader.NodeType == XmlNodeType.Element &&
                            xmlReader.IsEmptyElement == false)
                        {
                            startMarkers.Push(
                                CreateStartMarker(xmlReader));
                        }
                        else if (xmlReader.NodeType == XmlNodeType.EndElement)
                        {
                            var startMarker = startMarkers.Pop();
                            var foldMarker = CreateFoldMarker(document, xmlReader, startMarker);
                            if (foldMarker != null)
                            {
                                markers.Add(foldMarker);
                            }
                        }
                        else if (xmlReader.NodeType == XmlNodeType.Comment)
                        {
                            var foldMarker = CreateCommentFoldMarker(document, xmlReader);
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
                return (List<FoldMarker>)document.FoldingManager.FoldMarker;
            }
            return markers;
        }

        private FoldStartMarker CreateStartMarker(XmlTextReader xmlReader)
        {
            var startMarker = new FoldStartMarker(
                xmlReader.LinePosition - 2, xmlReader.LineNumber - 1,
                xmlReader.LocalName, xmlReader.Prefix);
            return startMarker;
        }

        private FoldMarker CreateFoldMarker(IDocument document, XmlTextReader reader,
            FoldStartMarker startMarker)
        {
            var endLineNumber = reader.LineNumber - 1;
            FoldMarker marker = null;
            if (endLineNumber > startMarker.LineNumber)
            {
                marker = new FoldMarker(document, startMarker.LineNumber,
                    startMarker.Column, endLineNumber, reader.LinePosition +
                    startMarker.QualifiedName.Length, FoldType.TypeBody, startMarker.FoldText);
            }
            return marker;
        }

        private FoldMarker CreateCommentFoldMarker(IDocument document, XmlTextReader xmlReader)
        {
            FoldMarker marker = null;
            var comment = xmlReader.Value;
            if (string.IsNullOrEmpty(comment) == false)
            {
                var lines = comment.Replace(Environment.NewLine, "\n").Split('\n');
                if (lines.Length > 1)
                {
                    var startLineNumber = xmlReader.LineNumber - 1;
                    var startColumn = xmlReader.LinePosition - 5;
                    var endLine = startLineNumber + lines.Length - 1;
                    var endColumn = lines[lines.Length - 1].Length + 3;
                    var foldText = "...";
                    marker = new FoldMarker(document, startLineNumber, startColumn,
                        endLine, endColumn, FoldType.TypeBody, foldText);
                }
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
                    return string.Concat(
                        "<", QualifiedName, ">", "...");
                }
            }

            public string QualifiedName
            {
                get
                {
                    return string.IsNullOrEmpty(_prefix) ?
                        _name : _prefix + ":" + _name;
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
