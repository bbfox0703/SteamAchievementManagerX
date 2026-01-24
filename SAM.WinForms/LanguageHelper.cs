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

using System.Windows.Forms;
using SAM.API.Wrappers;

namespace SAM.WinForms
{
    /// <summary>
    /// Helper class for retrieving the current language selection from UI or Steam API.
    /// </summary>
    public static class LanguageHelper
    {
        /// <summary>
        /// Gets the current language from the combo box selection or falls back to Steam's current game language.
        /// </summary>
        /// <param name="comboBox">The language selection combo box.</param>
        /// <param name="steamApps">The Steam Apps API wrapper for fallback language detection.</param>
        /// <returns>The selected or detected language string.</returns>
        public static string GetCurrentLanguage(ToolStripComboBox comboBox, SteamApps008 steamApps)
        {
            if (comboBox.SelectedItem is string selectedLanguage && !string.IsNullOrEmpty(selectedLanguage))
            {
                return selectedLanguage;
            }

            return steamApps.GetCurrentGameLanguage();
        }
    }
}
