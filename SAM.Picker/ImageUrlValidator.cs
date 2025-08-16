#nullable enable

using System;
using System.Collections.Generic;

namespace SAM.Picker
{
    internal static class ImageUrlValidator
    {
        private static readonly HashSet<string> AllowedHosts = new(StringComparer.OrdinalIgnoreCase)
        {
            "shared.cloudflare.steamstatic.com",
            "shared.akamai.steamstatic.com",
            "cdn.steamstatic.com",
            "shared.steamstatic.com",
        };

        public static bool TryCreateUri(string url, out Uri? uri)
        {
            uri = null;

            if (string.IsNullOrEmpty(url) == true)
            {
                return false;
            }

            if (Uri.TryCreate(url, UriKind.Absolute, out var candidate) == false)
            {
                return false;
            }

            if (candidate.Scheme != Uri.UriSchemeHttps)
            {
                return false;
            }

            if (AllowedHosts.Contains(candidate.Host) == false)
            {
                return false;
            }

            uri = candidate;
            return true;
        }
    }
}

