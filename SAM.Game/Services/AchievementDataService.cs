using System;
using System.Collections.Generic;
using SAM.Game.Stats;
using static SAM.API.Utilities.InvariantShorthand;

namespace SAM.Game.Services
{
    /// <summary>
    /// Manages achievement data loading, filtering, and storage operations.
    /// </summary>
    internal class AchievementDataService
    {
        private readonly API.Client _steamClient;
        private readonly List<AchievementDefinition> _achievementDefinitions;

        /// <summary>
        /// Initializes a new instance of the AchievementDataService.
        /// </summary>
        /// <param name="steamClient">The Steam API client</param>
        /// <param name="achievementDefinitions">List of achievement definitions from schema</param>
        public AchievementDataService(
            API.Client steamClient,
            List<AchievementDefinition> achievementDefinitions)
        {
            this._steamClient = steamClient;
            this._achievementDefinitions = achievementDefinitions;
        }

        /// <summary>
        /// Loads achievements from Steam API with optional filtering.
        /// </summary>
        /// <param name="wantLocked">True to include locked achievements</param>
        /// <param name="wantUnlocked">True to include unlocked achievements</param>
        /// <param name="textSearch">Optional text search filter (searches name and description)</param>
        /// <returns>List of filtered achievement info objects</returns>
        public List<AchievementInfo> LoadAchievements(
            bool wantLocked,
            bool wantUnlocked,
            string? textSearch)
        {
            var achievements = new List<AchievementInfo>();

            foreach (var def in this._achievementDefinitions)
            {
                if (string.IsNullOrEmpty(def.Id))
                {
                    continue;
                }

                if (this._steamClient.SteamUserStats.GetAchievementAndUnlockTime(
                    def.Id,
                    out bool isAchieved,
                    out var unlockTime) == false)
                {
                    continue;
                }

                // Filter by locked/unlocked state
                bool wanted = (wantLocked == false && wantUnlocked == false) || isAchieved switch
                {
                    true => wantUnlocked,
                    false => wantLocked,
                };
                if (wanted == false)
                {
                    continue;
                }

                // Filter by text search
                if (textSearch != null)
                {
                    if (def.Name.IndexOf(textSearch, StringComparison.OrdinalIgnoreCase) < 0 &&
                        (string.IsNullOrEmpty(def.Description) ||
                         def.Description.IndexOf(textSearch, StringComparison.OrdinalIgnoreCase) < 0))
                    {
                        continue;
                    }
                }

                var info = new AchievementInfo
                {
                    Id = def.Id,
                    IsAchieved = isAchieved,
                    UnlockTime = isAchieved == true && unlockTime > 0
                        ? DateTimeOffset.FromUnixTimeSeconds(unlockTime).LocalDateTime
                        : null,
                    IconNormal = string.IsNullOrEmpty(def.IconNormal) ? null : def.IconNormal,
                    IconLocked = string.IsNullOrEmpty(def.IconLocked) ? def.IconNormal : def.IconLocked,
                    Permission = def.Permission,
                    Name = def.Name,
                    Description = def.Description,
                };

                achievements.Add(info);
            }

            return achievements;
        }

        /// <summary>
        /// Stores achievement changes to Steam API.
        /// </summary>
        /// <param name="achievements">List of achievements to store</param>
        /// <returns>Number of achievements stored, or -1 on error</returns>
        public int StoreAchievements(List<AchievementInfo> achievements)
        {
            if (achievements.Count == 0)
            {
                return 0;
            }

            foreach (var info in achievements)
            {
                if (this._steamClient.SteamUserStats.SetAchievement(info.Id, info.IsAchieved) == false)
                {
                    return -1;
                }
            }

            return achievements.Count;
        }
    }
}
