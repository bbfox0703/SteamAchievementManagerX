#nullable enable

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using SAM.Game.Stats;

namespace SAM.Game.Presenters
{
    /// <summary>
    /// Handles achievement ListView population and presentation logic.
    /// </summary>
    internal static class AchievementListPresenter
    {
        /// <summary>
        /// Populates a ListView with achievement data using proper styling and sorting.
        /// </summary>
        /// <param name="listView">The target ListView control</param>
        /// <param name="achievements">Achievement data to display</param>
        /// <param name="backColor">Background color for items</param>
        /// <param name="foreColor">Foreground color for items</param>
        /// <param name="sortColumn">Column index to sort by</param>
        /// <param name="sortOrder">Sort order (ascending/descending)</param>
        /// <param name="queueIconCallback">Callback to queue icon downloads</param>
        public static void PopulateListView(
            ListView listView,
            List<AchievementInfo> achievements,
            Color backColor,
            Color foreColor,
            int sortColumn,
            SortOrder sortOrder,
            Action<AchievementInfo> queueIconCallback)
        {
            listView.Items.Clear();
            listView.BeginUpdate();

            bool light = WinForms.WindowsThemeDetector.IsLightTheme();

            foreach (var info in achievements)
            {
                ListViewItem item = new()
                {
                    Checked = info.IsAchieved,
                    Tag = info,
                    Text = info.Name,
                    BackColor = (info.Permission & 3) == 0
                        ? backColor
                        : (light
                            ? ControlPaint.Light(backColor)
                            : ControlPaint.Dark(backColor)),
                    ForeColor = foreColor,
                };

                info.Item = item;

                // Handle unlocalized achievement names (starting with #)
                if (item.Text.StartsWith("#", StringComparison.InvariantCulture))
                {
                    item.Text = info.Id;
                    item.SubItems.Add("");
                }
                else
                {
                    item.SubItems.Add(info.Description);
                }

                // Add unlock time column
                item.SubItems.Add(info.UnlockTime.HasValue
                    ? info.UnlockTime.Value.ToString()
                    : "");

                // Add achievement ID and timer columns
                item.SubItems.Add(info.Id);
                item.SubItems.Add("-1");

                // Apply colors to all subitems
                foreach (ListViewItem.ListViewSubItem subItem in item.SubItems)
                {
                    subItem.BackColor = item.BackColor;
                    subItem.ForeColor = item.ForeColor;
                }

                info.ImageIndex = 0;

                // Queue icon download
                queueIconCallback(info);
                listView.Items.Add(item);
            }

            // Sort using the specified column/order
            listView.ListViewItemSorter = new ListViewItemComparer(sortColumn, sortOrder);
            listView.Sort();
            listView.EndUpdate();
        }

        /// <summary>
        /// Comparer for sorting ListView items by column.
        /// </summary>
        public class ListViewItemComparer : System.Collections.IComparer
        {
            private readonly int col;
            private readonly SortOrder order;

            public ListViewItemComparer(int column, SortOrder order)
            {
                col = column;
                this.order = order;
            }

            public int Compare(object? x, object? y)
            {
                if (x is not ListViewItem itemX || y is not ListViewItem itemY)
                {
                    return 0;
                }

                var s1 = itemX.SubItems[col].Text;
                var s2 = itemY.SubItems[col].Text;

                int result;

                // Try to parse as DateTime for date comparison (unlock time column)
                if (DateTime.TryParse(s1, out DateTime d1) && DateTime.TryParse(s2, out DateTime d2))
                {
                    result = DateTime.Compare(d1, d2);
                }
                else
                {
                    // Default to string comparison
                    result = string.Compare(s1, s2, StringComparison.InvariantCulture);
                }

                return (order == SortOrder.Ascending) ? result : -result;
            }
        }
    }
}
