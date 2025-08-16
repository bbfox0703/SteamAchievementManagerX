namespace SAM.ModernPicker
{
    partial class ModernPickerForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.TextBox appIdTextBox;
        private System.Windows.Forms.Button launchButton;
        private System.Windows.Forms.Label label1;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.appIdTextBox = new System.Windows.Forms.TextBox();
            this.launchButton = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // appIdTextBox
            // 
            this.appIdTextBox.Location = new System.Drawing.Point(64, 12);
            this.appIdTextBox.Name = "appIdTextBox";
            this.appIdTextBox.Size = new System.Drawing.Size(150, 23);
            this.appIdTextBox.TabIndex = 1;
            // 
            // launchButton
            // 
            this.launchButton.Location = new System.Drawing.Point(220, 11);
            this.launchButton.Name = "launchButton";
            this.launchButton.Size = new System.Drawing.Size(75, 25);
            this.launchButton.TabIndex = 2;
            this.launchButton.Text = "Launch";
            this.launchButton.UseVisualStyleBackColor = true;
            this.launchButton.Click += new System.EventHandler(this.LaunchButton_Click);
            //
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 15);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(46, 15);
            this.label1.TabIndex = 0;
            this.label1.Text = "App ID:";
            // 
            // ModernPickerForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(307, 48);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.launchButton);
            this.Controls.Add(this.appIdTextBox);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ModernPickerForm";
            this.Text = "SAM Modern Picker";
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
