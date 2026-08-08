namespace HTSController
{
    partial class MarkdownDialog
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            okButton = new Button();
            webBrowser = new WebBrowser();
            SuspendLayout();
            // 
            // okButton
            // 
            okButton.Location = new Point(380, 469);
            okButton.Margin = new Padding(3, 4, 3, 4);
            okButton.Name = "okButton";
            okButton.Size = new Size(128, 49);
            okButton.TabIndex = 0;
            okButton.Text = "OK";
            okButton.UseVisualStyleBackColor = true;
            okButton.Click += okButton_Click;
            // 
            // webBrowser
            // 
            webBrowser.Location = new Point(12, 13);
            webBrowser.Margin = new Padding(3, 4, 3, 4);
            webBrowser.MinimumSize = new Size(20, 25);
            webBrowser.Name = "webBrowser";
            webBrowser.Size = new Size(893, 435);
            webBrowser.TabIndex = 2;
            // 
            // MarkdownDialog
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(917, 541);
            Controls.Add(webBrowser);
            Controls.Add(okButton);
            Margin = new Padding(3, 4, 3, 4);
            Name = "MarkdownDialog";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Help";
            ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.WebBrowser webBrowser;
    }
}