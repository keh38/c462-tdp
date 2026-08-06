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
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            statusStrip = new StatusStrip();
            connectionStatusLabel = new ToolStripStatusLabel();
            matlabStatusLabel = new ToolStripStatusLabel();
            subjectStatusLabel = new ToolStripStatusLabel();
            sceneNameLabel = new ToolStripStatusLabel();
            tabControl = new TabControl();
            patternsPage = new TabPage();
            dataGridView1 = new DataGridView();
            RunButton = new Button();
            matlabFunctionDropDown = new ComboBox();
            chatListBox = new ListBox();
            textBox1 = new TextBox();
            richTextBox1 = new RichTextBox();
            elementsPage = new TabPage();
            errorTextBox = new TextBox();
            NewButton = new Button();
            DeleteButton = new Button();
            SaveButton = new Button();
            signalGraph = new ScottPlot.WinForms.FormsPlot();
            configFileDropDown = new ComboBox();
            propertyGrid = new PropertyGrid();
            imageList = new ImageList(components);
            dataPathTextBox = new TextBox();
            logTextBox = new TextBox();
            StopButton = new Button();
            label1 = new Label();
            statusStrip.SuspendLayout();
            tabControl.SuspendLayout();
            patternsPage.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).BeginInit();
            elementsPage.SuspendLayout();
            SuspendLayout();
            // 
            // statusStrip
            // 
            statusStrip.ImageScalingSize = new Size(20, 20);
            statusStrip.Items.AddRange(new ToolStripItem[] { connectionStatusLabel, matlabStatusLabel, subjectStatusLabel, sceneNameLabel });
            statusStrip.Location = new Point(0, 625);
            statusStrip.Name = "statusStrip";
            statusStrip.Padding = new Padding(1, 0, 12, 0);
            statusStrip.Size = new Size(864, 29);
            statusStrip.TabIndex = 0;
            statusStrip.Text = "statusStrip1";
            // 
            // connectionStatusLabel
            // 
            connectionStatusLabel.BorderSides = ToolStripStatusLabelBorderSides.Right;
            connectionStatusLabel.Name = "connectionStatusLabel";
            connectionStatusLabel.Size = new Size(90, 24);
            connectionStatusLabel.Text = "Not connected";
            // 
            // matlabStatusLabel
            // 
            matlabStatusLabel.BorderSides = ToolStripStatusLabelBorderSides.Right;
            matlabStatusLabel.Image = Properties.Resources.Matlab_Logo_32;
            matlabStatusLabel.Name = "matlabStatusLabel";
            matlabStatusLabel.Size = new Size(125, 24);
            matlabStatusLabel.Text = "MATLAB available";
            // 
            // subjectStatusLabel
            // 
            subjectStatusLabel.BorderSides = ToolStripStatusLabelBorderSides.Right;
            subjectStatusLabel.Name = "subjectStatusLabel";
            subjectStatusLabel.Size = new Size(53, 24);
            subjectStatusLabel.Text = "Subject:";
            // 
            // sceneNameLabel
            // 
            sceneNameLabel.BorderSides = ToolStripStatusLabelBorderSides.Right;
            sceneNameLabel.Name = "sceneNameLabel";
            sceneNameLabel.Size = new Size(45, 24);
            sceneNameLabel.Text = "Scene:";
            // 
            // tabControl
            // 
            tabControl.Controls.Add(patternsPage);
            tabControl.Controls.Add(elementsPage);
            tabControl.Dock = DockStyle.Fill;
            tabControl.Location = new Point(0, 0);
            tabControl.Margin = new Padding(3, 2, 3, 2);
            tabControl.Name = "tabControl";
            tabControl.SelectedIndex = 0;
            tabControl.Size = new Size(864, 625);
            tabControl.TabIndex = 1;
            // 
            // patternsPage
            // 
            patternsPage.Controls.Add(label1);
            patternsPage.Controls.Add(StopButton);
            patternsPage.Controls.Add(logTextBox);
            patternsPage.Controls.Add(dataPathTextBox);
            patternsPage.Controls.Add(dataGridView1);
            patternsPage.Controls.Add(RunButton);
            patternsPage.Controls.Add(matlabFunctionDropDown);
            patternsPage.Controls.Add(chatListBox);
            patternsPage.Controls.Add(textBox1);
            patternsPage.Controls.Add(richTextBox1);
            patternsPage.Location = new Point(4, 24);
            patternsPage.Margin = new Padding(3, 2, 3, 2);
            patternsPage.Name = "patternsPage";
            patternsPage.Padding = new Padding(3, 2, 3, 2);
            patternsPage.Size = new Size(856, 597);
            patternsPage.TabIndex = 1;
            patternsPage.Text = "Patterns";
            patternsPage.UseVisualStyleBackColor = true;
            // 
            // dataGridView1
            // 
            dataGridView1.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView1.Location = new Point(248, 362);
            dataGridView1.Name = "dataGridView1";
            dataGridView1.Size = new Size(591, 210);
            dataGridView1.TabIndex = 6;
            // 
            // RunButton
            // 
            RunButton.Location = new Point(17, 362);
            RunButton.Margin = new Padding(3, 2, 3, 2);
            RunButton.Name = "RunButton";
            RunButton.Size = new Size(86, 22);
            RunButton.TabIndex = 5;
            RunButton.Text = "RUN";
            RunButton.UseVisualStyleBackColor = true;
            RunButton.Click += RunButton_Click;
            // 
            // matlabFunctionDropDown
            // 
            matlabFunctionDropDown.FormattingEnabled = true;
            matlabFunctionDropDown.Location = new Point(17, 310);
            matlabFunctionDropDown.Margin = new Padding(3, 2, 3, 2);
            matlabFunctionDropDown.Name = "matlabFunctionDropDown";
            matlabFunctionDropDown.Size = new Size(216, 23);
            matlabFunctionDropDown.TabIndex = 4;
            // 
            // chatListBox
            // 
            chatListBox.FormattingEnabled = true;
            chatListBox.ItemHeight = 15;
            chatListBox.Location = new Point(17, 26);
            chatListBox.Margin = new Padding(3, 2, 3, 2);
            chatListBox.Name = "chatListBox";
            chatListBox.Size = new Size(216, 259);
            chatListBox.TabIndex = 2;
            // 
            // textBox1
            // 
            textBox1.BorderStyle = BorderStyle.FixedSingle;
            textBox1.Location = new Point(248, 238);
            textBox1.Margin = new Padding(3, 2, 3, 2);
            textBox1.Multiline = true;
            textBox1.Name = "textBox1";
            textBox1.ScrollBars = ScrollBars.Vertical;
            textBox1.Size = new Size(588, 95);
            textBox1.TabIndex = 1;
            // 
            // richTextBox1
            // 
            richTextBox1.BorderStyle = BorderStyle.FixedSingle;
            richTextBox1.Location = new Point(248, 22);
            richTextBox1.Margin = new Padding(3, 2, 3, 2);
            richTextBox1.Name = "richTextBox1";
            richTextBox1.ReadOnly = true;
            richTextBox1.ScrollBars = RichTextBoxScrollBars.Vertical;
            richTextBox1.Size = new Size(588, 206);
            richTextBox1.TabIndex = 0;
            richTextBox1.Text = "";
            // 
            // elementsPage
            // 
            elementsPage.BackColor = SystemColors.Control;
            elementsPage.Controls.Add(errorTextBox);
            elementsPage.Controls.Add(NewButton);
            elementsPage.Controls.Add(DeleteButton);
            elementsPage.Controls.Add(SaveButton);
            elementsPage.Controls.Add(signalGraph);
            elementsPage.Controls.Add(configFileDropDown);
            elementsPage.Controls.Add(propertyGrid);
            elementsPage.Location = new Point(4, 24);
            elementsPage.Margin = new Padding(3, 2, 3, 2);
            elementsPage.Name = "elementsPage";
            elementsPage.Padding = new Padding(3, 2, 3, 2);
            elementsPage.Size = new Size(856, 597);
            elementsPage.TabIndex = 0;
            elementsPage.Text = "Elements";
            // 
            // errorTextBox
            // 
            errorTextBox.ForeColor = Color.Firebrick;
            errorTextBox.Location = new Point(407, 398);
            errorTextBox.Multiline = true;
            errorTextBox.Name = "errorTextBox";
            errorTextBox.ReadOnly = true;
            errorTextBox.Size = new Size(417, 148);
            errorTextBox.TabIndex = 6;
            // 
            // NewButton
            // 
            NewButton.Location = new Point(177, 11);
            NewButton.Name = "NewButton";
            NewButton.Size = new Size(59, 23);
            NewButton.TabIndex = 5;
            NewButton.Text = "New";
            NewButton.UseVisualStyleBackColor = true;
            NewButton.Click += NewButton_Click;
            // 
            // DeleteButton
            // 
            DeleteButton.Location = new Point(307, 11);
            DeleteButton.Name = "DeleteButton";
            DeleteButton.Size = new Size(59, 23);
            DeleteButton.TabIndex = 4;
            DeleteButton.Text = "Delete";
            DeleteButton.UseVisualStyleBackColor = true;
            DeleteButton.Click += DeleteButton_Click;
            // 
            // SaveButton
            // 
            SaveButton.Location = new Point(242, 11);
            SaveButton.Name = "SaveButton";
            SaveButton.Size = new Size(59, 23);
            SaveButton.TabIndex = 3;
            SaveButton.Text = "Save";
            SaveButton.UseVisualStyleBackColor = true;
            SaveButton.Click += SaveButton_Click;
            // 
            // signalGraph
            // 
            signalGraph.BackColor = SystemColors.Control;
            signalGraph.Location = new Point(372, 44);
            signalGraph.Name = "signalGraph";
            signalGraph.Size = new Size(467, 327);
            signalGraph.TabIndex = 2;
            // 
            // configFileDropDown
            // 
            configFileDropDown.DropDownStyle = ComboBoxStyle.DropDownList;
            configFileDropDown.FormattingEnabled = true;
            configFileDropDown.Location = new Point(17, 11);
            configFileDropDown.Margin = new Padding(3, 2, 3, 2);
            configFileDropDown.Name = "configFileDropDown";
            configFileDropDown.Size = new Size(139, 23);
            configFileDropDown.TabIndex = 1;
            configFileDropDown.SelectedIndexChanged += configFileDropDown_SelectedIndexChanged;
            // 
            // propertyGrid
            // 
            propertyGrid.Location = new Point(17, 44);
            propertyGrid.Margin = new Padding(3, 2, 3, 2);
            propertyGrid.Name = "propertyGrid";
            propertyGrid.Size = new Size(349, 523);
            propertyGrid.TabIndex = 0;
            propertyGrid.ToolbarVisible = false;
            propertyGrid.PropertyValueChanged += propertyGrid_PropertyValueChanged;
            // 
            // imageList
            // 
            imageList.ColorDepth = ColorDepth.Depth32Bit;
            imageList.ImageStream = (ImageListStreamer)resources.GetObject("imageList.ImageStream");
            imageList.TransparentColor = Color.Transparent;
            imageList.Images.SetKeyName(0, "nav_plain_red.png");
            imageList.Images.SetKeyName(1, "nav_plain_green.png");
            // 
            // dataPathTextBox
            // 
            dataPathTextBox.Location = new Point(17, 389);
            dataPathTextBox.Name = "dataPathTextBox";
            dataPathTextBox.ReadOnly = true;
            dataPathTextBox.Size = new Size(216, 23);
            dataPathTextBox.TabIndex = 7;
            // 
            // logTextBox
            // 
            logTextBox.Location = new Point(17, 418);
            logTextBox.Multiline = true;
            logTextBox.Name = "logTextBox";
            logTextBox.ReadOnly = true;
            logTextBox.ScrollBars = ScrollBars.Vertical;
            logTextBox.Size = new Size(216, 154);
            logTextBox.TabIndex = 8;
            // 
            // StopButton
            // 
            StopButton.Location = new Point(147, 362);
            StopButton.Margin = new Padding(3, 2, 3, 2);
            StopButton.Name = "StopButton";
            StopButton.Size = new Size(86, 22);
            StopButton.TabIndex = 9;
            StopButton.Text = "STOP";
            StopButton.UseVisualStyleBackColor = true;
            StopButton.Click += StopButton_Click;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(17, 293);
            label1.Name = "label1";
            label1.Size = new Size(144, 15);
            label1.TabIndex = 10;
            label1.Text = "MATLAB analysis function";
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(864, 654);
            Controls.Add(tabControl);
            Controls.Add(statusStrip);
            FormBorderStyle = FormBorderStyle.Fixed3D;
            Margin = new Padding(3, 2, 3, 2);
            MaximizeBox = false;
            Name = "MainForm";
            Text = "Tapping Pattern Development Platform";
            FormClosing += MainForm_FormClosing;
            Load += MainForm_Load;
            Shown += MainForm_Shown;
            statusStrip.ResumeLayout(false);
            statusStrip.PerformLayout();
            tabControl.ResumeLayout(false);
            patternsPage.ResumeLayout(false);
            patternsPage.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).EndInit();
            elementsPage.ResumeLayout(false);
            elementsPage.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private StatusStrip statusStrip;
        private ToolStripStatusLabel connectionStatusLabel;
        private ToolStripStatusLabel matlabStatusLabel;
        private ToolStripStatusLabel sceneNameLabel;
        private TabControl tabControl;
        private TabPage elementsPage;
        private TabPage patternsPage;
        private PropertyGrid propertyGrid;
        private ComboBox configFileDropDown;
        private Button RunButton;
        private ComboBox matlabFunctionDropDown;
        private ListBox chatListBox;
        private TextBox textBox1;
        private RichTextBox richTextBox1;
        private DataGridView dataGridView1;
        private ScottPlot.WinForms.FormsPlot signalGraph;
        private ImageList imageList;
        private Button DeleteButton;
        private Button SaveButton;
        private ToolStripStatusLabel subjectStatusLabel;
        private Button NewButton;
        private TextBox errorTextBox;
        private TextBox logTextBox;
        private TextBox dataPathTextBox;
        private Button StopButton;
        private Label label1;
    }
}
