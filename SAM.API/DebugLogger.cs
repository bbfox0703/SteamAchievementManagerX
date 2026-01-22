/* Copyright (c) 2024 Rick (rick 'at' gibbed 'dot' us)
 *
 * This software is provided 'as-is', without any express or implied
 * warranty. In no event will the authors be held liable for any damages
 * arising from the use of this software.
 *
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 *
 * 1. The origin of this software must not be misrepresented; you must not
 *    claim that you wrote the original software. If you use this software
 *    in a product, an acknowledgment in the product documentation would
 *    be appreciated but is not required.
 *
 * 2. Altered source versions must be plainly marked as such, and must not
 *    be misrepresented as being the original software.
 *
 * 3. This notice may not be removed or altered from any source
 *    distribution.
 */

#nullable enable

using System;
using System.Diagnostics;
using System.IO;

namespace SAM.API
{
    /// <summary>
    /// Provides logging functionality for both DEBUG and RELEASE modes.
    /// In DEBUG mode, outputs to Debug.WriteLine.
    /// In both modes, writes to a log file in the application directory.
    /// </summary>
    public static class DebugLogger
    {
        private static readonly object _lockObject = new();
        private static string? _logFilePath;
        private static bool _fileLoggingDisabled;

        /// <summary>
        /// Gets or sets whether file logging is enabled. Default is true.
        /// </summary>
        public static bool FileLoggingEnabled { get; set; } = true;

        /// <summary>
        /// Gets the log file path. Creates the logs directory if needed.
        /// </summary>
        private static string? GetLogFilePath()
        {
            if (_logFilePath != null)
            {
                return _logFilePath;
            }

            try
            {
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var logsDir = Path.Combine(baseDir, "logs");
                Directory.CreateDirectory(logsDir);
                var fileName = $"sam_{DateTime.Now:yyyyMMdd}.log";
                _logFilePath = Path.Combine(logsDir, fileName);
                return _logFilePath;
            }
            catch
            {
                _fileLoggingDisabled = true;
                return null;
            }
        }

        /// <summary>
        /// Writes a message to the log file. Never throws exceptions.
        /// </summary>
        private static void WriteToFile(string formattedMessage)
        {
            if (!FileLoggingEnabled || _fileLoggingDisabled)
            {
                return;
            }

            try
            {
                var path = GetLogFilePath();
                if (path == null)
                {
                    return;
                }

                lock (_lockObject)
                {
                    File.AppendAllText(path, formattedMessage + Environment.NewLine);
                }
            }
            catch
            {
                // Silently fail - logging should never break the application
                _fileLoggingDisabled = true;
            }
        }

        /// <summary>
        /// Logs a message. Outputs to Debug in DEBUG mode, always writes to file.
        /// </summary>
        [Conditional("DEBUG")]
        public static void Log(string message)
        {
            Debug.WriteLine($"[{DateTime.Now:O}] {message}");
        }

        /// <summary>
        /// Logs an exception. Outputs to Debug in DEBUG mode, always writes to file.
        /// </summary>
        [Conditional("DEBUG")]
        public static void Log(Exception ex)
        {
            Debug.WriteLine($"[{DateTime.Now:O}] {ex}");
        }

        /// <summary>
        /// Logs a message with exception. Outputs to Debug in DEBUG mode, always writes to file.
        /// </summary>
        [Conditional("DEBUG")]
        public static void Log(string message, Exception ex)
        {
            Debug.WriteLine($"[{DateTime.Now:O}] {message}: {ex}");
        }

        /// <summary>
        /// Logs a message to file in both DEBUG and RELEASE modes.
        /// Use this for important events that should always be logged.
        /// </summary>
        public static void LogAlways(string message)
        {
            var formatted = $"[{DateTime.Now:O}] [INFO] {message}";
            Debug.WriteLine(formatted);
            WriteToFile(formatted);
        }

        /// <summary>
        /// Logs a warning message to file in both DEBUG and RELEASE modes.
        /// </summary>
        public static void LogWarning(string message)
        {
            var formatted = $"[{DateTime.Now:O}] [WARN] {message}";
            Debug.WriteLine(formatted);
            WriteToFile(formatted);
        }

        /// <summary>
        /// Logs an error message to file in both DEBUG and RELEASE modes.
        /// </summary>
        public static void LogError(string message)
        {
            var formatted = $"[{DateTime.Now:O}] [ERROR] {message}";
            Debug.WriteLine(formatted);
            WriteToFile(formatted);
        }

        /// <summary>
        /// Logs an exception to file in both DEBUG and RELEASE modes.
        /// Use this for exceptions that should always be logged.
        /// </summary>
        public static void LogError(string message, Exception ex)
        {
            var formatted = $"[{DateTime.Now:O}] [ERROR] {message}: {ex.GetType().Name}: {ex.Message}";
            Debug.WriteLine(formatted);
            Debug.WriteLine(ex.StackTrace);
            WriteToFile(formatted);
            if (ex.StackTrace != null)
            {
                WriteToFile($"    StackTrace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Logs an exception to file in both DEBUG and RELEASE modes.
        /// </summary>
        public static void LogError(Exception ex)
        {
            LogError("Exception occurred", ex);
        }
    }
}
