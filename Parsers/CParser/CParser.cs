using System.Windows.Forms;
using Peter.CSParser;

namespace Peter.CParser
{
    internal class CParser
    {
        public static void ParseToTree(string fileName, TreeNodeCollection nodes)
        {
            var scanner = new Peter.CParser.Scanner(fileName);
            var parser = new Peter.CParser.Parser(scanner);
            parser.Parse();

            // Include...
            var nInclude = new TreeNode("Includes");
            foreach (TokenMatch tm in parser.CodeInfo.Includes)
            {
                var n = new TreeNode(tm.Value)
                {
                    Tag = tm.Position
                };
                nInclude.Nodes.Add(n);
            }
            if (nInclude.Nodes.Count > 0)
            {
                nodes.Add(nInclude);
            }

            // Defines...
            var nDefine = new TreeNode("Defines");
            foreach (TokenMatch tm in parser.CodeInfo.Defines)
            {
                var n = new TreeNode(tm.Value)
                {
                    Tag = tm.Position
                };
                nDefine.Nodes.Add(n);
            }
            if (nDefine.Nodes.Count > 0)
            {
                nodes.Add(nDefine);
            }

            // GlobalVars...
            var nGlobalVars = new TreeNode("Global Variables");
            foreach (TokenMatch tm in parser.CodeInfo.GlobalVariables)
            {
                var n = new TreeNode(tm.Value)
                {
                    Tag = tm.Position
                };
                nGlobalVars.Nodes.Add(n);
            }
            if (nGlobalVars.Nodes.Count > 0)
            {
                nodes.Add(nGlobalVars);
                nGlobalVars.Expand();
            }

            // Structs...
            var nStructs = new TreeNode("Structs");
            foreach (TokenMatch tm in parser.CodeInfo.Structs)
            {
                var n = new TreeNode(tm.Value)
                {
                    Tag = tm.Position
                };
                nStructs.Nodes.Add(n);
            }
            if (nStructs.Nodes.Count > 0)
            {
                nodes.Add(nStructs);
                nStructs.Expand();
            }

            // TypeDefs...
            var nTypeDefs = new TreeNode("TypeDefs");
            foreach (TokenMatch tm in parser.CodeInfo.TypeDefs)
            {
                var n = new TreeNode(tm.Value)
                {
                    Tag = tm.Position
                };
                nTypeDefs.Nodes.Add(n);
            }
            if (nTypeDefs.Nodes.Count > 0)
            {
                nodes.Add(nTypeDefs);
                nTypeDefs.Expand();
            }

            // Prototypes...
            var nPrototypes = new TreeNode("Prototypes");
            foreach (TokenMatch tm in parser.CodeInfo.Prototypes)
            {
                var n = new TreeNode(tm.Value)
                {
                    Tag = tm.Position
                };
                nPrototypes.Nodes.Add(n);
            }
            if (nPrototypes.Nodes.Count > 0)
            {
                nodes.Add(nPrototypes);
                nPrototypes.Expand();
            }

            // Functions...
            var nFunctions = new TreeNode("Functions");
            foreach (TokenMatch tm in parser.CodeInfo.Functions)
            {
                var n = new TreeNode(tm.Value)
                {
                    Tag = tm.Position
                };
                nFunctions.Nodes.Add(n);
            }
            if (nFunctions.Nodes.Count > 0)
            {
                nodes.Add(nFunctions);
                nFunctions.Expand();
            }
        }
    }
}
