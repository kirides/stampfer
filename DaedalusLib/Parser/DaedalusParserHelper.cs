using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
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
        public static ParserResult Load(string fileName)
        {
            using(var fs = File.OpenRead(fileName))
            {
                return Load(fs);
            }
        }

        public static ParserResult Parse(string code)
        {
            using (var ms = new MemoryStream(Encoding.Default.GetBytes(code)))
            {
                return Load(ms);
            }
        }

        public static ParserResult Load(Stream stream)
        {
            var errorAusgabe = "";
            DaedalusParser parser;
            using (var scanner = new Scanner(stream))
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
                    if (string.IsNullOrWhiteSpace(error)) continue;

                    var rxLine = new Regex(@"Zeile (\d+)");
                    var rxCol = new Regex(@"Spalte (\d+)");
                    var matchLine = rxLine.Match(error);
                    var matchCol = rxCol.Match(error);
                    var line = "";
                    var column = "";

                    if (matchLine.Success) line = matchLine.Groups[1].Value;
                    if (matchCol.Success) column = matchCol.Groups[1].Value;

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
