#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Picker.Services
{
    /// <summary>
    /// Provides game filtering and searching functionality.
    /// </summary>
    internal static class GameListFilter
    {
        /// <summary>
        /// Filters games based on name search and type filters.
        /// </summary>
        /// <param name="games">Source game collection</param>
        /// <param name="nameSearch">Optional name search text (null for no search)</param>
        /// <param name="wantNormals">Include normal games</param>
        /// <param name="wantDemos">Include demo games</param>
        /// <param name="wantMods">Include mod games</param>
        /// <param name="wantJunk">Include junk games</param>
        /// <returns>Filtered and sorted list of games</returns>
        public static List<GameInfo> FilterGames(
            IEnumerable<GameInfo> games,
            string? nameSearch,
            bool wantNormals,
            bool wantDemos,
            bool wantMods,
            bool wantJunk)
        {
            var filtered = new List<GameInfo>();

            foreach (var info in games)
            {
                // Name search filter
                if (nameSearch != null &&
                    info.Name.IndexOf(nameSearch, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                // Type filter
                bool wanted = info.Type switch
                {
                    "normal" => wantNormals,
                    "demo" => wantDemos,
                    "mod" => wantMods,
                    "junk" => wantJunk,
                    _ => true,
                };

                if (wanted)
                {
                    filtered.Add(info);
                }
            }

            return filtered;
        }
    }
}
