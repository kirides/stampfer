using System;

namespace PeterInterface
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class PeterPluginAttribute : Attribute
    {
        public PeterPluginAttribute(PeterPluginType type)
        {
            this.Type = type;
        }

        public PeterPluginType Type { get; }
    }
}
