/**************************************************************************************
    Stampfer - Gothic Script Editor
    Copyright (C) 2009 Alexander "Sumpfkrautjunkie" Ruppert

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
**************************************************************************************/
using System;

namespace Peter
{
    [Serializable()]
    public class Instance : IComparable, IComparable<Instance>
    {
        public string Name;
        public string File;
        public string Params;
        public int Line;
        public Instance(string s1, string s2)
        {
            Name = s1;
            File = s2;

        }
        public Instance(string s1, int i1)
        {
            Name = s1;
            Line = i1;

        }
        public override string ToString() => Name;
        int IComparable.CompareTo(object obj)
        {
            return CompareTo(obj as Instance);
        }
        public int CompareTo(Instance other)
        {
            return string.Compare(this.ToString(), other.ToString(), true);
        }
    }
}
