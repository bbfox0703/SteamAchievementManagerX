#nullable enable

using System;
using System.Diagnostics;
using System.IO;

namespace SAM.Picker.Services
{
    /// <summary>
    /// Handles launching SAM.Game.exe for a specific game.
    /// </summary>
    internal static class GameLauncher
    {
        /// <summary>
        /// Launches SAM.Game.exe with the specified game ID.
        /// </summary>
        /// <param name="gameId">The Steam game ID</param>
        /// <returns>True if launch succeeded, false otherwise</returns>
        public static bool LaunchGame(uint gameId)
        {
            try
            {
                string gamePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SAM.Game.exe");

                if (!File.Exists(gamePath))
                {
                    return false;
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = gamePath,
                    Arguments = gameId.ToString(),
                    UseShellExecute = false,
                    WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory
                });

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
