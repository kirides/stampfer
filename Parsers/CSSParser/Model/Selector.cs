using System;
using System.Collections.Generic;
using System.Text;

namespace Peter.CSSParser
{
    /// <summary></summary>
    public class Selector
    {
        private List<Property> properties = new List<Property>();

        /// <summary></summary>
        public List<Tag> Tags { get; set; } = new List<Tag>();

        /// <summary></summary>
        public List<Property> Properties
        {
            get { return properties; }
            set { properties = value; }
        }
    }
}
