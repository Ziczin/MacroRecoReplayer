using System;
using System.Diagnostics;

namespace MacroRecoReplayer
{
    public static class Logger
    {
        [Conditional("DEBUG")]
        public static void InitConsole()
        {
            Debug.WriteLine("=== DEBUG MODE ACTIVE ===");
        }

        [Conditional("DEBUG")]
        public static void Log(string message)
        {
            Debug.WriteLine($"[{DateTime.Now:HH:mm:ss}] {message}");
        }
    }
}