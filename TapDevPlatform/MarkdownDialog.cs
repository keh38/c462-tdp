using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Markdig;

namespace HTSController
{
    public partial class MarkdownDialog : Form
    {
        public MarkdownDialog()
        {
            InitializeComponent();
        }

        public static void ShowMarkdownDialog(string markdownContent)
        {
            var dialog = new MarkdownDialog();

            // Convert the markdown to HTML
            string html = MarkdownHelper.ConvertMarkdownToHtml(markdownContent);

            dialog.webBrowser.DocumentText = html;
            dialog.ShowDialog();
        }

        public static class MarkdownHelper
        {
            public static string ConvertMarkdownToHtml(string markdownText)
            {
                // Use the Markdig pipeline to convert the markdown to HTML
                var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
                string htmlContent = Markdown.ToHtml(markdownText, pipeline);
                return htmlContent;
            }
        }

        private void okButton_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
