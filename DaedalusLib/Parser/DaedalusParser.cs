
using System;
using System.Collections.Generic;

namespace DaedalusLib.Parser
{
    public enum Kinds : int
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
        Else = 13,
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

        Assign = 25,
        BraceOpen = 26,
        Comma = 27,
        BraceClose = 28,
        Semicolon = 29,
        BracketOpen = 30,
        BracketClose = 31,
        ParenOpen = 32,
        ParenClose = 33,
        AssignMul = 34,
        AssignDiv = 35,
        AssignMod = 36,
        AssignAdd = 37,
        AssignSub = 38,
        AssignAnd = 39,
        AssignXOr = 40,
        AssignOr = 41,
        AssignShiftLeft = 42,
        AssignShiftRight = 43,
        Or = 44,
        And = 45,
        BitOr = 46,
        BitXOr = 47,
        BitAnd = 48,
        NotEquals = 49,
        Equals = 50,
        LesserEquals = 51,
        Lesser = 52,
        GreaterEquals = 53,
        Greater = 54,
        ShiftLeft = 55,
        ShiftRight = 56,
        Add = 57,
        Sub = 58,
        Mul = 59,
        Div = 60,
        Mod = 61,
        Incr = 62,
        Decr = 63,
        BitNot = 64,
        Not = 65,
        Dot = 66,

        Unknown = 67,

        ErrInvalidBlock = 68,
        ErrInvalidDeclaration = 69,
        ErrInvalidFunction = 70,
        ErrInvalidInstance = 71,
        ErrInvalidPrototype = 72,
        ErrInvalidClass = 73,
        ErrInvalidConstantDeclaration = 74,
        ErrInvalidVariableDeclaration = 75,
        ErrInvalidTypeDeclaration = 76,
        ErrInvalidType = 77,
        ErrInvalidClassName = 78,
        ErrInvalidBlocKBody = 79,
        ErrInvalidStatement = 80,
        ErrInvalidIfStatement = 81,
        ErrInvalidAssignment = 82,
        ErrInvalidOperator = 83,
        ErrInvalidExpression = 84,
    }
    public class DaedalusParser
    {
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

        private void SynErr(Kinds kind)
        {
            if (errDist >= minErrDist) errors.SynErr(la.line, la.col, kind);
            errDist = 0;
        }

        public void SemErr(string msg)
        {
            if (errDist >= minErrDist) errors.SemErr(t.line, t.col, msg);
            errDist = 0;
        }

        private void Get()
        {
            for (; ; )
            {
                t = la;
                la = scanner.Scan();
                if (la.kind <= KIND_MAXKIND) { ++errDist; break; }

                la = t;
            }
        }

        private void Expect(Kinds kind)
        {
            if (la.Kind == kind) Get(); else { SynErr(kind); }
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
            var kind = la.Kind;
            if (kind == Kinds.Var || kind == Kinds.Const)
            {
                Declaration();
            }
            else if (kind == Kinds.Function)
            {
                Function();
            }
            else if (kind == Kinds.Instance)
            {
                Instance();
            }
            else if (kind == Kinds.Prototype)
            {
                Prototype();
            }
            else if (kind == Kinds.Class)
            {
                Class();
            }
            else SynErr(Kinds.ErrInvalidBlock);
        }

        private void Declaration()
        {
            if (la.Kind == Kinds.Const)
            {
                ConstDecl();
            }
            else if (la.Kind == Kinds.Var)
            {
                VarDecl();
            }
            else SynErr(Kinds.ErrInvalidDeclaration);
        }

        private void Function()
        {
            Expect(Kinds.Function);
            if (StartOf(2))
            {
                this.m_Current = la.val.ToLower() + " ";
                Type();
            }
            else if (la.Kind == Kinds.Void)
            {
                this.m_Current = la.val.ToLower() + " ";
                Get();
            }
            else SynErr(Kinds.ErrInvalidFunction);
            this.m_CurrentPos = la.pos;
            this.m_Current += la.val + " ";
            Expect(Kinds.Identifier);
            this.m_Current += la.val;
            Expect(Kinds.ParenOpen);
            //this.m_Current += la.val;
            if (la.Kind == Kinds.Var)
            {
                Parameter();

                while (la.Kind == Kinds.Comma)
                {
                    Get();
                    Parameter();
                }
            }
            var tm = new TokenMatch
            {
                Position = this.m_CurrentPos
            };

            m_Current = m_Current.Trim();
            if (!m_Current.EndsWith(")"))
                m_Current += ")";

            tm.Value = this.m_Current;
            this.m_CodeInfo.Functions.Add(tm);
            Expect(Kinds.ParenClose);
            Expect(Kinds.BraceOpen);
            while (StartOf(3))
            {
                Body();
            }
            Expect(Kinds.BraceClose);
            Expect(Kinds.Semicolon);
        }

        private void Instance()
        {
            Expect(Kinds.Instance);
            var tm = new TokenMatch
            {
                Position = la.pos
            };
            this.m_Current = la.val;
            tm.Value = this.m_Current;
            this.m_CodeInfo.Instances.Add(tm);
            Expect(Kinds.Identifier);
            while (la.Kind == Kinds.Comma)
            {
                Get();
                Expect(Kinds.Identifier);
            }
            Expect(Kinds.ParenOpen);
            if (la.Kind == Kinds.Identifier)
            {
                Get();
            }
            else if (StartOf(4))
            {
                Classname();
            }
            else SynErr(Kinds.ErrInvalidInstance);
            Expect(Kinds.ParenClose);
            if (la.Kind == Kinds.BraceOpen)
            {
                Get();
                while (StartOf(3))
                {
                    Body();
                }
                Expect(Kinds.BraceClose);
            }
            Expect(Kinds.Semicolon);
        }

        private void Prototype()
        {
            Expect(Kinds.Prototype);
            Expect(Kinds.Identifier);
            Expect(Kinds.ParenOpen);
            if (la.Kind == Kinds.Identifier)
            {
                Get();
            }
            else if (StartOf(4))
            {
                Classname();
            }
            else SynErr(Kinds.ErrInvalidPrototype);
            Expect(Kinds.ParenClose);
            Expect(Kinds.BraceOpen);
            while (StartOf(3))
            {
                Body();
            }
            Expect(Kinds.BraceClose);
            Expect(Kinds.Semicolon);
        }

        private void Class()
        {
            Expect(Kinds.Class);
            if (la.Kind == Kinds.Identifier)
            {
                Get();
            }
            else if (StartOf(4))
            {
                Classname();
            }
            else SynErr(Kinds.ErrInvalidClass);
            Expect(Kinds.BraceOpen);
            while (StartOf(3))
            {
                Body();
            }
            Expect(Kinds.BraceClose);
            Expect(Kinds.Semicolon);
        }

        private void ConstDecl()
        {
            Expect(Kinds.Const);
            this.m_Current = "@" + la.val.ToLower() + " ";
            m_CurrentPos = la.pos;
            TypeDecl();

            Expect(Kinds.Assign);
            this.m_Current += (" = " + la.val);

            if (StartOf(5))
            {
                Expression();

            }
            else if (la.Kind == Kinds.BraceOpen)
            {
                Get();
                Expression();

                while (la.Kind == Kinds.Comma)
                {
                    Get();
                    Expression();
                }
                Expect(Kinds.BraceClose);
            }
            else SynErr(Kinds.ErrInvalidConstantDeclaration);

            Expect(Kinds.Semicolon);

            this.m_Current = this.m_Current.Remove(0, 1);
            var tm = new TokenMatch
            {
                Position = m_CurrentPos,
                Value = this.m_Current
            };
            this.m_CodeInfo.ConstDeclarations.Add(tm);
        }

        private void VarDecl()
        {
            Expect(Kinds.Var);
            this.m_Current = la.val.ToLower() + " ";
            TypeDecl();
            if (la.Kind == Kinds.Assign)
            {
                Get();
                if (StartOf(5))
                {
                    Expression();
                }
                else if (la.Kind == Kinds.BraceOpen)
                {
                    Get();
                    Expression();
                    while (la.Kind == Kinds.Comma)
                    {
                        Get();
                        Expression();
                    }
                    Expect(Kinds.BraceClose);
                }
                else SynErr(Kinds.ErrInvalidVariableDeclaration);
            }
            Expect(Kinds.Semicolon);

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
            Expect(Kinds.Identifier);
            while (la.Kind == Kinds.Comma)
            {
                Get();
                Expect(Kinds.Identifier);
            }
            if (la.Kind == Kinds.BracketOpen)
            {
                Get();
                if (la.Kind == Kinds.IntegerValue || la.Kind == Kinds.Identifier)
                {
                    Get();
                }
                else SynErr(Kinds.ErrInvalidTypeDeclaration);
                Expect(Kinds.BracketClose);
            }
        }

        private void Expression()
        {
            AndExpr();
            while (la.Kind == Kinds.Or)
            {
                Get();
                AndExpr();
            }
        }

        private void Type()
        {
            switch (la.Kind)
            {
                case Kinds.Int:
                case Kinds.String:
                case Kinds.Float:
                case Kinds.CNpc:
                case Kinds.CItem:
                case Kinds.CMission:
                case Kinds.CInfo:
                case Kinds.CSpell:
                case Kinds.CFocus:
                case Kinds.CItemReact:
                case Kinds.Function:
                    {
                        Get();
                        break;
                    }
                default: SynErr(Kinds.ErrInvalidType); break;
            }
        }

        private void Classname()
        {
            switch (la.Kind)
            {
                case Kinds.CNpc:
                case Kinds.CItem:
                case Kinds.CMission:
                case Kinds.CInfo:
                case Kinds.CSpell:
                case Kinds.CFocus:
                case Kinds.CItemReact:
                    {
                        Get();
                        break;
                    }
                default: SynErr(Kinds.ErrInvalidClassName); break;
            }
        }

        private void Body()
        {
            if (la.Kind == Kinds.Var || la.Kind == Kinds.Const)
            {
                Declaration();
            }
            else if (StartOf(6))
            {
                Statement();
            }
            else SynErr(Kinds.ErrInvalidBlocKBody);
        }

        private void Parameter()
        {
            this.m_Current += la.val;
            Expect(Kinds.Var);
            this.m_Current += " " + la.val;
            Type();
            this.m_Current += " " + la.val;
            Expect(Kinds.Identifier);
            this.m_Current += la.val + " ";
        }

        private void Statement()
        {
            if (la.Kind == Kinds.If)
            {
                IfStatement();
                while (la.Kind == Kinds.If)
                {
                    IfStatement();
                }
                Expect(Kinds.Semicolon);
            }
            else if (StartOf(5))
            {
                Expression();
                if (StartOf(7))
                {
                    Assign();
                    Expression();
                }
                Expect(Kinds.Semicolon);
            }
            else if (la.Kind == Kinds.Return)
            {
                Get();
                if (StartOf(5))
                {
                    Expression();
                }
                Expect(Kinds.Semicolon);
            }
            else SynErr(Kinds.ErrInvalidStatement);
        }

        private void IfStatement()
        {
            Expect(Kinds.If);
            Expression();
            Expect(Kinds.BraceOpen);
            while (StartOf(3))
            {
                Body();
            }
            Expect(Kinds.BraceClose);
            while (la.Kind == Kinds.Else)
            {
                Get();
                if (la.Kind == Kinds.If)
                {
                    Get();
                    Expression();
                    Expect(Kinds.BraceOpen);
                    while (StartOf(3))
                    {
                        Body();
                    }
                    Expect(Kinds.BraceClose);
                }
                else if (la.Kind == Kinds.BraceOpen)
                {
                    Get();
                    while (StartOf(3))
                    {
                        Body();
                    }
                    Expect(Kinds.BraceClose);
                }
                else SynErr(Kinds.ErrInvalidIfStatement);
            }
        }

        private void Assign()
        {
            switch (la.Kind)
            {
                case Kinds.Assign:
                case Kinds.AssignMul:
                case Kinds.AssignDiv:
                case Kinds.AssignMod:
                case Kinds.AssignAdd:
                case Kinds.AssignSub:
                case Kinds.AssignAnd:
                case Kinds.AssignXOr:
                case Kinds.AssignOr:
                case Kinds.AssignShiftLeft:
                case Kinds.AssignShiftRight:
                    {
                        Get();
                        break;
                    }
                default: SynErr(Kinds.ErrInvalidAssignment); break;
            }
        }

        private void AndExpr()
        {
            BitOrExpr();
            while (la.Kind == Kinds.And)
            {
                Get();
                BitOrExpr();
            }
        }

        private void BitOrExpr()
        {
            BitXorExpr();
            while (la.Kind == Kinds.BitOr)
            {
                Get();
                BitXorExpr();
            }
        }

        private void BitXorExpr()
        {
            BitAndExpr();
            while (la.Kind == Kinds.BitXOr)
            {
                Get();
                BitAndExpr();
            }
        }

        private void BitAndExpr()
        {
            CondExpr();
            while (la.Kind == Kinds.BitAnd)
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
                switch (la.Kind)
                {
                    case Kinds.NotEquals:
                    case Kinds.Equals:
                    case Kinds.LesserEquals:
                    case Kinds.Lesser:
                    case Kinds.GreaterEquals:
                    case Kinds.Greater:
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
            while (la.Kind == Kinds.ShiftLeft || la.Kind == Kinds.ShiftRight)
            {
                Get();
                AddExpr();
            }
        }

        private void AddExpr()
        {
            MulExpr();
            while (la.Kind == Kinds.Add || la.Kind == Kinds.Sub)
            {
                Get();
                MulExpr();
            }
        }

        private void MulExpr()
        {
            CastExpr();
            while (la.Kind == Kinds.Mul || la.Kind == Kinds.Div || la.Kind == Kinds.Mod)
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
            else if (la.Kind == Kinds.Incr || la.Kind == Kinds.Decr)
            {
                Get();
                UnaryExp();
            }
            else if (StartOf(10))
            {
                Get();
                CastExpr();
            }
            else SynErr(Kinds.ErrInvalidOperator);
        }

        private void PostFixExp()
        {
            Primary();
            while (StartOf(11))
            {
                if (la.Kind == Kinds.BracketOpen)
                {
                    Get();
                    Expression();
                    Expect(Kinds.BracketClose);
                }
                else if (la.Kind == Kinds.ParenOpen)
                {
                    Get();
                    if (StartOf(5))
                    {
                        ActualParameters();
                    }
                    Expect(Kinds.ParenClose);
                }
                else if (la.Kind == Kinds.Dot)
                {
                    Get();
                    Expect(Kinds.Identifier);
                }
                else
                {
                    Get();
                }
            }
        }

        private void Primary()
        {
            switch (la.Kind)
            {
                case Kinds.Identifier:
                case Kinds.IntegerValue:
                case Kinds.FloatValue:
                case Kinds.StringValue:
                    Get();
                    break;
                case Kinds.ParenOpen:
                    Get();
                    Expression();
                    Expect(Kinds.ParenClose);
                    break;
                default:
                    SynErr(Kinds.ErrInvalidExpression);
                    break;
            }
        }

        private void ActualParameters()
        {
            Expression();
            while (la.Kind == Kinds.Comma)
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
        private static readonly Dictionary<Kinds, string> messages = new Dictionary<Kinds, string>
        {
            { Kinds.EOF, "Dateiende erwartet" },
            { Kinds.Identifier, "Bezeichner erwartet" },
            { Kinds.IntegerValue, "Integer-Wert erwartet" },
            { Kinds.FloatValue, "Dateiende erwartet" },
            { Kinds.StringValue, "Dateiende erwartet" },
            { Kinds.Int, "int erwartet" },
            { Kinds.String, "string erwartet" },
            { Kinds.Float, "float erwartet" },
            { Kinds.Void, "void erwartet" },
            { Kinds.Var, "var erwartet" },
            { Kinds.Const, "const erwartet" },
            { Kinds.Instance, "instance erwartet" },
            { Kinds.If, "if erwartet" },
            { Kinds.Else, "else erwartet" },
            { Kinds.Prototype, "prototype erwartet" },
            { Kinds.Return, "return erwartet" },
            { Kinds.CItem, "c_item erwartet" },
            { Kinds.CNpc, "c_npc erwartet" },
            { Kinds.CMission, "c_mission erwartet" },
            { Kinds.CFocus, "c_focus erwartet" },
            { Kinds.CInfo, "c_info erwartet" },
            { Kinds.CItemReact, "c_itemreact erwartet" },
            { Kinds.CSpell, "c_spell erwartet" },
            { Kinds.Function, "func erwartet" },
            { Kinds.Class, "class erwartet" },
            { Kinds.Assign, "\"=\" erwartet" },
            { Kinds.BraceOpen, "\"{\" erwartet" },
            { Kinds.Comma, "\",\" erwartet" },
            { Kinds.BraceClose, "\"}\" erwartet" },
            { Kinds.Semicolon, "\";\" erwartet" },
            { Kinds.BracketOpen, "\"[\" erwartet" },
            { Kinds.BracketClose, "\"]\" erwartet" },
            { Kinds.ParenOpen, "\"(\" erwartet" },
            { Kinds.ParenClose, "\")\" erwartet" },
            { Kinds.AssignMul, "\"*=\" erwartet" },
            { Kinds.AssignDiv, "\"/=\" erwartet" },
            { Kinds.AssignMod, "\"%=\" erwartet" },
            { Kinds.AssignAdd, "\"+=\" erwartet" },
            { Kinds.AssignSub, "\"-=\" erwartet" },
            { Kinds.AssignAnd, "\"&=\" erwartet" },
            { Kinds.AssignXOr, "\"^=\" erwartet" },
            { Kinds.AssignOr, "\"|=\" erwartet" },
            { Kinds.AssignShiftLeft, "\"<<=\" erwartet" },
            { Kinds.AssignShiftRight, "\">>=\" erwartet" },
            { Kinds.Or, "\"||\" erwartet" },
            { Kinds.And, "\"&&\" erwartet" },
            { Kinds.BitOr, "\"|\" erwartet" },
            { Kinds.BitXOr, "\"^\" erwartet" },
            { Kinds.BitAnd, "\"&\" erwartet" },
            { Kinds.NotEquals, "\"!=\" erwartet" },
            { Kinds.Equals, "\"==\" erwartet" },
            { Kinds.LesserEquals, "\"<=\" erwartet" },
            { Kinds.Lesser, "\"<\" erwartet" },
            { Kinds.GreaterEquals, "\">=\" erwartet" },
            { Kinds.Greater, "\">\" erwartet" },
            { Kinds.ShiftLeft, "\"<<\" erwartet" },
            { Kinds.ShiftRight, "\">>\" erwartet" },
            { Kinds.Add, "\"+\" erwartet" },
            { Kinds.Sub, "\"-\" erwartet" },
            { Kinds.Mul, "\"*\" erwartet" },
            { Kinds.Div, "\"/\" erwartet" },
            { Kinds.Mod, "\"%\" erwartet" },
            { Kinds.Incr, "\"++\" erwartet" },
            { Kinds.Decr, "\"--\" erwartet" },
            { Kinds.BitNot, "\"~\" erwartet" },
            { Kinds.Not, "\"!\" erwartet" },
            { Kinds.Dot, "\".\" erwartet" },
            { Kinds.Unknown, "Unbekannt" },
            { Kinds.ErrInvalidBlock, "Ungültiger Block" },
            { Kinds.ErrInvalidDeclaration, "Ungültige Deklaration" },
            { Kinds.ErrInvalidFunction, "Ungültige Funktion" },
            { Kinds.ErrInvalidInstance, "Ungültige Instance" },
            { Kinds.ErrInvalidPrototype, "Ungültiger Prototyp" },
            { Kinds.ErrInvalidClass, "Ungültige Klasse" },
            { Kinds.ErrInvalidConstantDeclaration, "Ungültige Konstantendeklaration" },
            { Kinds.ErrInvalidVariableDeclaration, "Ungültige Variablendeklaration" },
            { Kinds.ErrInvalidTypeDeclaration, "Ungültige Typendeklaration" },
            { Kinds.ErrInvalidType, "Ungültiger Typ" },
            { Kinds.ErrInvalidClassName, "Ungültiger Klassenname" },
            { Kinds.ErrInvalidBlocKBody, "Ungültiger Block" },
            { Kinds.ErrInvalidStatement, "Ungültiger Ausdruck" },
            { Kinds.ErrInvalidIfStatement, "Ungültige Abfrage" },
            { Kinds.ErrInvalidAssignment, "Ungültige Zuweisung" },
            { Kinds.ErrInvalidOperator, "Ungültiger Operator" },
            { Kinds.ErrInvalidExpression, "Ungültiger Ausdruck" },
            { Kinds.Identifier, "Dateiendeerwartet" },
            { Kinds.Identifier, "Dateiendeerwartet" },
            { Kinds.Identifier, "Dateiendeerwartet" },
        };

        public void SynErr(int line, int col, Kinds kind)
        {
            if (!messages.TryGetValue(kind, out var s))
            {
                switch (kind)
                {
                    default: s = $"error {(int)kind}"; break;
                }
            }
            else
            {
                s = $"{kind}: {s}";
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