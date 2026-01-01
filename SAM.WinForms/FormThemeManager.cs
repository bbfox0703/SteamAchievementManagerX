#nullable enable

using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Win32;

namespace SAM.WinForms
{
    /// <summary>
    /// Manages form theme integration including Windows 11 Mica effects and color updates.
    /// </summary>
    public class FormThemeManager
    {
        private readonly Form _form;
        private Color _borderColor;

        /// <summary>
        /// Gets the current border color based on the active theme.
        /// </summary>
        public Color BorderColor => this._borderColor;

        /// <summary>
        /// Initializes a new FormThemeManager for the specified form.
        /// </summary>
        /// <param name="form">The form to manage</param>
        public FormThemeManager(Form form)
        {
            this._form = form;
            this._borderColor = Color.Gray;
        }

        /// <summary>
        /// Subscribes to Windows theme change events and performs initial setup.
        /// Call this in the form constructor after InitializeComponent().
        /// </summary>
        public void Initialize()
        {
            SystemEvents.UserPreferenceChanged += this.OnUserPreferenceChanged;

            this._form.HandleCreated += (s, e) =>
            {
                DwmWindowManager.ApplyMicaEffect(this._form.Handle, !WindowsThemeDetector.IsLightTheme());
            };

            this._form.FormClosed += (s, e) =>
            {
                SystemEvents.UserPreferenceChanged -= this.OnUserPreferenceChanged;
            };
        }

        /// <summary>
        /// Updates form colors based on current Windows theme.
        /// </summary>
        /// <param name="additionalUpdateAction">Optional callback for form-specific color updates</param>
        public void UpdateColors(Action? additionalUpdateAction = null)
        {
            bool light = WindowsThemeDetector.IsLightTheme();

            if (light)
            {
                this._borderColor = Color.FromArgb(200, 200, 200);
                this._form.BackColor = Color.White;
                this._form.ForeColor = Color.Black;
            }
            else
            {
                this._borderColor = Color.FromArgb(50, 50, 50);
                this._form.BackColor = Color.FromArgb(32, 32, 32);
                this._form.ForeColor = Color.White;
            }

            ThemeHelper.ApplyTheme(this._form, this._form.BackColor, this._form.ForeColor);

            // Allow form to perform additional updates (e.g., ListView item colors)
            additionalUpdateAction?.Invoke();

            this._form.Invalidate();
        }

        /// <summary>
        /// Applies Mica effect to the form.
        /// Call this after the form handle is created.
        /// </summary>
        public void ApplyMicaEffect()
        {
            if (this._form.IsHandleCreated)
            {
                DwmWindowManager.ApplyMicaEffect(this._form.Handle, !WindowsThemeDetector.IsLightTheme());
            }
        }

        private void OnUserPreferenceChanged(object? sender, UserPreferenceChangedEventArgs e)
        {
            if (this._form.IsHandleCreated)
            {
                this._form.BeginInvoke(new MethodInvoker(() =>
                {
                    this.UpdateColors();
                    this.ApplyMicaEffect();
                }));
            }
        }
    }
}
