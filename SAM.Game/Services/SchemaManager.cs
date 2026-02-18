using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using SAM.Game.Stats;
using APITypes = SAM.API.Types;
using static SAM.API.Utilities.InvariantShorthand;

namespace SAM.Game.Services
{
    /// <summary>
    /// Manages loading and parsing of Steam user game stats schema (VDF format).
    /// Handles achievement and statistic definitions with localization support.
    /// </summary>
    internal class SchemaManager
    {
        private readonly long _gameId;
        private readonly string _language;

        /// <summary>
        /// Initializes a new instance of the SchemaManager.
        /// </summary>
        /// <param name="gameId">The Steam game ID</param>
        /// <param name="language">The language code for localization (e.g., "english", "schinese")</param>
        public SchemaManager(long gameId, string language)
        {
            this._gameId = gameId;
            this._language = language;
        }

        /// <summary>
        /// Loads and parses the user game stats schema.
        /// </summary>
        /// <param name="achievements">Output list of achievement definitions</param>
        /// <param name="stats">Output list of statistic definitions</param>
        /// <returns>True if schema was loaded successfully, false otherwise</returns>
        public bool LoadSchema(
            out List<AchievementDefinition> achievements,
            out List<StatDefinition> stats)
        {
            achievements = new List<AchievementDefinition>();
            stats = new List<StatDefinition>();

            string path;
            try
            {
                string fileName = _($"UserGameStatsSchema_{this._gameId}.bin");
                path = API.Steam.GetInstallPath();
                path = Path.Combine(path, "appcache", "stats", fileName);

                if (!File.Exists(path))
                {
                    return false;
                }
            }
            catch (Exception)
            {
                return false;
            }

            var kv = KeyValue.LoadAsBinary(path);
            if (kv == null)
            {
                return false;
            }

            var statsNode = kv[this._gameId.ToString(CultureInfo.InvariantCulture)]["stats"];
            if (!statsNode.Valid || statsNode.Children == null)
            {
                return false;
            }

            foreach (var stat in statsNode.Children)
            {
                if (!stat.Valid)
                {
                    continue;
                }

                APITypes.UserStatType type;

                // schema in the new format?
                var typeNode = stat["type"];
                if (typeNode.Valid == true && typeNode.Type == KeyValueType.String)
                {
                    if (Enum.TryParse((string)typeNode.Value!, true, out type) == false)
                    {
                        type = APITypes.UserStatType.Invalid;
                    }
                }
                else
                {
                    type = APITypes.UserStatType.Invalid;
                }

                // schema in the old format?
                if (type == APITypes.UserStatType.Invalid)
                {
                    var typeIntNode = stat["type_int"];
                    var rawType = typeIntNode.Valid == true
                        ? typeIntNode.AsInteger(0)
                        : typeNode.AsInteger(0);
                    type = (APITypes.UserStatType)rawType;
                }

                switch (type)
                {
                    case APITypes.UserStatType.Invalid:
                        break;

                    case APITypes.UserStatType.Integer:
                        stats.Add(ParseIntegerStat(stat));
                        break;

                    case APITypes.UserStatType.Float:
                    case APITypes.UserStatType.AverageRate:
                        stats.Add(ParseFloatStat(stat));
                        break;

                    case APITypes.UserStatType.Achievements:
                    case APITypes.UserStatType.GroupAchievements:
                        ParseAchievements(stat, achievements);
                        break;

                    default:
                        throw new InvalidOperationException("invalid stat type");
                }
            }

            return true;
        }

        private IntegerStatDefinition ParseIntegerStat(KeyValue stat)
        {
            var id = stat["name"].AsString("");
            string name = GetLocalizedString(stat["display"]["name"], id);

            return new IntegerStatDefinition
            {
                Id = id,
                DisplayName = name,
                MinValue = stat["min"].AsInteger(int.MinValue),
                MaxValue = stat["max"].AsInteger(int.MaxValue),
                MaxChange = stat["maxchange"].AsInteger(0),
                IncrementOnly = stat["incrementonly"].AsBoolean(false),
                SetByTrustedGameServer = stat["bSetByTrustedGS"].AsBoolean(false),
                DefaultValue = stat["default"].AsInteger(0),
                Permission = stat["permission"].AsInteger(0),
            };
        }

        private FloatStatDefinition ParseFloatStat(KeyValue stat)
        {
            var id = stat["name"].AsString("");
            string name = GetLocalizedString(stat["display"]["name"], id);

            return new FloatStatDefinition
            {
                Id = id,
                DisplayName = name,
                MinValue = stat["min"].AsFloat(float.MinValue),
                MaxValue = stat["max"].AsFloat(float.MaxValue),
                MaxChange = stat["maxchange"].AsFloat(0.0f),
                IncrementOnly = stat["incrementonly"].AsBoolean(false),
                DefaultValue = stat["default"].AsFloat(0.0f),
                Permission = stat["permission"].AsInteger(0),
            };
        }

        private void ParseAchievements(KeyValue stat, List<AchievementDefinition> achievements)
        {
            if (stat.Children == null)
            {
                return;
            }

            foreach (var bits in stat.Children.Where(
                b => string.Compare(b.Name, "bits", StringComparison.InvariantCultureIgnoreCase) == 0))
            {
                if (!bits.Valid || bits.Children == null)
                {
                    continue;
                }

                foreach (var bit in bits.Children)
                {
                    string id = bit["name"].AsString("");
                    string name = GetLocalizedString(bit["display"]["name"], id);
                    string desc = GetLocalizedString(bit["display"]["desc"], "");

                    achievements.Add(new AchievementDefinition
                    {
                        Id = id,
                        Name = name,
                        Description = desc,
                        IconNormal = bit["display"]["icon"].AsString(""),
                        IconLocked = bit["display"]["icon_gray"].AsString(""),
                        IsHidden = bit["display"]["hidden"].AsBoolean(false),
                        Permission = bit["permission"].AsInteger(0),
                    });
                }
            }
        }

        /// <summary>
        /// Gets a localized string from a KeyValue node with fallback logic.
        /// Falls back: requested language → english → raw value → default value
        /// </summary>
        private string GetLocalizedString(KeyValue kv, string defaultValue)
        {
            var name = kv[this._language].AsString("");
            if (!string.IsNullOrEmpty(name))
            {
                return name;
            }

            if (this._language != "english")
            {
                name = kv["english"].AsString("");
                if (!string.IsNullOrEmpty(name))
                {
                    return name;
                }
            }

            name = kv.AsString("");
            if (!string.IsNullOrEmpty(name))
            {
                return name;
            }

            return defaultValue;
        }
    }
}
