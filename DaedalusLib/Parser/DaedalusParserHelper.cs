using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace DaedalusLib.Parser
{
    public class ParserResult
    {
        public List<TokenMatch> Instances { get; set; }
        public List<TokenMatch> Functions { get; set; }
        public List<TokenMatch> Variables { get; set; }
        public List<TokenMatch> Constants { get; set; }
        public List<LineError> ErrorMessages { get; set; }
    }
    public class LineError
    {
        public string Message { get; set; }
        public int Line { get; set; }
        public int Column { get; set; }
    }

    public static class DaedalusParserHelper
    {
        public static ParserResult Parse(string fileName) {
            string errorAusgabe = "";
            DaedalusParser parser;
            using (var scanner = new Scanner(fileName))
            {
                parser = new DaedalusParser(scanner);
                TextWriter errorString = new StringWriter();
                parser.errors.errorStream = errorString;
                parser.Parse();
                scanner.buffer = null;
                errorAusgabe = errorString.ToString();
            }

            var lineErrs = new List<LineError>();
            if (errorAusgabe.Length > 0)
            {
                var err = errorAusgabe.Split('\n');
                for (var iErr = 0; iErr < err.Length; iErr++)
                {
                    var error = err[iErr].Trim();

                    var rxLine = new Regex("Zeile ");
                    var rxCol = new Regex("Spalte ");
                    var matchLine = rxLine.Match(error);
                    var matchCol = rxCol.Match(error);
                    var line = "";
                    var column = "";
                    for (var i = matchLine.Index + matchLine.Length; i < error.Length; i++)
                    {
                        if (error[i] == ',')
                        {
                            break;
                        }
                        line += error[i];
                    }
                    for (var i = matchCol.Index + matchCol.Length; i < error.Length; i++)
                    {
                        if (error[i] == ':')
                        {
                            break;
                        }
                        column += error[i];
                    }

                    int.TryParse(line, out var lineVal);
                    int.TryParse(column, out var colVal);
                    lineErrs.Add(new LineError { Line = lineVal, Column = colVal, Message = error });
                }
                /* Fehler: bei line, line2 */
            }

            return new ParserResult
            {
                Instances = parser.m_CodeInfo.Instances,
                Functions = parser.m_CodeInfo.Functions,
                Variables = parser.m_CodeInfo.VarDeclarations,
                Constants = parser.m_CodeInfo.ConstDeclarations,
                ErrorMessages = lineErrs
            };
        }
    }
}
