using System;
using System.Diagnostics;
using System.IO;

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
                    Debug.WriteLine($"Invalid small_capsule path for app {id} language {language}: {candidate}");
                }
            }
            else
            {
                Debug.WriteLine($"Missing small_capsule for app {id} language {language}");
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
                        Debug.WriteLine($"Invalid small_capsule path for app {id} language english: {candidate}");
                    }
                }
                else
                {
                    Debug.WriteLine($"Missing small_capsule for app {id} language english");
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
                    Debug.WriteLine($"Invalid logo path for app {id}: {candidate}");
                }
            }
            else
            {
                Debug.WriteLine($"Missing logo for app {id}");
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
                    Debug.WriteLine($"Invalid library_600x900 path for app {id}: {candidate}");
                }
            }
            else
            {
                Debug.WriteLine($"Missing library_600x900 for app {id}");
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
                    Debug.WriteLine($"Invalid header_image path for app {id}: {candidate}");
                }
            }
            else
            {
                Debug.WriteLine($"Missing header_image for app {id}");
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
