using System.Text.RegularExpressions;

namespace PeterInterface
{
    public interface IPeterPluginTab
    {
        void Save();

        void SaveAs(string filePath);

        void Cut();

        void Copy();

        void Paste();

        void Undo();

        void Redo();

        void Delete();

        void Print();

        void SelectAll();

        void Duplicate();

        bool CloseTab();

        IPeterPluginHost Host { get; set; }

        string FileName { get; }

        string Selection { get; }

        bool AbleToUndo { get; }

        bool AbleToRedo { get; }

        bool AbleToPaste { get; }

        bool AbleToCut { get; }

        bool AbleToCopy { get; }

        bool AbleToSelectAll { get; }

        bool AbleToSave { get; }

        bool AbleToDelete { get; }

        bool NeedsSaving { get; }

        string TabText { get; set; }

        void MarkAll(Regex reg);

        bool FindNext(Regex reg, bool searchUp);

        void ReplaceNext(Regex reg, string replaceWith, bool searchUp);

        void ReplaceAll(Regex reg, string replaceWith);

        void SelectWord(int line, int offset, int wordLeng);
    }
}
