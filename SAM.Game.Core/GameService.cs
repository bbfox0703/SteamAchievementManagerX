using System;
using System.Collections.Generic;
using SAM.Game.Stats;
using SAM.API.Types;
#if !WINDOWS
using Steamworks;
#endif

namespace SAM.Game.Core;

/// <summary>
/// Provides non-UI logic for retrieving and manipulating game statistics
/// and achievements. Intended to be shared between different UI layers.
/// </summary>
public class GameService
{
    private readonly long _gameId;
    private readonly API.Client _client;

    public GameService(long gameId, API.Client client)
    {
        _gameId = gameId;
        _client = client;
    }

    /// <summary>
    /// Request the latest stats from the Steam API for the current user.
    /// </summary>
    /// <returns>True if the request was issued.</returns>
    public bool RequestCurrentStats()
    {
#if WINDOWS
        var steamId = _client.SteamUser.GetSteamId();
        var callHandle = _client.SteamUserStats.RequestUserStats(steamId);
        return callHandle != API.CallHandle.Invalid;
#else
        SteamUser.GetSteamID();
        return SteamUserStats.RequestCurrentStats();
#endif
    }

    /// <summary>
    /// Retrieve current achievement information after stats have been loaded.
    /// </summary>
    public IReadOnlyList<AchievementInfo> GetAchievements()
    {
        var list = new List<AchievementInfo>();
#if WINDOWS
        uint count = _client.SteamUserStats.GetNumAchievements();
        for (uint i = 0; i < count; i++)
        {
            var id = _client.SteamUserStats.GetAchievementName(i);
            if (string.IsNullOrEmpty(id))
            {
                continue;
            }

            _client.SteamUserStats.GetAchievementAndUnlockTime(id, out bool achieved, out var unlockTime);
            _client.SteamUserStats.GetAchievementDisplayAttribute(id, "name", out string name);
            _client.SteamUserStats.GetAchievementDisplayAttribute(id, "desc", out string desc);
            _client.SteamUserStats.GetAchievementDisplayAttribute(id, "icon", out string icon);
            _client.SteamUserStats.GetAchievementDisplayAttribute(id, "icongray", out string iconGray);

            list.Add(new AchievementInfo
            {
                Id = id,
                IsAchieved = achieved,
                UnlockTime = achieved && unlockTime > 0 ? DateTimeOffset.FromUnixTimeSeconds(unlockTime).LocalDateTime : null,
                IconNormal = string.IsNullOrEmpty(icon) ? null : icon,
                IconLocked = string.IsNullOrEmpty(iconGray) ? icon : iconGray,
                Name = name,
                Description = desc,
            });
        }
#else
        uint count = SteamUserStats.GetNumAchievements();
        for (uint i = 0; i < count; i++)
        {
            var id = SteamUserStats.GetAchievementName(i);
            if (string.IsNullOrEmpty(id))
            {
                continue;
            }

            SteamUserStats.GetAchievementAndUnlockTime(id, out bool achieved, out uint unlockTime);
            var name = SteamUserStats.GetAchievementDisplayAttribute(id, "name");
            var desc = SteamUserStats.GetAchievementDisplayAttribute(id, "desc");
            var icon = SteamUserStats.GetAchievementDisplayAttribute(id, "icon");
            var iconGray = SteamUserStats.GetAchievementDisplayAttribute(id, "icongray");

            list.Add(new AchievementInfo
            {
                Id = id,
                IsAchieved = achieved,
                UnlockTime = achieved && unlockTime > 0 ? DateTimeOffset.FromUnixTimeSeconds(unlockTime).LocalDateTime : null,
                IconNormal = string.IsNullOrEmpty(icon) ? null : icon,
                IconLocked = string.IsNullOrEmpty(iconGray) ? icon : iconGray,
                Name = name,
                Description = desc,
            });
        }
#endif
        return list;
    }

    /// <summary>
    /// Retrieve current statistics after stats have been loaded.
    /// </summary>
    public IReadOnlyList<StatInfo> GetStats()
    {
        var list = new List<StatInfo>();
#if WINDOWS
        uint count = _client.SteamUserStats.GetNumStats();
        for (uint i = 0; i < count; i++)
        {
            var id = _client.SteamUserStats.GetStatName(i);
            if (string.IsNullOrEmpty(id))
            {
                continue;
            }

            var type = _client.SteamUserStats.GetStatType(id);
            switch (type)
            {
                case UserStatType.Integer:
                    if (_client.SteamUserStats.GetStat(id, out int intValue))
                    {
                        var info = new IntStatInfo { Id = id, DisplayName = id, IntValue = intValue, OriginalValue = intValue };
                        list.Add(info);
                    }
                    break;
                case UserStatType.Float:
                    if (_client.SteamUserStats.GetStat(id, out float floatValue))
                    {
                        var info = new FloatStatInfo { Id = id, DisplayName = id, FloatValue = floatValue, OriginalValue = floatValue };
                        list.Add(info);
                    }
                    break;
            }
        }
#else
        uint count = SteamUserStats.GetNumStats();
        for (uint i = 0; i < count; i++)
        {
            var id = SteamUserStats.GetStatName(i);
            if (string.IsNullOrEmpty(id))
            {
                continue;
            }

            var type = (UserStatType)SteamUserStats.GetStatType(id);
            switch (type)
            {
                case UserStatType.Integer:
                    if (SteamUserStats.GetStat(id, out int intValue))
                    {
                        var info = new IntStatInfo { Id = id, DisplayName = id, IntValue = intValue, OriginalValue = intValue };
                        list.Add(info);
                    }
                    break;
                case UserStatType.Float:
                    if (SteamUserStats.GetStat(id, out float floatValue))
                    {
                        var info = new FloatStatInfo { Id = id, DisplayName = id, FloatValue = floatValue, OriginalValue = floatValue };
                        list.Add(info);
                    }
                    break;
            }
        }
#endif
        return list;
    }

    /// <summary>
    /// Store current stats back to Steam.
    /// </summary>
    public bool StoreStats()
    {
#if WINDOWS
        return _client.SteamUserStats.StoreStats();
#else
        return SteamUserStats.StoreStats();
#endif
    }

    /// <summary>
    /// Set an achievement's unlocked state.
    /// </summary>
    public bool SetAchievement(string id, bool achieved)
    {
#if WINDOWS
        return _client.SteamUserStats.SetAchievement(id, achieved);
#else
        return achieved ? SteamUserStats.SetAchievement(id) : SteamUserStats.ClearAchievement(id);
#endif
    }
}
