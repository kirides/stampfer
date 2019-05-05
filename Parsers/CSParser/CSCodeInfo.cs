using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;

namespace Peter.CSParser
{
    struct TokenMatch
    {
        public string Value;
        public int Position;

        public TokenMatch(string val, int pos)
        {
            this.Value = val;
            this.Position = pos;
        }
    }

    public class CSCodeInfo
    {
        private ArrayList m_Constructors;

        public CSCodeInfo()
        {
            this.Usings = new ArrayList();
            this.NameSpaces = new ArrayList();
            this.Fields = new ArrayList();
            this.Methods = new ArrayList();
            this.Properties = new ArrayList();
            this.m_Constructors = new ArrayList();
        }

        /// <summary>
        /// Gets or Sets the List of 'using ...' in the code...
        /// </summary>
        public ArrayList Usings { get; set; }

        /// <summary>
        /// Gets or Sets the List of 'namespace ...' in the code...
        /// </summary>
        public ArrayList NameSpaces { get; set; }

        /// <summary>
        /// Gets or Sets the List of Fields in the code...
        /// </summary>
        public ArrayList Fields { get; set; }

        /// <summary>
        /// Gets or Sets the List of Methods in the code...
        /// </summary>
        public ArrayList Methods { get; set; }

        /// <summary>
        /// Gets or Sets the List of Properties in the code...
        /// </summary>
        public ArrayList Properties { get; set; }

        /// <summary>
        /// Gets or Sets the List of Constructors in the code...
        /// </summary>
        public ArrayList Constructors
        {
            get { return this.m_Constructors; }

            set { this.m_Constructors = value; }
        }
    }
}
