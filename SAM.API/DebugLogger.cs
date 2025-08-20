using System;
using System.Diagnostics;

namespace SAM.API
{
    public static class DebugLogger
    {
        [Conditional("DEBUG")]
        public static void Log(string message)
        {
            Debug.WriteLine($"[{DateTime.Now:O}] {message}");
        }

        [Conditional("DEBUG")]
        public static void Log(Exception ex)
        {
            Debug.WriteLine($"[{DateTime.Now:O}] {ex}");
        }

        [Conditional("DEBUG")]
        public static void Log(string message, Exception ex)
        {
            Debug.WriteLine($"[{DateTime.Now:O}] {message} {ex}");
        }
    }
}
