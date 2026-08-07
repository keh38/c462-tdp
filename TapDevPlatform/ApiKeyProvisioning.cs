using System.Windows.Forms;
using TDP.Security;

public static class ApiKeyProvisioning
{
    public const string Target = "TDP:AnthropicApiKey";

    public static void PromptAndStore(IWin32Window owner)
    {
        using var dlg = new Form
        {
            Width = 460,
            Height = 150,
            Text = "Set Anthropic API Key",
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterParent,
            MinimizeBox = false,
            MaximizeBox = false
        };
        var box = new TextBox { Left = 12, Top = 18, Width = 420, UseSystemPasswordChar = true };
        var ok = new Button { Text = "Store", Left = 276, Width = 75, Top = 60, DialogResult = DialogResult.OK };
        var cancel = new Button { Text = "Cancel", Left = 357, Width = 75, Top = 60, DialogResult = DialogResult.Cancel };
        dlg.Controls.Add(box); dlg.Controls.Add(ok); dlg.Controls.Add(cancel);
        dlg.AcceptButton = ok; dlg.CancelButton = cancel;

        if (dlg.ShowDialog(owner) == DialogResult.OK && box.Text.Trim().Length > 0)
            CredentialStore.Save(Target, box.Text.Trim());
    }
}