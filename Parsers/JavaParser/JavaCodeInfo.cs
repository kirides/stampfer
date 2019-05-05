using System;
using System.Collections;

namespace Peter.JavaParser
{
    public class JavaCodeInfo
    {
        private ArrayList m_Constructors;

        public JavaCodeInfo()
        {
            this.Imports = new ArrayList();
            this.NameSpaces = new ArrayList();
            this.Fields = new ArrayList();
            this.Methods = new ArrayList();
            this.m_Constructors = new ArrayList();
        }

        /// <summary>
        /// Gets or Sets the List of 'using ...' in the code...
        /// </summary>
        public ArrayList Imports { get; set; }

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
        /// Gets or Sets the List of Constructors in the code...
        /// </summary>
        public ArrayList Constructors
        {
            get { return this.m_Constructors; }

            set { this.m_Constructors = value; }
        }
    }
}
