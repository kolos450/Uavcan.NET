﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CanardSharp.Dsdl
{
    public interface IUavcanTypeFullName
    {
        string Namespace { get; }
        string Name { get; }
        string FullName { get; }
    }
}
