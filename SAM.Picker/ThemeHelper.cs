using System.Drawing;
using System.Windows.Forms;

namespace SAM.Picker
{
    internal static class ThemeHelper
    {
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

        internal static void ApplyTheme(object target, Color back, Color fore)
        {
            if (target == null)
            {
                return;
            }

            TrySetColor(target, back, fore);

            switch (target)
            {
                case Control control:
                    foreach (Control child in control.Controls)
                    {
                        ApplyTheme(child, back, fore);
                    }

                    if (control is ToolStrip toolStrip)
                    {
                        foreach (ToolStripItem item in toolStrip.Items)
                        {
                            ApplyTheme(item, back, fore);
                        }
                    }
                    else if (control is ListView listView)
                    {
                        foreach (ListViewItem item in listView.Items)
                        {
                            ApplyTheme(item, back, fore);
                        }
                    }
                    else if (control is DataGridView grid)
                    {
                        foreach (DataGridViewColumn column in grid.Columns)
                        {
                            ApplyTheme(column, back, fore);
                        }
                    }
                    break;

                case ToolStripDropDownItem dropDown:
                    foreach (ToolStripItem item in dropDown.DropDownItems)
                    {
                        ApplyTheme(item, back, fore);
                    }
                    break;

                case ListViewItem listViewItem:
                    foreach (ListViewItem.ListViewSubItem sub in listViewItem.SubItems)
                    {
                        ApplyTheme(sub, back, fore);
                    }
                    break;

                case DataGridViewColumn column:
                    column.DefaultCellStyle.BackColor = back;
                    column.DefaultCellStyle.ForeColor = fore;
                    break;
            }
        }
    }
}
