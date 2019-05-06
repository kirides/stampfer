using System.Drawing;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace PeterInterface
{
    public interface IPeterPlugin
    {
        void Start();

        void Close();

        string Name { get; }

        bool AbleToLoadFiles { get; }

        bool LoadFile(string filePath);

        bool HasMenu { get; }

        bool HasTabMenu { get; }

        bool HasContextMenu { get; }

        string Author { get; }

        string Version { get; }

        Image PluginImage { get; }

        ToolStripMenuItem GetMenu();

        ToolStripMenuItem[] GetTabMenu();

        ToolStripMenuItem[] GetContextMenu();

        PeterPluginType Type { get; }

        IPeterPluginHost Host { get; set; }

        void ActiveContentChanged(IDockContent tab);

        bool CheckContentString(string contentString);

        IDockContent GetContent(string contentString);

        Control OptionPanel { get; }

        void ApplyOptions();
    }
}
