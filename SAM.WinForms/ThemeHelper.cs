using System;
using System.Collections.Generic;
using System.Drawing;
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
        }

        private static void OnListViewDrawColumnHeader(object? sender, DrawListViewColumnHeaderEventArgs e)
        {
            if (sender is not ListView list)
            {
                return;
            }

            using var back = new SolidBrush(list.BackColor);
            using var fore = new SolidBrush(list.ForeColor);
            e.Graphics.FillRectangle(back, e.Bounds);
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
        }

        private static void OnListViewDrawItem(object? sender, DrawListViewItemEventArgs e)
        {
            e.DrawDefault = true;
        }

        private static void OnListViewDrawSubItem(object? sender, DrawListViewSubItemEventArgs e)
        {
            e.DrawDefault = true;
        }

        private static void OnTabControlDrawItem(object? sender, DrawItemEventArgs e)
        {
            if (sender is not TabControl tabs)
            {
                return;
            }

            TabPage page = tabs.TabPages[e.Index];
            using var back = new SolidBrush(page.BackColor);
            using var fore = new SolidBrush(page.ForeColor);
            e.Graphics.FillRectangle(back, e.Bounds);
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

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hWnd, int attr, ref int attrValue, int attrSize);

        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr hWnd, string? pszSubAppName, string? pszSubIdList);
    }
}

