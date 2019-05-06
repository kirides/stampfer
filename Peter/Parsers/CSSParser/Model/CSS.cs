using System;
using System.Collections.Generic;
using System.Text;

namespace Peter.CSSParser
{
    /// <summary></summary>
    public class CSS
    {
        private List<Selector> selectors = new List<Selector>();

        /// <summary></summary>
        public string FileName { get; set; }

        /// <summary></summary>
        public List<Selector> Selectors
        {
            get { return selectors; }
            set { selectors = value; }
        }
    }
}
