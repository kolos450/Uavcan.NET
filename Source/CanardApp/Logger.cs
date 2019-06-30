using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CanardApp
{
    static class Logger
    {
        public static void Log(string message, Exception ex)
        {
            if (message == null)
            {
                Log(ex?.ToString());
            }
            else
            {
                Log(message + Environment.NewLine + ex?.ToString());
            }
        }

        public static void Log(Exception ex)
        {
            Log(ex?.ToString());
        }

        public static void Log(string message)
        {
            message = message ?? "<null>";
            Trace.WriteLine(message);

            Console.WriteLine(message);
        }

        public static void Info(string v)
        {
            Log(v);
        }
    }
}
