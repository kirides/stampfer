using System.Collections;

namespace Peter.CParser
{
    public class CCodeInfo
    {
        public CCodeInfo()
        {
            this.Includes = new ArrayList();
            this.GlobalVariables = new ArrayList();
            this.Functions = new ArrayList();
            this.Prototypes = new ArrayList();
            this.Defines = new ArrayList();
            this.Structs = new ArrayList();
            this.TypeDefs = new ArrayList();
        }

        /// <summary>
        /// Gets or Sets the List of 'includes ...' in the code...
        /// </summary>
        public ArrayList Includes { get; set; }

        /// <summary>
        /// Gets or Sets the List of Global Variables in the code...
        /// </summary>
        public ArrayList GlobalVariables { get; set; }

        /// <summary>
        /// Gets or Sets the List of Functions in the code...
        /// </summary>
        public ArrayList Functions { get; set; }

        /// <summary>
        /// Gets or Sets the List of Prototypes in the code...
        /// </summary>
        public ArrayList Prototypes { get; set; }

        /// <summary>
        /// Gets or Sets the List of Defines in the Code...
        /// </summary>
        public ArrayList Defines { get; set; }

        /// <summary>
        /// Gets or Sets the List of Structs in the Code...
        /// </summary>
        public ArrayList Structs { get; set; }

        /// <summary>
        /// Gets or Sets the List of TypeDefs in the Code...
        /// </summary>
        public ArrayList TypeDefs { get; set; }
    }
}
