using System.Collections.Generic;
using System.Xml.Serialization;

namespace Peter.Classes.Configuration
{
    [XmlRoot(ElementName = "Application")]
    public class Application
    {
        [XmlElement(ElementName = "SaveOnExit")]
        public bool SaveOnExit { get; set; }
        [XmlElement(ElementName = "Top")]
        public int Top { get; set; }
        [XmlElement(ElementName = "Left")]
        public int Left { get; set; }
        [XmlElement(ElementName = "Width")]
        public int Width { get; set; }
        [XmlElement(ElementName = "Height")]
        public int Height { get; set; }
        [XmlElement(ElementName = "RecentFileCount")]
        public int RecentFileCount { get; set; }
        [XmlElement(ElementName = "RecentProjectCount")]
        public int RecentProjectCount { get; set; }
    }

    [XmlRoot(ElementName = "project")]
    public class Project
    {
        [XmlElement(ElementName = "name")]
        public string Name { get; set; }
        [XmlElement(ElementName = "file")]
        public string File { get; set; }
    }

    [XmlRoot(ElementName = "RecentProjects")]
    public class RecentProjects
    {
        [XmlElement(ElementName = "project")]
        public List<Project> Project { get; set; }
    }

    [XmlRoot(ElementName = "RecentFiles")]
    public class RecentFiles
    {
        [XmlElement(ElementName = "file")]
        public List<string> File { get; set; }
    }

    [XmlRoot(ElementName = "Comment")]
    public class Comment
    {
        [XmlAttribute(AttributeName = "name")]
        public string Name { get; set; }
        [XmlAttribute(AttributeName = "begin")]
        public string Begin { get; set; }
        [XmlAttribute(AttributeName = "end")]
        public string End { get; set; }
    }

    [XmlRoot(ElementName = "LineComments")]
    public class LineComments
    {
        [XmlElement(ElementName = "Comment")]
        public List<Comment> Comment { get; set; }
    }

    [XmlRoot(ElementName = "Code")]
    public class Code
    {
        [XmlElement(ElementName = "LineComments")]
        public LineComments LineComments { get; set; }
    }

    [XmlRoot(ElementName = "Editor")]
    public class Editor
    {
        [XmlElement(ElementName = "ShowEOL")]
        public bool ShowEOL { get; set; }
        [XmlElement(ElementName = "ShowInvalidLines")]
        public bool ShowInvalidLines { get; set; }
        [XmlElement(ElementName = "ShowSpaces")]
        public bool ShowSpaces { get; set; }
        [XmlElement(ElementName = "ShowTabs")]
        public bool ShowTabs { get; set; }
        [XmlElement(ElementName = "ShowMatchBracket")]
        public bool ShowMatchBracket { get; set; }
        [XmlElement(ElementName = "ShowLineNumbers")]
        public bool ShowLineNumbers { get; set; }
        [XmlElement(ElementName = "ShowHRuler")]
        public bool ShowHRuler { get; set; }
        [XmlElement(ElementName = "ShowVRuler")]
        public bool ShowVRuler { get; set; }
        [XmlElement(ElementName = "EnableCodeFolding")]
        public bool EnableCodeFolding { get; set; }
        [XmlElement(ElementName = "ConvertTabs")]
        public bool ConvertTabs { get; set; }
        [XmlElement(ElementName = "UseAntiAlias")]
        public bool UseAntiAlias { get; set; }
        [XmlElement(ElementName = "AllowCaretBeyondEOL")]
        public bool AllowCaretBeyondEOL { get; set; }
        [XmlElement(ElementName = "HighlightCurrentLine")]
        public bool HighlightCurrentLine { get; set; }
        [XmlElement(ElementName = "AutoInsertBracket")]
        public bool AutoInsertBracket { get; set; }
        [XmlElement(ElementName = "TabIndent")]
        public int TabIndent { get; set; }
        [XmlElement(ElementName = "VerticalRulerCol")]
        public int VerticalRulerCol { get; set; }
        [XmlElement(ElementName = "IndentStyle")]
        public string IndentStyle { get; set; }
        [XmlElement(ElementName = "BracketMatchingStyle")]
        public string BracketMatchingStyle { get; set; }
        [XmlElement(ElementName = "Font")]
        public string Font { get; set; }
        public System.Drawing.Font FontInstance { get { var f = Font.Split(';'); return new System.Drawing.Font(f[0], float.Parse(f[1])); } }
        [XmlElement(ElementName = "Scripts")]
        public string Scripts { get; set; }
        [XmlElement(ElementName = "Bilder")]
        public string Bilder { get; set; }
        [XmlElement(ElementName = "parser")]
        public bool Parser { get; set; }
        [XmlElement(ElementName = "backup")]
        public bool Backup { get; set; }
        [XmlElement(ElementName = "autocomplete")]
        public bool Autocomplete { get; set; }
        [XmlElement(ElementName = "backupfolder")]
        public string Backupfolder { get; set; }
        [XmlElement(ElementName = "backupeach")]
        public int Backupeach { get; set; }
        [XmlElement(ElementName = "backupfolderonly")]
        public bool Backupfolderonly { get; set; }
        [XmlElement(ElementName = "autobrackets")]
        public bool Autobrackets { get; set; }
    }

    [XmlRoot(ElementName = "PeterConfig")]
    public class PeterConfig
    {
        [XmlElement(ElementName = "Application")]
        public Application Application { get; set; }
        [XmlElement(ElementName = "RecentProjects")]
        public RecentProjects RecentProjects { get; set; }
        [XmlElement(ElementName = "RecentFiles")]
        public RecentFiles RecentFiles { get; set; }
        [XmlElement(ElementName = "Code")]
        public Code Code { get; set; }
        [XmlElement(ElementName = "Editor")]
        public Editor Editor { get; set; }
    }

}
