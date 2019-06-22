using System;

namespace CanardSharp.Dsdl
{
    public class UavcanTypeMeta
    {
        public string Namespace { get; set; }
        public string Name { get; set; }
        public string FullName => string.IsNullOrEmpty(Name) ? Name : Namespace + "." + Name;
        public Version Version { get; set; }
        public int? DefaultDTID { get; set; }
    }
}
