using System;
using System.Diagnostics;

namespace Uavcan.NET.Studio
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

            Debug.Assert(false);
        }

        public static void Info(string v)
        {
            Log(v);
        }
    }
}
