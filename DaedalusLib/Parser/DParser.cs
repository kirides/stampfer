using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace DaedalusLib.Parser
{
    public class DaedalusParserView
    {
        public static void ParseToTree(bool errormeld, string fileName, ref List<Diagnostics> lInstances, ref List<Diagnostics> lFuncs, ref List<Diagnostics> lVars, ref List<Diagnostics> lConsts)
        {
            var ErrorAusgabe = "";
            DaedalusParser parser;
            //using (var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
            //using (var scanner = new Scanner(fs))
            using (var scanner = new Scanner(fileName))
            {
                parser = new DaedalusParser(scanner);
                TextWriter errorString = new StringWriter();
                parser.errors.errorStream = errorString;
                parser.Parse();
                scanner.buffer = null;
                ErrorAusgabe = errorString.ToString();
            }

            if (ErrorAusgabe.Length > 0)
            {
                var err = ErrorAusgabe.Split('\n');
                ErrorAusgabe = err[0].Trim(); ;

                var r1 = new Regex("Zeile ");
                var r2 = new Regex("Spalte ");
                var m1 = r1.Match(ErrorAusgabe);
                var m2 = r2.Match(ErrorAusgabe);
                var line = "";
                var line2 = "";
                for (var i = m1.Index + m1.Length; i < ErrorAusgabe.Length; i++)
                {
                    if (ErrorAusgabe[i] == ',')
                    {
                        break;
                    }
                    line = line + ErrorAusgabe[i];
                }
                for (var i = m2.Index + m2.Length; i < ErrorAusgabe.Length; i++)
                {
                    if (ErrorAusgabe[i] == ':')
                    {
                        break;
                    }
                    line2 = line2 + ErrorAusgabe[i];
                }

                /* Fehler: bei line, line2 */
            }
            else
            {
                /* Keine Fehler */
            }


            // Using...
            // MessageBox.Show(parser.errors.count.ToString());

            //MessageBox.Show(parser.m_CodeInfo.ConstDeclarations.Count.ToString());
            foreach (var tm in parser.m_CodeInfo.Instances)
            {
                lInstances.Add(new Diagnostics(tm.Value, tm.Position));
            }
            foreach (var tm in parser.m_CodeInfo.Functions)
            {
                lFuncs.Add(new Diagnostics(tm.Value, tm.Position));
            }
            foreach (var tm in parser.m_CodeInfo.VarDeclarations)
            {
                lVars.Add(new Diagnostics(tm.Value, tm.Position));
            }
            foreach (var tm in parser.m_CodeInfo.ConstDeclarations)
            {
                lConsts.Add(new Diagnostics(tm.Value, tm.Position));
            }



        }
    }
}
