using System.Collections;

namespace Peter.DParser
{
    internal struct TokenMatch
    {
        public string Value;
        public int Position;

        public TokenMatch(string val, int pos)
        {
            this.Value = val;
            this.Position = pos;
        }
    }

    public class DCodeInfo
    {
        public DCodeInfo()
        {
            this.VarDeclarations = new ArrayList();
            this.ConstDeclarations = new ArrayList();
            this.Functions = new ArrayList();
            this.Instances = new ArrayList();

        }

        /// <summary>
        /// Gets or Sets the List of 'using ...' in the code...
        /// </summary>


        public ArrayList VarDeclarations { get; set; }
        public ArrayList ConstDeclarations { get; set; }

        /// <summary>
        /// Gets or Sets the List of 'namespace ...' in the code...
        /// </summary>
        public ArrayList Functions { get; set; }

        /// <summary>
        /// Gets or Sets the List of Fields in the code...
        /// </summary>
        public ArrayList Instances { get; set; }


    }
}
