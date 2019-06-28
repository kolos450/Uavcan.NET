﻿using CanardSharp.Dsdl.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CanardSharp.Dsdl
{
    public abstract class IUavcanTypeResolver
    {
        public virtual UavcanType ResolveType(string ns, string typeName)
        {
            return TryResolveType(ns, typeName) ??
                 throw new Exception($"Type definition not found: {typeName}.");
        }

        public abstract UavcanType TryResolveType(string ns, string typeName);
    }
}
