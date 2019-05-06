using System;
using System.Collections.Generic;
using System.Text;

namespace Peter.CSSParser
{
    /// <summary></summary>
    public class Tag
    {
        private string id;

        /// <summary></summary>
        public TagType TagType { get; set; }

        /// <summary></summary>
        public bool IsIDSelector
        {
            //get { return (int)(this.tagtype & TagType.IDed) > 0; }
            get { return id != null; }
        }

        /// <summary></summary>
        public bool HasName
        {
            get { return Name != null; }
        }

        /// <summary></summary>
        public bool HasClass
        {
            get { return Class != null; }
        }

        /// <summary></summary>
        public bool HasPseudoClass
        {
            get { return Pseudo != null; }
        }

        /// <summary></summary>
        public string Name { get; set; }

        /// <summary></summary>
        public string Class { get; set; }

        /// <summary></summary>
        public string Pseudo { get; set; }

        /// <summary></summary>
        public string Id
        {
            get { return id; }
            set { id = value; }
        }

        /// <summary></summary>
        public List<Tag> SubTags { get; set; } = new List<Tag>();

        /// <summary></summary>
        /// <returns></returns>
        public override string ToString()
        {
            System.Text.StringBuilder txt = new System.Text.StringBuilder(ToShortString());

            foreach (Tag t in SubTags)
            {
                txt.Append(" ");
                txt.Append(t.ToString());
            }
            return txt.ToString();
        }

        /// <summary></summary>
        /// <returns></returns>
        public string ToShortString()
        {
            System.Text.StringBuilder txt = new System.Text.StringBuilder();
            if (HasName)
            {
                txt.Append(Name);
            }
            if (HasClass)
            {
                txt.Append(".");
                txt.Append(Class);
            }
            if (IsIDSelector)
            {
                txt.Append("#");
                txt.Append(id);
            }
            if (HasPseudoClass)
            {
                txt.Append(":");
                txt.Append(Pseudo);
            }
            return txt.ToString();
        }
    }
}
