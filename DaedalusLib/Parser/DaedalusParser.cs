
using System;

namespace DaedalusLib.Parser
{
    public enum Kinds : byte
    {
        EOF = 0,
        Identifier = 1,
        IntegerValue = 2,
        FloatValue = 3,
        StringValue = 4,
        Int = 5,
        String = 6,
        Float = 7,
        Void = 8,
        Var = 9,
        Const = 10,
        Instance = 11,
        If = 12,
        Else= 13,
        Prototype = 14,
        Return = 15,
        CItem = 16,
        CNpc = 17,
        CMission = 18,
        CFocus = 19,
        CInfo = 20,
        CItemReact = 21,
        CSpell = 22,
        Function = 23,
        Class = 24,
    }
    public class DaedalusParser
    {
        private const int _EOF = 0;
        private const int _identifier = 1;
        private const int _integervalue = 2;
        private const int _floatnumber = 3;
        private const int _stringvalue = 4;
        private const int _int = 5;
        private const int _string = 6;
        private const int _float = 7;
        private const int _void = 8;
        private const int KIND_VAR = 9;
        private const int KIND_CONST = 10;
        private const int KIND_INSTANCE = 11;
        private const int _if = 12;
        private const int _else = 13;
        private const int KIND_PROTOTYPE = 14;
        private const int _return = 15;
        private const int _citem = 16;
        private const int _cnpc = 17;
        private const int _cmission = 18;
        private const int _cfocus = 19;
        private const int _cinfo = 20;
        private const int _citemreact = 21;
        private const int _cspell = 22;
        private const int KIND_FUNCTION = 23;
        private const int KIND_CLASS = 24;
        private const int KIND_MAXKIND = 67;
        private const bool T = true;
        private const bool x = false;
        private const int minErrDist = 2;

        private string m_Current = "";
        private int m_CurrentPos = 0;
        public Scanner scanner;
        public Errors errors;
        public DaedalusCodeInfo m_CodeInfo = new DaedalusCodeInfo();

        public Token t;    // last recognized token
        public Token la;   // lookahead token
        private int errDist = minErrDist;

        public DaedalusParser(Scanner scanner)
        {
            this.scanner = scanner;
            errors = new Errors();
        }

        private void SynErr(int n)
        {
            if (errDist >= minErrDist) errors.SynErr(la.line, la.col, n);
            errDist = 0;
        }

        public void SemErr(string msg)
        {
            if (errDist >= minErrDist) errors.SemErr(t.line, t.col, msg);
            errDist = 0;
        }

        private void Get()
        {
            for (;;)
            {
                t = la;
                la = scanner.Scan();
                if (la.kind <= KIND_MAXKIND) { ++errDist; break; }

                la = t;
            }
        }

        private void Expect(int n)
        {
            if (la.kind == n) Get(); else { SynErr(n); }
        }

        private bool StartOf(int s)
        {
            return set[s, la.kind];
        }

        private void D()
        {
            while (StartOf(1))
            {
                Block();
            }
        }

        private void Block()
        {
            if (la.kind == KIND_VAR || la.kind == KIND_CONST)
            {
                Declaration();
            }
            else if (la.kind == KIND_FUNCTION)
            {
                Function();
            }
            else if (la.kind == KIND_INSTANCE)
            {
                Instance();
            }
            else if (la.kind == KIND_PROTOTYPE)
            {
                Prototype();
            }
            else if (la.kind == KIND_CLASS)
            {
                Class();
            }
            else SynErr(68);
        }

        private void Declaration()
        {
            if (la.kind == KIND_CONST)
            {
                ConstDecl();
            }
            else if (la.kind == KIND_VAR)
            {
                VarDecl();
            }
            else SynErr(69);
        }

        private void Function()
        {
            Expect(23);
            if (StartOf(2))
            {
                this.m_Current = la.val.ToLower() + " ";
                Type();
            }
            else if (la.kind == 8)
            {
                this.m_Current = la.val.ToLower() + " ";
                Get();
            }
            else SynErr(70);
            this.m_CurrentPos = la.pos;
            this.m_Current += la.val + " ";
            Expect(1);
            this.m_Current += la.val;
            Expect(32);
            //this.m_Current += la.val;
            if (la.kind == KIND_VAR)
            {
                Parameter();

                while (la.kind == 27)
                {
                    Get();
                    Parameter();

                }
            }
            var tm = new TokenMatch
            {
                Position = this.m_CurrentPos
            };
            //this.m_Current += la.val;
            m_Current = m_Current.Trim();
            if (!m_Current.EndsWith(")"))
                m_Current += ")";

            tm.Value = this.m_Current;
            this.m_CodeInfo.Functions.Add(tm);
            Expect(33);
            Expect(26);
            while (StartOf(3))
            {
                Body();
            }
            Expect(28);
            Expect(29);
        }

        private void Instance()
        {
            Expect(11);
            var tm = new TokenMatch
            {
                Position = la.pos
            };
            this.m_Current = la.val;
            tm.Value = this.m_Current;
            this.m_CodeInfo.Instances.Add(tm);
            Expect(1);
            while (la.kind == 27)
            {
                Get();
                Expect(1);
            }
            Expect(32);
            if (la.kind == 1)
            {
                Get();
            }
            else if (StartOf(4))
            {
                Classname();
            }
            else SynErr(71);
            Expect(33);
            if (la.kind == 26)
            {
                Get();
                while (StartOf(3))
                {
                    Body();
                }
                Expect(28);
            }
            Expect(29);
        }

        private void Prototype()
        {
            Expect(14);
            Expect(1);
            Expect(32);
            if (la.kind == 1)
            {
                Get();
            }
            else if (StartOf(4))
            {
                Classname();
            }
            else SynErr(72);
            Expect(33);
            Expect(26);
            while (StartOf(3))
            {
                Body();
            }
            Expect(28);
            Expect(29);
        }

        private void Class()
        {
            Expect(24);
            if (la.kind == 1)
            {
                Get();
            }
            else if (StartOf(4))
            {
                Classname();
            }
            else SynErr(73);
            Expect(26);
            while (StartOf(3))
            {
                Body();
            }
            Expect(28);
            Expect(29);
        }

        private void ConstDecl()
        {
            Expect(KIND_CONST);
            this.m_Current = "@" + la.val.ToLower() + " ";
            m_CurrentPos = la.pos;
            TypeDecl();

            Expect(25);
            this.m_Current += (" = " + la.val);

            if (StartOf(5))
            {
                Expression();

            }
            else if (la.kind == 26)
            {
                Get();
                Expression();

                while (la.kind == 27)
                {
                    Get();
                    Expression();
                }
                Expect(28);


            }
            else SynErr(74);


            Expect(29);

            this.m_Current = this.m_Current.Remove(0, 1);
            var tm = new TokenMatch
            {
                Position = m_CurrentPos,
                //this.m_Current += la.val;

                Value = this.m_Current
            };
            this.m_CodeInfo.ConstDeclarations.Add(tm);
        }

        private void VarDecl()
        {
            Expect(KIND_VAR);
            this.m_Current = la.val.ToLower() + " ";
            TypeDecl();
            if (la.kind == 25)
            {
                Get();
                if (StartOf(5))
                {
                    Expression();
                }
                else if (la.kind == 26)
                {
                    Get();
                    Expression();
                    while (la.kind == 27)
                    {
                        Get();
                        Expression();
                    }
                    Expect(28);
                }
                else SynErr(75);
            }
            Expect(29);

            var tm = new TokenMatch
            {
                Position = la.pos,
                Value = this.m_Current
            };
            this.m_CodeInfo.VarDeclarations.Add(tm);
        }

        private void TypeDecl()
        {
            Type();
            this.m_Current += la.val;
            Expect(1);
            while (la.kind == 27)
            {
                Get();
                Expect(1);
            }
            if (la.kind == 30)
            {
                Get();
                if (la.kind == 2)
                {
                    Get();
                }
                else if (la.kind == 1)
                {
                    Get();
                }
                else SynErr(76);
                Expect(31);
            }
        }

        private void Expression()
        {
            AndExpr();
            while (la.kind == 44)
            {
                Get();
                AndExpr();
            }
        }

        private void Type()
        {
            switch (la.kind)
            {
                case 5:
                case 6:
                case 7:
                case 17:
                case 16:
                case 18:
                case 20:
                case 22:
                case 19:
                case 21:
                case 23:
                    {
                        Get();
                        break;
                    }
                default: SynErr(77); break;
            }
        }

        private void Classname()
        {
            switch (la.kind)
            {
                case 17:
                case 16:
                case 18:
                case 20:
                case 22:
                case 19:
                case 21:
                    {
                        Get();
                        break;
                    }
                default: SynErr(78); break;
            }
        }

        private void Body()
        {
            if (la.kind == 9 || la.kind == 10)
            {
                Declaration();
            }
            else if (StartOf(6))
            {
                Statement();
            }
            else SynErr(79);
        }

        private void Parameter()
        {
            this.m_Current += la.val;
            Expect(9);
            this.m_Current += " " + la.val;
            Type();
            this.m_Current += " " + la.val;
            Expect(1);
            this.m_Current += la.val + " ";
        }

        private void Statement()
        {
            if (la.kind == 12)
            {
                IfStatement();
                while (la.kind == 12)
                {
                    IfStatement();
                }
                Expect(29);
            }
            else if (StartOf(5))
            {
                Expression();
                if (StartOf(7))
                {
                    Assign();
                    Expression();
                }
                Expect(29);
            }
            else if (la.kind == 15)
            {
                Get();
                if (StartOf(5))
                {
                    Expression();
                }
                Expect(29);
            }
            else SynErr(80);
        }

        private void IfStatement()
        {
            Expect(12);
            Expression();
            Expect(26);
            while (StartOf(3))
            {
                Body();
            }
            Expect(28);
            while (la.kind == 13)
            {
                Get();
                if (la.kind == 12)
                {
                    Get();
                    Expression();
                    Expect(26);
                    while (StartOf(3))
                    {
                        Body();
                    }
                    Expect(28);
                }
                else if (la.kind == 26)
                {
                    Get();
                    while (StartOf(3))
                    {
                        Body();
                    }
                    Expect(28);
                }
                else SynErr(81);
            }
        }

        private void Assign()
        {
            switch (la.kind)
            {
                case 25:
                case 34:
                case 35:
                case 36:
                case 37:
                case 38:
                case 39:
                case 40:
                case 41:
                case 42:
                case 43:
                    {
                        Get();
                        break;
                    }
                default: SynErr(82); break;
            }
        }

        private void AndExpr()
        {
            BitOrExpr();
            while (la.kind == 45)
            {
                Get();
                BitOrExpr();
            }
        }

        private void BitOrExpr()
        {
            BitXorExpr();
            while (la.kind == 46)
            {
                Get();
                BitXorExpr();
            }
        }

        private void BitXorExpr()
        {
            BitAndExpr();
            while (la.kind == 47)
            {
                Get();
                BitAndExpr();
            }
        }

        private void BitAndExpr()
        {
            CondExpr();
            while (la.kind == 48)
            {
                Get();
                CondExpr();
            }
        }

        private void CondExpr()
        {
            ShiftExpr();
            while (StartOf(8))
            {
                switch (la.kind)
                {
                    case 49:
                    case 50:
                    case 51:
                    case 52:
                    case 53:
                    case 54:
                        {
                            Get();
                            break;
                        }
                }
                ShiftExpr();
            }
        }

        private void ShiftExpr()
        {
            AddExpr();
            while (la.kind == 55 || la.kind == 56)
            {
                Get();
                AddExpr();
            }
        }

        private void AddExpr()
        {
            MulExpr();
            while (la.kind == 57 || la.kind == 58)
            {
                Get();
                MulExpr();
            }
        }

        private void MulExpr()
        {
            CastExpr();
            while (la.kind == 59 || la.kind == 60 || la.kind == 61)
            {
                Get();
                CastExpr();
            }
        }

        private void CastExpr()
        {
            UnaryExp();
        }

        private void UnaryExp()
        {
            if (StartOf(9))
            {
                PostFixExp();
            }
            else if (la.kind == 62 || la.kind == 63)
            {
                Get();
                UnaryExp();
            }
            else if (StartOf(10))
            {
                Get();
                CastExpr();
            }
            else SynErr(83);
        }

        private void PostFixExp()
        {
            Primary();
            while (StartOf(11))
            {
                if (la.kind == 30)
                {
                    Get();
                    Expression();
                    Expect(31);
                }
                else if (la.kind == 32)
                {
                    Get();
                    if (StartOf(5))
                    {
                        ActualParameters();
                    }
                    Expect(33);
                }
                else if (la.kind == 66)
                {
                    Get();
                    Expect(1);
                }
                else
                {
                    Get();
                }
            }
        }

        private void Primary()
        {
            switch (la.kind)
            {
                case 1:
                case 2:
                case 3:
                case 4:
                    Get();
                    break;
                case 32:
                    Get();
                    Expression();
                    Expect(33);
                    break;
                default:
                    SynErr(84);
                    break;
            }
        }

        private void ActualParameters()
        {
            Expression();
            while (la.kind == 27)
            {
                Get();
                Expression();
            }
        }

        public void Parse()
        {
            la = new Token
            {
                val = ""
            };
            Get();
            D();

            Expect(0);
        }

        private static readonly bool[,] set = {
        {T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x},
        {x,x,x,x, x,x,x,x, x,T,T,T, x,x,T,x, x,x,x,x, x,x,x,T, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x},
        {x,x,x,x, x,T,T,T, x,x,x,x, x,x,x,x, T,T,T,T, T,T,T,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x},
        {x,T,T,T, T,x,x,x, x,T,T,x, T,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,T,x, x,x,T,T, T,T,x,x, x},
        {x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,T,T,T, T,T,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x},
        {x,T,T,T, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,T,x, x,x,T,T, T,T,x,x, x},
        {x,T,T,T, T,x,x,x, x,x,x,x, T,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,T,x, x,x,T,T, T,T,x,x, x},
        {x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x,x,x,x, x,x,T,T, T,T,T,T, T,T,T,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x},
        {x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,T,T, T,T,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x},
        {x,T,T,T, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x},
        {x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,T,x, x,x,x,x, T,T,x,x, x},
        {x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,T, x,x,T,x, x}

    };
    } // end Parser


    public class Errors
    {
        public int count = 0;                                    // number of errors detected
        public System.IO.TextWriter errorStream = Console.Out;   // error messages go to this stream
        public string errMsgFormat = "Zeile {0}, Spalte {1}: {2}"; // 0=line, 1=column, 2=text

        public void SynErr(int line, int col, int n)
        {
            string s;
            switch (n)
            {
                case 0: s = "Dateiende erwartet"; break;
                case 1: s = "Bezeichner erwartet"; break;
                case 2: s = "Integer-Wert erwartet"; break;
                case 3: s = "Float-Wert erwartet"; break;
                case 4: s = "String-Wert erwartet"; break;
                case 5: s = "int erwartet"; break;
                case 6: s = "string erwartet"; break;
                case 7: s = "float erwartet"; break;
                case 8: s = "void erwartet"; break;
                case 9: s = "var erwartet"; break;
                case 10: s = "const erwartet"; break;
                case 11: s = "instance erwartet"; break;
                case 12: s = "if erwartet"; break;
                case 13: s = "else erwartet"; break;
                case 14: s = "prototype erwartet"; break;
                case 15: s = "return erwartet"; break;
                case 16: s = "c_item erwartet"; break;
                case 17: s = "c_npc erwartet"; break;
                case 18: s = "c_mission erwartet"; break;
                case 19: s = "c_focus erwartet"; break;
                case 20: s = "c_info erwartet"; break;
                case 21: s = "c_itemreact erwartet"; break;
                case 22: s = "c_spell erwartet"; break;
                case 23: s = "func erwartet"; break;
                case 24: s = "class erwartet"; break;
                case 25: s = "\"=\" erwartet"; break;
                case 26: s = "\"{\" erwartet"; break;
                case 27: s = "\",\" erwartet"; break;
                case 28: s = "\"}\" erwartet"; break;
                case 29: s = "\";\" erwartet"; break;
                case 30: s = "\"[\" erwartet"; break;
                case 31: s = "\"]\" erwartet"; break;
                case 32: s = "\"(\" erwartet"; break;
                case 33: s = "\")\" erwartet"; break;
                case 34: s = "\"*=\" erwartet"; break;
                case 35: s = "\"/=\" erwartet"; break;
                case 36: s = "\"%=\" erwartet"; break;
                case 37: s = "\"+=\" erwartet"; break;
                case 38: s = "\"-=\" erwartet"; break;
                case 39: s = "\"&=\" erwartet"; break;
                case 40: s = "\"^=\" erwartet"; break;
                case 41: s = "\"|=\" erwartet"; break;
                case 42: s = "\"<<=\" erwartet"; break;
                case 43: s = "\">>=\" erwartet"; break;
                case 44: s = "\"||\" erwartet"; break;
                case 45: s = "\"&&\" erwartet"; break;
                case 46: s = "\"|\" erwartet"; break;
                case 47: s = "\"^\" erwartet"; break;
                case 48: s = "\"&\" erwartet"; break;
                case 49: s = "\"!=\" erwartet"; break;
                case 50: s = "\"==\" erwartet"; break;
                case 51: s = "\"<=\" erwartet"; break;
                case 52: s = "\"<\" erwartet"; break;
                case 53: s = "\">=\" erwartet"; break;
                case 54: s = "\">\" erwartet"; break;
                case 55: s = "\"<<\" erwartet"; break;
                case 56: s = "\">>\" erwartet"; break;
                case 57: s = "\"+\" erwartet"; break;
                case 58: s = "\"-\" erwartet"; break;
                case 59: s = "\"*\" erwartet"; break;
                case 60: s = "\"/\" erwartet"; break;
                case 61: s = "\"%\" erwartet"; break;
                case 62: s = "\"++\" erwartet"; break;
                case 63: s = "\"--\" erwartet"; break;
                case 64: s = "\"~\" erwartet"; break;
                case 65: s = "\"!\" erwartet"; break;
                case 66: s = "\".\" erwartet"; break;
                case 67: s = "??? erwartet"; break;
                case 68: s = "Ungültiger Block"; break;
                case 69: s = "Ungültige Deklaration"; break;
                case 70: s = "Ungültige Funktion"; break;
                case 71: s = "Ungültige Instance"; break;
                case 72: s = "Ungültiger Prototyp"; break;
                case 73: s = "Ungültige Klasse"; break;
                case 74: s = "Ungültige Konstantendeklaration"; break;
                case 75: s = "Ungültige Variablendeklaration"; break;
                case 76: s = "Ungültige Typendeklaration"; break;
                case 77: s = "Ungültiger Typ"; break;
                case 78: s = "Ungültige Klasse"; break;
                case 79: s = "Ungültiger Block"; break;
                case 80: s = "Ungültiger Ausdruck"; break;
                case 81: s = "Ungültige Abfrage"; break;
                case 82: s = "Ungültige Zuweisung"; break;
                case 83: s = "Ungültiger Operator"; break;
                case 84: s = "Ungültiger Ausdruck"; break;
                default: s = "error " + n; break;
            }
            errorStream.WriteLine(errMsgFormat, line, col, s);
            count++;
        }

        public void SemErr(int line, int col, string s)
        {
            errorStream.WriteLine(errMsgFormat, line, col, s);
            count++;
        }

        public void SemErr(string s)
        {
            errorStream.WriteLine(s);
            count++;
        }

        public void Warning(int line, int col, string s)
        {
            errorStream.WriteLine(errMsgFormat, line, col, s);
        }

        public void Warning(string s)
        {
            errorStream.WriteLine(s);
        }
    } // Errors


    public class FatalError : Exception
    {
        public FatalError(string m) : base(m) { }
    }

}