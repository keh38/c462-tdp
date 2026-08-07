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
            testApiButton = new Button();
            newChatButton = new Button();
            label2 = new Label();
            label1 = new Label();
            stopButton = new Button();
            logTextBox = new TextBox();
            dataPathTextBox = new TextBox();
            dataGridView1 = new DataGridView();
            runButton = new Button();
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
            setKeyButton = new Button();
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
            statusStrip.Location = new Point(0, 842);
            statusStrip.Name = "statusStrip";
            statusStrip.Size = new Size(1137, 30);
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
            // subjectStatusLabel
            // 
            subjectStatusLabel.BorderSides = ToolStripStatusLabelBorderSides.Right;
            subjectStatusLabel.Name = "subjectStatusLabel";
            subjectStatusLabel.Size = new Size(65, 24);
            subjectStatusLabel.Text = "Subject:";
            // 
            // sceneNameLabel
            // 
            sceneNameLabel.BorderSides = ToolStripStatusLabelBorderSides.Right;
            sceneNameLabel.Name = "sceneNameLabel";
            sceneNameLabel.Size = new Size(55, 24);
            sceneNameLabel.Text = "Scene:";
            // 
            // tabControl
            // 
            tabControl.Controls.Add(patternsPage);
            tabControl.Controls.Add(elementsPage);
            tabControl.Dock = DockStyle.Fill;
            tabControl.Location = new Point(0, 0);
            tabControl.Name = "tabControl";
            tabControl.SelectedIndex = 0;
            tabControl.Size = new Size(1137, 842);
            tabControl.TabIndex = 1;
            // 
            // patternsPage
            // 
            patternsPage.Controls.Add(setKeyButton);
            patternsPage.Controls.Add(testApiButton);
            patternsPage.Controls.Add(newChatButton);
            patternsPage.Controls.Add(label2);
            patternsPage.Controls.Add(label1);
            patternsPage.Controls.Add(stopButton);
            patternsPage.Controls.Add(logTextBox);
            patternsPage.Controls.Add(dataPathTextBox);
            patternsPage.Controls.Add(dataGridView1);
            patternsPage.Controls.Add(runButton);
            patternsPage.Controls.Add(matlabFunctionDropDown);
            patternsPage.Controls.Add(chatListBox);
            patternsPage.Controls.Add(textBox1);
            patternsPage.Controls.Add(richTextBox1);
            patternsPage.Location = new Point(4, 29);
            patternsPage.Name = "patternsPage";
            patternsPage.Padding = new Padding(3);
            patternsPage.Size = new Size(1129, 809);
            patternsPage.TabIndex = 1;
            patternsPage.Text = "Patterns";
            patternsPage.UseVisualStyleBackColor = true;
            // 
            // testApiButton
            // 
            testApiButton.Location = new Point(150, 23);
            testApiButton.Name = "testApiButton";
            testApiButton.Size = new Size(94, 29);
            testApiButton.TabIndex = 13;
            testApiButton.Text = "Test";
            testApiButton.UseVisualStyleBackColor = true;
            testApiButton.Click += testApiButton_Click;
            // 
            // newChatButton
            // 
            newChatButton.Location = new Point(263, 25);
            newChatButton.Name = "newChatButton";
            newChatButton.Size = new Size(94, 29);
            newChatButton.TabIndex = 12;
            newChatButton.Text = "New";
            newChatButton.UseVisualStyleBackColor = true;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(19, 32);
            label2.Name = "label2";
            label2.Size = new Size(87, 20);
            label2.TabIndex = 11;
            label2.Text = "Chat history";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(19, 391);
            label1.Name = "label1";
            label1.Size = new Size(178, 20);
            label1.TabIndex = 10;
            label1.Text = "MATLAB analysis function";
            // 
            // stopButton
            // 
            stopButton.Location = new Point(259, 483);
            stopButton.Name = "stopButton";
            stopButton.Size = new Size(98, 29);
            stopButton.TabIndex = 9;
            stopButton.Text = "STOP";
            stopButton.UseVisualStyleBackColor = true;
            stopButton.Click += StopButton_Click;
            // 
            // logTextBox
            // 
            logTextBox.Location = new Point(19, 557);
            logTextBox.Margin = new Padding(3, 4, 3, 4);
            logTextBox.Multiline = true;
            logTextBox.Name = "logTextBox";
            logTextBox.ReadOnly = true;
            logTextBox.ScrollBars = ScrollBars.Vertical;
            logTextBox.Size = new Size(338, 223);
            logTextBox.TabIndex = 8;
            // 
            // dataPathTextBox
            // 
            dataPathTextBox.Location = new Point(19, 519);
            dataPathTextBox.Margin = new Padding(3, 4, 3, 4);
            dataPathTextBox.Name = "dataPathTextBox";
            dataPathTextBox.ReadOnly = true;
            dataPathTextBox.Size = new Size(338, 27);
            dataPathTextBox.TabIndex = 7;
            // 
            // dataGridView1
            // 
            dataGridView1.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView1.Location = new Point(383, 479);
            dataGridView1.Margin = new Padding(3, 4, 3, 4);
            dataGridView1.Name = "dataGridView1";
            dataGridView1.RowHeadersWidth = 51;
            dataGridView1.Size = new Size(729, 301);
            dataGridView1.TabIndex = 6;
            // 
            // runButton
            // 
            runButton.Location = new Point(19, 483);
            runButton.Name = "runButton";
            runButton.Size = new Size(98, 29);
            runButton.TabIndex = 5;
            runButton.Text = "RUN";
            runButton.UseVisualStyleBackColor = true;
            runButton.Click += RunButton_Click;
            // 
            // matlabFunctionDropDown
            // 
            matlabFunctionDropDown.FormattingEnabled = true;
            matlabFunctionDropDown.Location = new Point(19, 413);
            matlabFunctionDropDown.Name = "matlabFunctionDropDown";
            matlabFunctionDropDown.Size = new Size(338, 28);
            matlabFunctionDropDown.TabIndex = 4;
            matlabFunctionDropDown.SelectedIndexChanged += matlabFunctionDropDown_SelectedIndexChanged;
            // 
            // chatListBox
            // 
            chatListBox.FormattingEnabled = true;
            chatListBox.Location = new Point(19, 60);
            chatListBox.Name = "chatListBox";
            chatListBox.Size = new Size(338, 304);
            chatListBox.TabIndex = 2;
            // 
            // textBox1
            // 
            textBox1.BorderStyle = BorderStyle.FixedSingle;
            textBox1.Location = new Point(383, 313);
            textBox1.Multiline = true;
            textBox1.Name = "textBox1";
            textBox1.ScrollBars = ScrollBars.Vertical;
            textBox1.Size = new Size(726, 126);
            textBox1.TabIndex = 1;
            // 
            // richTextBox1
            // 
            richTextBox1.BorderStyle = BorderStyle.FixedSingle;
            richTextBox1.Location = new Point(383, 25);
            richTextBox1.Name = "richTextBox1";
            richTextBox1.ReadOnly = true;
            richTextBox1.ScrollBars = RichTextBoxScrollBars.Vertical;
            richTextBox1.Size = new Size(725, 273);
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
            elementsPage.Location = new Point(4, 29);
            elementsPage.Name = "elementsPage";
            elementsPage.Padding = new Padding(3);
            elementsPage.Size = new Size(1129, 809);
            elementsPage.TabIndex = 0;
            elementsPage.Text = "Elements";
            // 
            // errorTextBox
            // 
            errorTextBox.ForeColor = Color.Firebrick;
            errorTextBox.Location = new Point(465, 531);
            errorTextBox.Margin = new Padding(3, 4, 3, 4);
            errorTextBox.Multiline = true;
            errorTextBox.Name = "errorTextBox";
            errorTextBox.ReadOnly = true;
            errorTextBox.Size = new Size(476, 196);
            errorTextBox.TabIndex = 6;
            // 
            // NewButton
            // 
            NewButton.Location = new Point(202, 15);
            NewButton.Margin = new Padding(3, 4, 3, 4);
            NewButton.Name = "NewButton";
            NewButton.Size = new Size(67, 31);
            NewButton.TabIndex = 5;
            NewButton.Text = "New";
            NewButton.UseVisualStyleBackColor = true;
            NewButton.Click += NewButton_Click;
            // 
            // DeleteButton
            // 
            DeleteButton.Location = new Point(351, 15);
            DeleteButton.Margin = new Padding(3, 4, 3, 4);
            DeleteButton.Name = "DeleteButton";
            DeleteButton.Size = new Size(67, 31);
            DeleteButton.TabIndex = 4;
            DeleteButton.Text = "Delete";
            DeleteButton.UseVisualStyleBackColor = true;
            DeleteButton.Click += DeleteButton_Click;
            // 
            // SaveButton
            // 
            SaveButton.Location = new Point(277, 15);
            SaveButton.Margin = new Padding(3, 4, 3, 4);
            SaveButton.Name = "SaveButton";
            SaveButton.Size = new Size(67, 31);
            SaveButton.TabIndex = 3;
            SaveButton.Text = "Save";
            SaveButton.UseVisualStyleBackColor = true;
            SaveButton.Click += SaveButton_Click;
            // 
            // signalGraph
            // 
            signalGraph.BackColor = SystemColors.Control;
            signalGraph.Location = new Point(425, 59);
            signalGraph.Margin = new Padding(3, 4, 3, 4);
            signalGraph.Name = "signalGraph";
            signalGraph.Size = new Size(534, 436);
            signalGraph.TabIndex = 2;
            // 
            // configFileDropDown
            // 
            configFileDropDown.DropDownStyle = ComboBoxStyle.DropDownList;
            configFileDropDown.FormattingEnabled = true;
            configFileDropDown.Location = new Point(19, 15);
            configFileDropDown.Name = "configFileDropDown";
            configFileDropDown.Size = new Size(158, 28);
            configFileDropDown.TabIndex = 1;
            configFileDropDown.SelectedIndexChanged += configFileDropDown_SelectedIndexChanged;
            // 
            // propertyGrid
            // 
            propertyGrid.Location = new Point(19, 59);
            propertyGrid.Name = "propertyGrid";
            propertyGrid.Size = new Size(399, 697);
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
            // setKeyButton
            // 
            setKeyButton.Location = new Point(138, 460);
            setKeyButton.Name = "setKeyButton";
            setKeyButton.Size = new Size(94, 29);
            setKeyButton.TabIndex = 14;
            setKeyButton.Text = "Set key";
            setKeyButton.UseVisualStyleBackColor = true;
            setKeyButton.Click += setKeyButton_Click;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1137, 872);
            Controls.Add(tabControl);
            Controls.Add(statusStrip);
            FormBorderStyle = FormBorderStyle.Fixed3D;
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
        private Button runButton;
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
        private Button stopButton;
        private Label label1;
        private Button newChatButton;
        private Label label2;
        private Button testApiButton;
        private Button setKeyButton;
    }
}
