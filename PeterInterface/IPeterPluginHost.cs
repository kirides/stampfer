using System;
using System.Drawing;
using WeifenLuo.WinFormsUI.Docking;

namespace PeterInterface
{
	public interface IPeterPluginHost
	{
		string EditorType { get; }

		string ApplicationExeStartPath { get; }

		void NewDocument();

		void Trace(string text);

		void SaveAs(IPeterPluginTab tab);

		void CreateEditor(string path, string tabName);

		void CreateEditor(string path, string tabName, Icon image);

		void CreateEditor(string path, string tabName, Icon image, IDockContent addToContent);

		Icon GetFileIcon(string path, bool linkOverlay);

		void AddDockContent(IDockContent content);

		void AddDockContent(IDockContent content, DockState state);

		void AddDockContent(IDockContent content, Rectangle floatingRec);
	}
}
