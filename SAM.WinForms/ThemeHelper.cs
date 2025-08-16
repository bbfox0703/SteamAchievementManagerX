using System;
using System.Collections.Generic;
using System.Drawing;
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

            // ListView items.
            RegisterHandler(typeof(ListView), (o, back, fore) =>
            {
                var list = (ListView)o;
                if (list.VirtualMode)
                {
                    return;
                }

                foreach (ListViewItem item in list.Items)
                {
                    ApplyTheme(item, back, fore);
                }
            });

            // DataGridView columns.
            RegisterHandler(typeof(DataGridView), (o, back, fore) =>
            {
                var grid = (DataGridView)o;
                foreach (DataGridViewColumn column in grid.Columns)
                {
                    ApplyTheme(column, back, fore);
                }
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
    }
}

