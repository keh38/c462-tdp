namespace TapDevPlatform
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            statusStrip = new StatusStrip();
            connectionStatusLabel = new ToolStripStatusLabel();
            matlabStatusLabel = new ToolStripStatusLabel();
            sceneNameLabel = new ToolStripStatusLabel();
            tabControl1 = new TabControl();
            elementsPage = new TabPage();
            patternsPage = new TabPage();
            propertyGrid = new PropertyGrid();
            comboBox1 = new ComboBox();
            richTextBox1 = new RichTextBox();
            textBox1 = new TextBox();
            listBox1 = new ListBox();
            comboBox2 = new ComboBox();
            comboBox3 = new ComboBox();
            button1 = new Button();
            statusStrip.SuspendLayout();
            tabControl1.SuspendLayout();
            elementsPage.SuspendLayout();
            patternsPage.SuspendLayout();
            SuspendLayout();
            // 
            // statusStrip
            // 
            statusStrip.ImageScalingSize = new Size(20, 20);
            statusStrip.Items.AddRange(new ToolStripItem[] { connectionStatusLabel, matlabStatusLabel, sceneNameLabel });
            statusStrip.Location = new Point(0, 527);
            statusStrip.Name = "statusStrip";
            statusStrip.Size = new Size(987, 30);
            statusStrip.TabIndex = 0;
            statusStrip.Text = "statusStrip1";
            // 
            // connectionStatusLabel
            // 
            connectionStatusLabel.BorderSides = ToolStripStatusLabelBorderSides.Right;
            connectionStatusLabel.Name = "connectionStatusLabel";
            connectionStatusLabel.Size = new Size(111, 24);
            connectionStatusLabel.Text = "Not connected";
            // 
            // matlabStatusLabel
            // 
            matlabStatusLabel.BorderSides = ToolStripStatusLabelBorderSides.Right;
            matlabStatusLabel.Image = Properties.Resources.Matlab_Logo_32;
            matlabStatusLabel.Name = "matlabStatusLabel";
            matlabStatusLabel.Size = new Size(153, 24);
            matlabStatusLabel.Text = "MATLAB available";
            // 
            // sceneNameLabel
            // 
            sceneNameLabel.BorderSides = ToolStripStatusLabelBorderSides.Right;
            sceneNameLabel.Name = "sceneNameLabel";
            sceneNameLabel.Size = new Size(55, 24);
            sceneNameLabel.Text = "Scene:";
            // 
            // tabControl1
            // 
            tabControl1.Controls.Add(elementsPage);
            tabControl1.Controls.Add(patternsPage);
            tabControl1.Dock = DockStyle.Fill;
            tabControl1.Location = new Point(0, 0);
            tabControl1.Name = "tabControl1";
            tabControl1.SelectedIndex = 0;
            tabControl1.Size = new Size(987, 527);
            tabControl1.TabIndex = 1;
            // 
            // elementsPage
            // 
            elementsPage.Controls.Add(comboBox1);
            elementsPage.Controls.Add(propertyGrid);
            elementsPage.Location = new Point(4, 29);
            elementsPage.Name = "elementsPage";
            elementsPage.Padding = new Padding(3);
            elementsPage.Size = new Size(979, 494);
            elementsPage.TabIndex = 0;
            elementsPage.Text = "Elements";
            elementsPage.UseVisualStyleBackColor = true;
            // 
            // patternsPage
            // 
            patternsPage.Controls.Add(button1);
            patternsPage.Controls.Add(comboBox3);
            patternsPage.Controls.Add(comboBox2);
            patternsPage.Controls.Add(listBox1);
            patternsPage.Controls.Add(textBox1);
            patternsPage.Controls.Add(richTextBox1);
            patternsPage.Location = new Point(4, 29);
            patternsPage.Name = "patternsPage";
            patternsPage.Padding = new Padding(3);
            patternsPage.Size = new Size(979, 494);
            patternsPage.TabIndex = 1;
            patternsPage.Text = "Patterns";
            patternsPage.UseVisualStyleBackColor = true;
            // 
            // propertyGrid
            // 
            propertyGrid.Location = new Point(19, 58);
            propertyGrid.Name = "propertyGrid";
            propertyGrid.Size = new Size(399, 397);
            propertyGrid.TabIndex = 0;
            propertyGrid.ToolbarVisible = false;
            // 
            // comboBox1
            // 
            comboBox1.FormattingEnabled = true;
            comboBox1.Location = new Point(19, 15);
            comboBox1.Name = "comboBox1";
            comboBox1.Size = new Size(228, 28);
            comboBox1.TabIndex = 1;
            // 
            // richTextBox1
            // 
            richTextBox1.BorderStyle = BorderStyle.FixedSingle;
            richTextBox1.Location = new Point(283, 29);
            richTextBox1.Name = "richTextBox1";
            richTextBox1.ReadOnly = true;
            richTextBox1.Size = new Size(672, 273);
            richTextBox1.TabIndex = 0;
            richTextBox1.Text = "";
            // 
            // textBox1
            // 
            textBox1.BorderStyle = BorderStyle.FixedSingle;
            textBox1.Location = new Point(283, 317);
            textBox1.Multiline = true;
            textBox1.Name = "textBox1";
            textBox1.Size = new Size(672, 126);
            textBox1.TabIndex = 1;
            // 
            // listBox1
            // 
            listBox1.FormattingEnabled = true;
            listBox1.Location = new Point(19, 75);
            listBox1.Name = "listBox1";
            listBox1.Size = new Size(246, 204);
            listBox1.TabIndex = 2;
            // 
            // comboBox2
            // 
            comboBox2.FormattingEnabled = true;
            comboBox2.Location = new Point(19, 29);
            comboBox2.Name = "comboBox2";
            comboBox2.Size = new Size(246, 28);
            comboBox2.TabIndex = 3;
            // 
            // comboBox3
            // 
            comboBox3.FormattingEnabled = true;
            comboBox3.Location = new Point(19, 317);
            comboBox3.Name = "comboBox3";
            comboBox3.Size = new Size(246, 28);
            comboBox3.TabIndex = 4;
            // 
            // button1
            // 
            button1.Location = new Point(19, 368);
            button1.Name = "button1";
            button1.Size = new Size(246, 29);
            button1.TabIndex = 5;
            button1.Text = "button1";
            button1.UseVisualStyleBackColor = true;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(987, 557);
            Controls.Add(tabControl1);
            Controls.Add(statusStrip);
            Name = "MainForm";
            Text = "Tapping Pattern Development Platform";
            statusStrip.ResumeLayout(false);
            statusStrip.PerformLayout();
            tabControl1.ResumeLayout(false);
            elementsPage.ResumeLayout(false);
            patternsPage.ResumeLayout(false);
            patternsPage.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private StatusStrip statusStrip;
        private ToolStripStatusLabel connectionStatusLabel;
        private ToolStripStatusLabel matlabStatusLabel;
        private ToolStripStatusLabel sceneNameLabel;
        private TabControl tabControl1;
        private TabPage elementsPage;
        private TabPage patternsPage;
        private PropertyGrid propertyGrid;
        private ComboBox comboBox1;
        private Button button1;
        private ComboBox comboBox3;
        private ComboBox comboBox2;
        private ListBox listBox1;
        private TextBox textBox1;
        private RichTextBox richTextBox1;
    }
}
