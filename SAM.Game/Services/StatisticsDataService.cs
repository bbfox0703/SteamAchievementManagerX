using System;
using System.Collections.Generic;
using SAM.Game.Stats;

namespace SAM.Game.Services
{
    /// <summary>
    /// Manages statistics data loading and storage operations.
    /// </summary>
    internal class StatisticsDataService
    {
        private readonly API.Client _steamClient;
        private readonly List<StatDefinition> _statDefinitions;

        /// <summary>
        /// Initializes a new instance of the StatisticsDataService.
        /// </summary>
        /// <param name="steamClient">The Steam API client</param>
        /// <param name="statDefinitions">List of statistic definitions from schema</param>
        public StatisticsDataService(
            API.Client steamClient,
            List<StatDefinition> statDefinitions)
        {
            this._steamClient = steamClient;
            this._statDefinitions = statDefinitions;
        }

        /// <summary>
        /// Loads statistics from Steam API.
        /// </summary>
        /// <returns>List of statistics loaded from Steam</returns>
        public List<StatInfo> LoadStatistics()
        {
            var statistics = new List<StatInfo>();

            foreach (var stat in this._statDefinitions)
            {
                if (string.IsNullOrEmpty(stat.Id) == true)
                {
                    continue;
                }

                if (stat is IntegerStatDefinition intStat)
                {
                    if (this._steamClient.SteamUserStats.GetStatValue(intStat.Id, out int value) == false)
                    {
                        continue;
                    }
                    statistics.Add(new IntStatInfo()
                    {
                        Id = intStat.Id,
                        DisplayName = intStat.DisplayName,
                        IntValue = value,
                        OriginalValue = value,
                        IsIncrementOnly = intStat.IncrementOnly,
                        Permission = intStat.Permission,
                    });
                }
                else if (stat is FloatStatDefinition floatStat)
                {
                    if (this._steamClient.SteamUserStats.GetStatValue(floatStat.Id, out float value) == false)
                    {
                        continue;
                    }
                    statistics.Add(new FloatStatInfo()
                    {
                        Id = floatStat.Id,
                        DisplayName = floatStat.DisplayName,
                        FloatValue = value,
                        OriginalValue = value,
                        IsIncrementOnly = floatStat.IncrementOnly,
                        Permission = floatStat.Permission,
                    });
                }
            }

            return statistics;
        }

        /// <summary>
        /// Stores modified statistics to Steam API.
        /// </summary>
        /// <param name="statistics">List of statistics to store</param>
        /// <returns>Number of statistics stored, or -1 on error</returns>
        public int StoreStatistics(List<StatInfo> statistics)
        {
            if (statistics.Count == 0)
            {
                return 0;
            }

            foreach (var stat in statistics)
            {
                if (stat is IntStatInfo intStat)
                {
                    if (this._steamClient.SteamUserStats.SetStatValue(
                        intStat.Id,
                        intStat.IntValue) == false)
                    {
                        return -1;
                    }
                }
                else if (stat is FloatStatInfo floatStat)
                {
                    if (this._steamClient.SteamUserStats.SetStatValue(
                        floatStat.Id,
                        floatStat.FloatValue) == false)
                    {
                        return -1;
                    }
                }
                else
                {
                    throw new InvalidOperationException("unsupported stat type");
                }
            }

            return statistics.Count;
        }
    }
}
