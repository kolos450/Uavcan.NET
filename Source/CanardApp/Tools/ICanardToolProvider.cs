using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CanardApp.Tools
{
    public interface ICanardToolProvider
    {
        string ToolTitle { get; }

        void Execute();
    }
}
