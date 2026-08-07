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
            sendButton = new Button();
            generateButton = new Button();
            newChatButton = new Button();
            label2 = new Label();
            label1 = new Label();
            stopButton = new Button();
            logTextBox = new TextBox();
            dataPathTextBox = new TextBox();
            trialsDataGridView = new DataGridView();
            runButton = new Button();
            matlabFunctionDropDown = new ComboBox();
            chatListBox = new ListBox();
            inputTextBox = new TextBox();
            transcriptRichTextBox = new RichTextBox();
            elementsPage = new TabPage();
            errorTextBox = new TextBox();
            NewButton = new Button();
            DeleteButton = new Button();
            SaveButton = new Button();
            signalGraph = new ScottPlot.WinForms.FormsPlot();
            configFileDropDown = new ComboBox();
            propertyGrid = new PropertyGrid();
            imageList = new ImageList(components);
            statusStrip.SuspendLayout();
            tabControl.SuspendLayout();
            patternsPage.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)trialsDataGridView).BeginInit();
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
            statusStrip.Size = new Size(995, 29);
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
            tabControl.Size = new Size(995, 625);
            tabControl.TabIndex = 1;
            // 
            // patternsPage
            // 
            patternsPage.Controls.Add(sendButton);
            patternsPage.Controls.Add(generateButton);
            patternsPage.Controls.Add(newChatButton);
            patternsPage.Controls.Add(label2);
            patternsPage.Controls.Add(label1);
            patternsPage.Controls.Add(stopButton);
            patternsPage.Controls.Add(logTextBox);
            patternsPage.Controls.Add(dataPathTextBox);
            patternsPage.Controls.Add(trialsDataGridView);
            patternsPage.Controls.Add(runButton);
            patternsPage.Controls.Add(matlabFunctionDropDown);
            patternsPage.Controls.Add(chatListBox);
            patternsPage.Controls.Add(inputTextBox);
            patternsPage.Controls.Add(transcriptRichTextBox);
            patternsPage.Location = new Point(4, 24);
            patternsPage.Margin = new Padding(3, 2, 3, 2);
            patternsPage.Name = "patternsPage";
            patternsPage.Padding = new Padding(3, 2, 3, 2);
            patternsPage.Size = new Size(987, 597);
            patternsPage.TabIndex = 1;
            patternsPage.Text = "Patterns";
            patternsPage.UseVisualStyleBackColor = true;
            // 
            // sendButton
            // 
            sendButton.Location = new Point(875, 334);
            sendButton.Margin = new Padding(3, 2, 3, 2);
            sendButton.Name = "sendButton";
            sendButton.Size = new Size(82, 22);
            sendButton.TabIndex = 16;
            sendButton.Text = "Send";
            sendButton.UseVisualStyleBackColor = true;
            // 
            // generateButton
            // 
            generateButton.Location = new Point(335, 353);
            generateButton.Margin = new Padding(3, 2, 3, 2);
            generateButton.Name = "generateButton";
            generateButton.Size = new Size(82, 22);
            generateButton.TabIndex = 15;
            generateButton.Text = "Generate";
            generateButton.UseVisualStyleBackColor = true;
            // 
            // newChatButton
            // 
            newChatButton.Location = new Point(230, 19);
            newChatButton.Margin = new Padding(3, 2, 3, 2);
            newChatButton.Name = "newChatButton";
            newChatButton.Size = new Size(82, 22);
            newChatButton.TabIndex = 12;
            newChatButton.Text = "New";
            newChatButton.UseVisualStyleBackColor = true;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(17, 24);
            label2.Name = "label2";
            label2.Size = new Size(71, 15);
            label2.TabIndex = 11;
            label2.Text = "Chat history";
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
            // stopButton
            // 
            stopButton.Location = new Point(227, 362);
            stopButton.Margin = new Padding(3, 2, 3, 2);
            stopButton.Name = "stopButton";
            stopButton.Size = new Size(86, 22);
            stopButton.TabIndex = 9;
            stopButton.Text = "STOP";
            stopButton.UseVisualStyleBackColor = true;
            stopButton.Click += StopButton_Click;
            // 
            // logTextBox
            // 
            logTextBox.Location = new Point(17, 418);
            logTextBox.Multiline = true;
            logTextBox.Name = "logTextBox";
            logTextBox.ReadOnly = true;
            logTextBox.ScrollBars = ScrollBars.Vertical;
            logTextBox.Size = new Size(296, 168);
            logTextBox.TabIndex = 8;
            // 
            // dataPathTextBox
            // 
            dataPathTextBox.Location = new Point(17, 389);
            dataPathTextBox.Name = "dataPathTextBox";
            dataPathTextBox.ReadOnly = true;
            dataPathTextBox.Size = new Size(296, 23);
            dataPathTextBox.TabIndex = 7;
            // 
            // trialsDataGridView
            // 
            trialsDataGridView.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            trialsDataGridView.Location = new Point(335, 380);
            trialsDataGridView.Name = "trialsDataGridView";
            trialsDataGridView.RowHeadersWidth = 51;
            trialsDataGridView.Size = new Size(638, 205);
            trialsDataGridView.TabIndex = 6;
            // 
            // runButton
            // 
            runButton.Location = new Point(17, 362);
            runButton.Margin = new Padding(3, 2, 3, 2);
            runButton.Name = "runButton";
            runButton.Size = new Size(86, 22);
            runButton.TabIndex = 5;
            runButton.Text = "RUN";
            runButton.UseVisualStyleBackColor = true;
            runButton.Click += RunButton_Click;
            // 
            // matlabFunctionDropDown
            // 
            matlabFunctionDropDown.FormattingEnabled = true;
            matlabFunctionDropDown.Location = new Point(17, 310);
            matlabFunctionDropDown.Margin = new Padding(3, 2, 3, 2);
            matlabFunctionDropDown.Name = "matlabFunctionDropDown";
            matlabFunctionDropDown.Size = new Size(296, 23);
            matlabFunctionDropDown.TabIndex = 4;
            matlabFunctionDropDown.SelectedIndexChanged += matlabFunctionDropDown_SelectedIndexChanged;
            // 
            // chatListBox
            // 
            chatListBox.FormattingEnabled = true;
            chatListBox.ItemHeight = 15;
            chatListBox.Location = new Point(17, 45);
            chatListBox.Margin = new Padding(3, 2, 3, 2);
            chatListBox.Name = "chatListBox";
            chatListBox.Size = new Size(296, 229);
            chatListBox.TabIndex = 2;
            // 
            // inputTextBox
            // 
            inputTextBox.AcceptsReturn = true;
            inputTextBox.BorderStyle = BorderStyle.FixedSingle;
            inputTextBox.Location = new Point(335, 235);
            inputTextBox.Margin = new Padding(3, 2, 3, 2);
            inputTextBox.Multiline = true;
            inputTextBox.Name = "inputTextBox";
            inputTextBox.ScrollBars = ScrollBars.Vertical;
            inputTextBox.Size = new Size(636, 95);
            inputTextBox.TabIndex = 1;
            // 
            // transcriptRichTextBox
            // 
            transcriptRichTextBox.BorderStyle = BorderStyle.FixedSingle;
            transcriptRichTextBox.Location = new Point(335, 19);
            transcriptRichTextBox.Margin = new Padding(3, 2, 3, 2);
            transcriptRichTextBox.Name = "transcriptRichTextBox";
            transcriptRichTextBox.ReadOnly = true;
            transcriptRichTextBox.ScrollBars = RichTextBoxScrollBars.Vertical;
            transcriptRichTextBox.Size = new Size(635, 206);
            transcriptRichTextBox.TabIndex = 0;
            transcriptRichTextBox.Text = "";
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
            elementsPage.Size = new Size(987, 597);
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
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(995, 654);
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
            ((System.ComponentModel.ISupportInitialize)trialsDataGridView).EndInit();
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
        private TextBox inputTextBox;
        private RichTextBox transcriptRichTextBox;
        private DataGridView trialsDataGridView;
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
        private Button generateButton;
        private Button sendButton;
    }
}
