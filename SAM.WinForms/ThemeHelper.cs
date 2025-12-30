using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SAM.WinForms
{
    /// <summary>
    /// Helper for applying theme colors to WinForms controls.
    /// </summary>
    public static class ThemeHelper
    {
        private static readonly List<(Type Type, Action<object, Color, Color> Handler)> _handlers = new();
        private static readonly ConditionalWeakTable<TabControl, ColorInfo> _tabControlColors = new();
        private static readonly ConditionalWeakTable<ListView, ColorInfo> _listViewColors = new();

        private sealed class ColorInfo
        {
            public Color Back { get; set; }
            public Color Fore { get; set; }
        }

        static ThemeHelper()
        {
            // Generic control recursion.
            RegisterHandler(typeof(Control), (o, back, fore) =>
            {
                var control = (Control)o;
                foreach (Control child in control.Controls)
                {
                    ApplyTheme(child, back, fore);
                }
            });

            // ToolStrip items.
            RegisterHandler(typeof(ToolStrip), (o, back, fore) =>
            {
                var strip = (ToolStrip)o;
                foreach (ToolStripItem item in strip.Items)
                {
                    ApplyTheme(item, back, fore);
                }
            });

            // ListView items and columns.
            RegisterHandler(typeof(ListView), (o, back, fore) =>
            {
                var list = (ListView)o;
                list.OwnerDraw = true;
                list.DrawColumnHeader -= OnListViewDrawColumnHeader;
                list.DrawColumnHeader += OnListViewDrawColumnHeader;
                list.DrawItem -= OnListViewDrawItem;
                list.DrawItem += OnListViewDrawItem;
                list.DrawSubItem -= OnListViewDrawSubItem;
                list.DrawSubItem += OnListViewDrawSubItem;
                list.Paint -= OnListViewPaint;
                list.Paint += OnListViewPaint;
                list.Disposed -= OnListViewDisposed;
                list.Disposed += OnListViewDisposed;

                var colorInfo = _listViewColors.GetValue(list, _ => new ColorInfo());
                colorInfo.Back = back;
                colorInfo.Fore = fore;

                foreach (ColumnHeader column in list.Columns)
                {
                    ApplyTheme(column, back, fore);
                }

                if (list.VirtualMode == false)
                {
                    foreach (ListViewItem item in list.Items)
                    {
                        ApplyTheme(item, back, fore);
                    }
                }

                ApplyScrollBarTheme(list, back);
            });

            // DataGridView columns and headers.
            RegisterHandler(typeof(DataGridView), (o, back, fore) =>
            {
                var grid = (DataGridView)o;
                grid.BackgroundColor = back;
                grid.DefaultCellStyle.BackColor = back;
                grid.DefaultCellStyle.ForeColor = fore;
                grid.EnableHeadersVisualStyles = false;
                grid.ColumnHeadersDefaultCellStyle.BackColor = back;
                grid.ColumnHeadersDefaultCellStyle.ForeColor = fore;
                grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = back;
                grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = fore;
                grid.RowHeadersDefaultCellStyle.BackColor = back;
                grid.RowHeadersDefaultCellStyle.ForeColor = fore;
                grid.RowHeadersDefaultCellStyle.SelectionBackColor = back;
                grid.RowHeadersDefaultCellStyle.SelectionForeColor = fore;

                foreach (DataGridViewColumn column in grid.Columns)
                {
                    ApplyTheme(column, back, fore);
                }

                grid.TopLeftHeaderCell.Style.BackColor = back;
                grid.TopLeftHeaderCell.Style.ForeColor = fore;
                grid.TopLeftHeaderCell.Style.SelectionBackColor = back;
                grid.TopLeftHeaderCell.Style.SelectionForeColor = fore;

                ApplyScrollBarTheme(grid, back);
            });

            // Drop-down items.
            RegisterHandler(typeof(ToolStripDropDownItem), (o, back, fore) =>
            {
                var dropDown = (ToolStripDropDownItem)o;
                foreach (ToolStripItem item in dropDown.DropDownItems)
                {
                    ApplyTheme(item, back, fore);
                }
            });

            // ListView sub-items.
            RegisterHandler(typeof(ListViewItem), (o, back, fore) =>
            {
                var lvi = (ListViewItem)o;
                foreach (ListViewItem.ListViewSubItem sub in lvi.SubItems)
                {
                    ApplyTheme(sub, back, fore);
                }
            });

            // DataGridView column styling.
            RegisterHandler(typeof(DataGridViewColumn), (o, back, fore) =>
            {
                var column = (DataGridViewColumn)o;
                column.DefaultCellStyle.BackColor = back;
                column.DefaultCellStyle.ForeColor = fore;
                column.HeaderCell.Style.BackColor = back;
                column.HeaderCell.Style.ForeColor = fore;
                column.HeaderCell.Style.SelectionBackColor = back;
                column.HeaderCell.Style.SelectionForeColor = fore;
            });

            // Tab control page text.
            RegisterHandler(typeof(TabControl), (o, back, fore) =>
            {
                var tabs = (TabControl)o;
                tabs.DrawMode = TabDrawMode.OwnerDrawFixed;
                tabs.DrawItem -= OnTabControlDrawItem;
                tabs.DrawItem += OnTabControlDrawItem;
                tabs.Paint -= OnTabControlPaint;
                tabs.Paint += OnTabControlPaint;
                tabs.Disposed -= OnTabControlDisposed;
                tabs.Disposed += OnTabControlDisposed;

                var colorInfo = _tabControlColors.GetValue(tabs, _ => new ColorInfo());
                colorInfo.Back = back;
                colorInfo.Fore = fore;

                ApplyScrollBarTheme(tabs, back);
            });

            // Tab pages must opt out of visual styles for custom colors.
            RegisterHandler(typeof(TabPage), (o, back, fore) =>
            {
                var page = (TabPage)o;
                page.UseVisualStyleBackColor = false;
            });
        }

        /// <summary>
        /// Registers a handler for applying theme colors to a specific type.
        /// </summary>
        public static void RegisterHandler(Type type, Action<object, Color, Color> handler)
        {
            _handlers.Add((type, handler));
        }

        /// <summary>
        /// Apply theme colors to an object and all of its children.
        /// </summary>
        public static void ApplyTheme(object? target, Color back, Color fore)
        {
            if (target == null)
            {
                return;
            }

            TrySetColor(target, back, fore);

            foreach (var (type, handler) in _handlers)
            {
                if (type.IsInstanceOfType(target))
                {
                    handler(target, back, fore);
                }
            }
        }

        private static void TrySetColor(object target, Color back, Color fore)
        {
            var type = target.GetType();
            var backProp = type.GetProperty("BackColor") ?? type.GetProperty("BackgroundColor");
            if (backProp?.CanWrite == true)
            {
                backProp.SetValue(target, back, null);
            }

            var foreProp = type.GetProperty("ForeColor");
            if (foreProp?.CanWrite == true)
            {
                foreProp.SetValue(target, fore, null);
            }
        }

        private static void ApplyScrollBarTheme(Control control, Color back)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) == false)
            {
                return;
            }

            // Determine if background is dark or light to set appropriate theme.
            bool dark = back.GetBrightness() < 0.5f;
            if (control.IsHandleCreated == false)
            {
                var handle = control.Handle; // force handle creation
            }

            int useDark = dark ? 1 : 0;
            _ = DwmSetWindowAttribute(control.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useDark, sizeof(int));
            _ = SetWindowTheme(control.Handle, dark ? "DarkMode_Explorer" : "Explorer", null);

            // For ListView, also apply theme to the header control
            if (control is ListView listView)
            {
                IntPtr headerHandle = SendMessage(listView.Handle, LVM_GETHEADER, IntPtr.Zero, IntPtr.Zero);
                if (headerHandle != IntPtr.Zero)
                {
                    _ = DwmSetWindowAttribute(headerHandle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useDark, sizeof(int));
                    _ = SetWindowTheme(headerHandle, dark ? "ItemsView" : "Explorer", null);
                }
            }
        }

        private static void OnListViewPaint(object? sender, PaintEventArgs e)
        {
            if (sender is not ListView list || list.View != View.Details || list.Columns.Count == 0)
            {
                return;
            }

            // Get stored colors for this ListView
            if (_listViewColors.TryGetValue(list, out var colorInfo))
            {
                // Calculate the total width of all columns - use long to avoid overflow
                long totalColumnsWidth = 0;
                foreach (ColumnHeader col in list.Columns)
                {
                    totalColumnsWidth += col.Width;
                }

                // Get the height of the header by checking where items start
                int headerHeight = 0;
                if (list.Items.Count > 0)
                {
                    Rectangle itemRect = list.GetItemRect(0);
                    headerHeight = itemRect.Top;
                }
                else
                {
                    // If no items, use font height as estimate
                    headerHeight = list.Font.Height + 8;
                }

                int listWidth = list.ClientRectangle.Width;
                if (totalColumnsWidth < listWidth && headerHeight > 0)
                {
                    Rectangle remainingRect = new Rectangle((int)totalColumnsWidth, 0, (int)(listWidth - totalColumnsWidth), headerHeight);
                    using var back = new SolidBrush(colorInfo.Back);
                    e.Graphics.FillRectangle(back, remainingRect);
                }
            }
        }

        private static void OnListViewDisposed(object? sender, EventArgs e)
        {
            if (sender is not ListView list)
            {
                return;
            }

            // Unregister all event handlers
            list.DrawColumnHeader -= OnListViewDrawColumnHeader;
            list.DrawItem -= OnListViewDrawItem;
            list.DrawSubItem -= OnListViewDrawSubItem;
            list.Paint -= OnListViewPaint;
            list.Disposed -= OnListViewDisposed;
        }

        private static void OnListViewDrawColumnHeader(object? sender, DrawListViewColumnHeaderEventArgs e)
        {
            if (sender is not ListView list)
            {
                return;
            }

            // Get stored colors for this ListView
            Color backColor = list.BackColor;
            Color foreColor = list.ForeColor;
            if (_listViewColors.TryGetValue(list, out var colorInfo))
            {
                backColor = colorInfo.Back;
                foreColor = colorInfo.Fore;
            }

            using var back = new SolidBrush(backColor);
            using var fore = new SolidBrush(foreColor);

            // Fill the current column header
            e.Graphics.FillRectangle(back, e.Bounds);

            // Check if this is the first column - if so, fill the entire header background first
            int headerIndex = -1;
            for (int i = 0; i < list.Columns.Count; i++)
            {
                if (list.Columns[i] == e.Header)
                {
                    headerIndex = i;
                    break;
                }
            }

            if (headerIndex == 0)
            {
                // Calculate total width of all columns - use long to avoid overflow
                long totalColumnsWidth = 0;
                foreach (ColumnHeader col in list.Columns)
                {
                    totalColumnsWidth += col.Width;
                }

                // Fill any remaining space to the right
                int listWidth = list.ClientRectangle.Width;
                if (totalColumnsWidth < listWidth)
                {
                    Rectangle remainingRect = new Rectangle((int)totalColumnsWidth, e.Bounds.Y, (int)(listWidth - totalColumnsWidth), e.Bounds.Height);
                    e.Graphics.FillRectangle(back, remainingRect);
                }
            }

            // Draw text
            var bounds = e.Bounds;
            bounds.Inflate(-2, 0);
            StringFormat format = new()
            {
                Alignment = StringAlignment.Near,
                LineAlignment = StringAlignment.Center,
            };
            var text = e.Header?.Text ?? string.Empty;
            var font = list.Font ?? SystemFonts.DefaultFont;
            e.Graphics.DrawString(text, font, fore, bounds, format);

            // Draw grid line (separator) on the right edge of the column header
            // Use a slightly darker/lighter color based on the background
            Color gridColor = backColor.GetBrightness() < 0.5f
                ? ControlPaint.Light(backColor, 0.3f)  // Dark theme: lighter grid line
                : ControlPaint.Dark(backColor, 0.1f);   // Light theme: darker grid line

            using var gridPen = new Pen(gridColor);
            e.Graphics.DrawLine(gridPen, e.Bounds.Right - 1, e.Bounds.Top, e.Bounds.Right - 1, e.Bounds.Bottom);
        }

        private static void OnListViewDrawItem(object? sender, DrawListViewItemEventArgs e)
        {
            e.DrawDefault = true;
        }

        private static void OnListViewDrawSubItem(object? sender, DrawListViewSubItemEventArgs e)
        {
            e.DrawDefault = true;
        }

        private static void OnTabControlPaint(object? sender, PaintEventArgs e)
        {
            if (sender is not TabControl tabs || tabs.TabCount == 0)
            {
                return;
            }

            // Get stored colors for this TabControl
            if (_tabControlColors.TryGetValue(tabs, out var colorInfo))
            {
                // Calculate the position after the last tab
                Rectangle lastTabRect = tabs.GetTabRect(tabs.TabCount - 1);
                int startX = lastTabRect.Right;
                int headerHeight = lastTabRect.Bottom;

                // Fill the remaining area to the right of the last tab
                if (startX < tabs.Width)
                {
                    Rectangle remainingRect = new Rectangle(startX, 0, tabs.Width - startX, headerHeight);
                    using var back = new SolidBrush(colorInfo.Back);
                    e.Graphics.FillRectangle(back, remainingRect);
                }
            }
        }

        private static void OnTabControlDisposed(object? sender, EventArgs e)
        {
            if (sender is not TabControl tabs)
            {
                return;
            }

            // Unregister all event handlers
            tabs.DrawItem -= OnTabControlDrawItem;
            tabs.Paint -= OnTabControlPaint;
            tabs.Disposed -= OnTabControlDisposed;
        }

        private static void OnTabControlDrawItem(object? sender, DrawItemEventArgs e)
        {
            if (sender is not TabControl tabs)
            {
                return;
            }

            TabPage page = tabs.TabPages[e.Index];

            // Get stored colors for this TabControl
            Color backColor = tabs.BackColor;
            Color foreColor = tabs.ForeColor;
            if (_tabControlColors.TryGetValue(tabs, out var colorInfo))
            {
                backColor = colorInfo.Back;
                foreColor = colorInfo.Fore;
            }

            using var back = new SolidBrush(backColor);
            using var fore = new SolidBrush(foreColor);
            e.Graphics.FillRectangle(back, e.Bounds);

            // If this is the last tab, also fill the remaining area to the right
            if (e.Index == tabs.TabCount - 1)
            {
                int startX = e.Bounds.Right;
                int headerHeight = e.Bounds.Bottom;
                if (startX < tabs.Width)
                {
                    Rectangle remainingRect = new Rectangle(startX, 0, tabs.Width - startX, headerHeight);
                    e.Graphics.FillRectangle(back, remainingRect);
                }
            }

            var bounds = e.Bounds;
            bounds.Inflate(-2, -2);
            StringFormat format = new()
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center,
            };
            var font = e.Font ?? SystemFonts.DefaultFont;
            e.Graphics.DrawString(page.Text ?? string.Empty, font, fore, bounds, format);
        }

        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        private const int LVM_GETHEADER = 0x1000 + 31;

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hWnd, int attr, ref int attrValue, int attrSize);

        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr hWnd, string? pszSubAppName, string? pszSubIdList);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
    }
}

