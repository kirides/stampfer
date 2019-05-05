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
using System.Drawing;

namespace Peter
{
    public class Common
    {
        /// <summary>
        /// Gets the Shell Icon for the given file...
        /// </summary>
        /// <param name="name">Path to file.</param>
        /// <param name="linkOverlay">Shortcut Overlay or not</param>
        /// <returns>Icon</returns>
        public static System.Drawing.Icon GetFileIcon(string name, bool linkOverlay)
        {
            var shfi = new cShell32.SHFILEINFO();
            var flags = cShell32.SHGFI_ICON | cShell32.SHGFI_USEFILEATTRIBUTES;

            if (linkOverlay) flags += cShell32.SHGFI_LINKOVERLAY;
            // flags += cShell32.SHGFI_SMALLICON; // include the small icon flag

            cShell32.SHGetFileInfo(name,
                                  cShell32.FILE_ATTRIBUTE_NORMAL,
                                  ref shfi,
                                  (uint)System.Runtime.InteropServices.Marshal.SizeOf(shfi),
                                  flags);

            // Copy (clone) the returned icon to a new object, thus allowing us 
            // to call DestroyIcon immediately
            try
            {
                var icon = (System.Drawing.Icon)System.Drawing.Icon.FromHandle(shfi.hIcon).Clone();
                User32.DestroyIcon(shfi.hIcon); // Cleanup
                return icon;
            }
            catch { return null; }
        }

        public static StringFormat StringFormatAlignment(ContentAlignment textalign)
        {
            var sf = new StringFormat();
            switch (textalign)
            {
                case ContentAlignment.TopLeft:
                case ContentAlignment.TopCenter:
                case ContentAlignment.TopRight:
                    sf.LineAlignment = StringAlignment.Near;
                    break;
                case ContentAlignment.MiddleLeft:
                case ContentAlignment.MiddleCenter:
                case ContentAlignment.MiddleRight:
                    sf.LineAlignment = StringAlignment.Center;
                    break;
                case ContentAlignment.BottomLeft:
                case ContentAlignment.BottomCenter:
                case ContentAlignment.BottomRight:
                    sf.LineAlignment = StringAlignment.Far;
                    break;
            }
            switch (textalign)
            {
                case ContentAlignment.TopLeft:
                case ContentAlignment.MiddleLeft:
                case ContentAlignment.BottomLeft:
                    sf.Alignment = StringAlignment.Near;
                    break;
                case ContentAlignment.TopCenter:
                case ContentAlignment.MiddleCenter:
                case ContentAlignment.BottomCenter:
                    sf.Alignment = StringAlignment.Center;
                    break;
                case ContentAlignment.TopRight:
                case ContentAlignment.MiddleRight:
                case ContentAlignment.BottomRight:
                    sf.Alignment = StringAlignment.Far;
                    break;
            }
            return sf;
        }

        /// <summary>
        /// Struct for the configuration settings for a editor...
        /// </summary>
        public struct EditorConfig
        {
            public bool ShowEOL;
            public bool ShowInvalidLines;
            public bool ShowSpaces;
            public bool ShowTabs;
            public bool ShowMatchingBracket;
            public bool ShowLineNumbers;
            public bool ShowVRuler;
            public bool ShowHRuler;
            public bool EnableCodeFolding;
            public bool ConvertTabs;
            public bool UseAntiAlias;
            public bool AllowCaretBeyondEOL;
            public bool HighlightCurrentLine;
            public bool AutoInsertBracket;
            public int TabIndent;
            public int VerticalRulerCol;
            public string IndentStyle;
            public string BracketMatchingStyle;
            public bool Backup;
            public bool AutoCompleteAuto;

            public Font EditorFont;
        };
    }
}
