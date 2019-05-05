using System;
using System.Collections.Generic;
using System.Text;

namespace Peter.CSSParser
{
    /// <summary></summary>
    public class Property
    {
        private List<PropertyValue> values = new List<PropertyValue>();

        /// <summary></summary>
        public string Attribute { get; set; }

        /// <summary></summary>
        public List<PropertyValue> Values
        {
            get { return this.values; }
            set { this.values = value; }
        }
    }
}
