using System;
using System.IO;
using SAM.API;

#nullable enable

namespace SAM.Picker
{
    internal static class GameImageUrlResolver
    {
        internal static string? GetGameImageUrl(Func<uint, string, string?> getAppData, uint id, string language)

        {
            string? candidate;

            candidate = getAppData(id, $"small_capsule/{language}");
            if (string.IsNullOrEmpty(candidate) == false)
            {
                if (TrySanitizeCandidate(candidate, out var safeCandidate))
                {
                    return $"https://shared.cloudflare.steamstatic.com/store_item_assets/steam/apps/{id}/{safeCandidate}";
                }
                else
                {
                    DebugLogger.Log($"Invalid small_capsule path for app {id} language {language}: {candidate}");
                }
            }
            else
            {
                DebugLogger.Log($"Missing small_capsule for app {id} language {language}");
            }

            if (language != "english")
            {
                candidate = getAppData(id, "small_capsule/english");
                if (string.IsNullOrEmpty(candidate) == false)
                {
                    if (TrySanitizeCandidate(candidate, out var safeCandidate))
                    {
                        return $"https://shared.cloudflare.steamstatic.com/store_item_assets/steam/apps/{id}/{safeCandidate}";
                    }
                    else
                    {
                        DebugLogger.Log($"Invalid small_capsule path for app {id} language english: {candidate}");
                    }
                }
                else
                {
                    DebugLogger.Log($"Missing small_capsule for app {id} language english");
                }
            }

            candidate = getAppData(id, "logo");
            if (string.IsNullOrEmpty(candidate) == false)
            {
                if (TrySanitizeCandidate(candidate, out var safeCandidate))
                {
                    return $"https://cdn.steamstatic.com/steamcommunity/public/images/apps/{id}/{safeCandidate}.jpg";
                }
                else
                {
                    DebugLogger.Log($"Invalid logo path for app {id}: {candidate}");
                }
            }
            else
            {
                DebugLogger.Log($"Missing logo for app {id}");
            }

            candidate = getAppData(id, "library_600x900");
            if (string.IsNullOrEmpty(candidate) == false)
            {
                if (TrySanitizeCandidate(candidate, out var safeCandidate))
                {
                    return $"https://shared.cloudflare.steamstatic.com/steam/apps/{id}/{safeCandidate}";
                }
                else
                {
                    DebugLogger.Log($"Invalid library_600x900 path for app {id}: {candidate}");
                }
            }
            else
            {
                DebugLogger.Log($"Missing library_600x900 for app {id}");
            }

            candidate = getAppData(id, "header_image");
            if (string.IsNullOrEmpty(candidate) == false)
            {
                if (TrySanitizeCandidate(candidate, out var safeCandidate))
                {
                    return $"https://shared.cloudflare.steamstatic.com/steam/apps/{id}/{safeCandidate}";
                }
                else
                {
                    DebugLogger.Log($"Invalid header_image path for app {id}: {candidate}");
                }
            }
            else
            {
                DebugLogger.Log($"Missing header_image for app {id}");
            }

            return null;
        }

        internal static bool TrySanitizeCandidate(string candidate, out string sanitized)
        {
            sanitized = Path.GetFileName(candidate);

            if (candidate.IndexOf("..", StringComparison.Ordinal) >= 0 ||
                candidate.IndexOf(':') >= 0)
            {
                return false;
            }

            if (Uri.TryCreate(candidate, UriKind.Absolute, out var uri) && string.IsNullOrEmpty(uri.Scheme) == false)
            {
                return false;
            }

            return true;
        }
    }
}
