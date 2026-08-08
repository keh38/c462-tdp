using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

using TDP.Api;
using TDP.Sessions;
using TDP.Tapping;

namespace TapDevPlatform
{ 
    /// <summary>
    /// Substage-4d master list, sessions, replay, and the tweak-a-generator hook.
    /// Partial of MainForm. Call InitializeMasterList() ONCE from the constructor,
    /// after InitializePatternsTab().
    /// </summary>
    public partial class MainForm : Form
    {
        // ----- state -------------------------------------------------------------
        private TappingSession _session;
        private readonly BindingList<TrialRowVM> _rows = new BindingList<TrialRowVM>();
        private Guid _highlightGeneratorId = Guid.Empty;   // newest batch, for row highlighting
        private GeneratorRun _tweakTarget;                 // set when "tweak this generator" is armed
        private bool _suppressAutosave;                    // guards bulk rebuilds (load) from thrashing saves

        private static readonly Color NewBatchColor = Color.FromArgb(232, 245, 233); // faint green

        // ----- wire-up -----------------------------------------------------------

        private void InitializeMasterList()
        {
            ConfigureTrialsGrid();

            trialsDataGridView.DataSource = _rows;
            _rows.ListChanged += (s, e) => Autosave();          // Tag edits, adds, removes
            trialsDataGridView.CellFormatting += TrialsGrid_CellFormatting;
            trialsDataGridView.KeyDown += TrialsGrid_KeyDown;   // Delete key

            chatListView.View = View.Details;
            chatListView.FullRowSelect = true;
            chatListView.MultiSelect = false;
            if (chatListView.Columns.Count == 0)
            {
                chatListView.Columns.Add("Title", 220);
                chatListView.Columns.Add("Modified", 120);
                chatListView.Columns.Add("Trials", 55, HorizontalAlignment.Right);
            }
            chatListView.SelectedIndexChanged += ChatListView_SelectedIndexChanged;

            chatListView.LabelEdit = true;
            chatListView.AfterLabelEdit += ChatListView_AfterLabelEdit;
            chatListView.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.F2 && chatListView.SelectedItems.Count > 0)
                    chatListView.SelectedItems[0].BeginEdit();
            };

            var listMenu = new ContextMenuStrip();
            listMenu.Items.Add("Rename", null, (s, e) =>
            {
                if (chatListView.SelectedItems.Count > 0)
                    chatListView.SelectedItems[0].BeginEdit();
            });
            listMenu.Items.Add("Delete", null, (s, e) => DeleteSelectedSession());
            chatListView.ContextMenuStrip = listMenu;

            // Right-click doesn't select by default — select the row under the cursor so the
            // menu (and F2) act on what was clicked.
            chatListView.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Right)
                {
                    var hit = chatListView.GetItemAt(e.X, e.Y);
                    if (hit != null) hit.Selected = true;
                }
            };

            StartNewSession();     // begin with an empty session
            RefreshSessionList();
        }

        private void ChatListView_AfterLabelEdit(object sender, LabelEditEventArgs e)
        {
            string newTitle = e.Label?.Trim();          // null when the user pressed Esc
            if (string.IsNullOrEmpty(newTitle)) { e.CancelEdit = true; return; }
            if (!(chatListView.Items[e.Item].Tag is SessionSummary summary)) { e.CancelEdit = true; return; }

            RenameSession(summary, newTitle);
            BeginInvoke(new Action(RefreshSessionList));   // rebuild after the edit control closes
        }

        private void RenameSession(SessionSummary summary, string newTitle)
        {
            try
            {
                if (_session != null && _session.Id == summary.Id)
                {
                    _session.Title = newTitle;
                    _session.TitleIsUserSet = true;
                    SaveCurrentSession();       // projects + persists; won't re-derive (title isn't "(untitled)")
                }
                else
                {
                    var s = SessionStore.Load(summary.FilePath);   // rename a session that isn't loaded
                    s.Title = newTitle;
                    s.TitleIsUserSet = true;
                    SessionStore.Save(s);
                }
            }
            catch (Exception ex)
            {
                AppendTranscript("Note", "Rename failed: " + ex.Message, NoteColor, boldLabel: true);
            }
        }

        private void DeleteSelectedSession()
        {
            if (chatListView.SelectedItems.Count == 0) return;
            if (!(chatListView.SelectedItems[0].Tag is SessionSummary summary)) return;

            var confirm = MessageBox.Show(
                $"Delete \u201c{summary.Title}\u201d? This cannot be undone.",
                "Delete session", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (confirm != DialogResult.Yes) return;

            bool deletingCurrent = _session != null && _session.Id == summary.Id;

            // Sever the loaded session FIRST, so no autosave can write the file back
            // after we delete it. StartNewSession swaps _session for a fresh one and
            // clears the grid/transcript.
            if (deletingCurrent)
                StartNewSession();

            try
            {
                SessionStore.Delete(summary);
            }
            catch (Exception ex)
            {
                AppendTranscript("Note", "Delete failed: " + ex.Message, NoteColor, boldLabel: true);
            }

            RefreshSessionList();
        }
        private void ConfigureTrialsGrid()
        {
            var g = trialsDataGridView;
            g.AutoGenerateColumns = false;
            g.Columns.Clear();
            g.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            g.MultiSelect = true;
            g.RowHeadersVisible = false;   
            g.AllowUserToAddRows = false;
            g.AllowUserToDeleteRows = false; // we handle delete ourselves (keeps model in sync)

            void Col(string header, string prop, int width, bool readOnly = true)
            {
                g.Columns.Add(new DataGridViewTextBoxColumn
                {
                    HeaderText = header,
                    DataPropertyName = prop,
                    Width = width,
                    ReadOnly = readOnly,
                    SortMode = DataGridViewColumnSortMode.NotSortable
                });
            }

            Col("Tag", nameof(TrialRowVM.Tag), 110, readOnly: false);  // the ONLY editable column
            Col("Pacer", nameof(TrialRowVM.Pacer), 50);
            Col("Response", nameof(TrialRowVM.Response), 90);
            Col("Pacer iv", nameof(TrialRowVM.PacerSummary), 100);
            Col("Distractor", nameof(TrialRowVM.DistractorSummary), 110);
            Col("LeadIn", nameof(TrialRowVM.LeadIn), 60);
            Col("Offset", nameof(TrialRowVM.Offset), 60);
            Col("Duration", nameof(TrialRowVM.Duration), 80);
            Col("Profile", nameof(TrialRowVM.Profile), 130);
            Col("Source", nameof(TrialRowVM.Source), 120);

            // Right-click menu: tweak the generator, or delete trials.
            var menu = new ContextMenuStrip();
            menu.Items.Add("Tweak this generator\u2026", null, (s, e) => TweakSelectedGenerator());
            menu.Items.Add("Delete trial(s)", null, (s, e) => DeleteSelectedTrials());
            g.ContextMenuStrip = menu;
        }

        // ----- append on generator success --------------------------------------
        // Replaces the 4c DeliverValidatedListAsync. RunWithRepairAsync must now pass
        // the generator code:  await DeliverValidatedListAsync(jsonPath, code);

        private async Task DeliverValidatedListAsync(string jsonPath, string generatorCode)
        {
            string json = File.ReadAllText(jsonPath);
            TappingTrialListDto list = TrialListCodec.Deserialize(json);

            var run = new GeneratorRun
            {
                Id = Guid.NewGuid(),
                Name = ExtractGeneratorName(generatorCode, list),
                Code = generatorCode,
                Seed = list?.Provenance?.Seed ?? 0
            };
            _session.Generators.Add(run);

            if (_session.Title == "(untitled)" && !_session.TitleIsUserSet)
                _session.Title = run.Name;   // first generator names the session if the user hasn't done so

            // Append the new trials to the master list (and the bound grid).
            int firstNew = _rows.Count;
            if (list?.Trials != null)
            {
                foreach (var t in list.Trials)
                    _rows.Add(new TrialRowVM(new TrialEntry { Trial = t, GeneratorId = run.Id }, run.Name));
            }

            // Highlight + scroll to the newest batch (by generator id — survives deletes).
            _highlightGeneratorId = run.Id;
            trialsDataGridView.ClearSelection();
            if (_rows.Count > firstNew)
            {
                trialsDataGridView.FirstDisplayedScrollingRowIndex = firstNew;
                for (int i = firstNew; i < _rows.Count; i++)
                    trialsDataGridView.Rows[i].Selected = true;
            }
            trialsDataGridView.Invalidate();   // repaint with the new highlight

            Autosave();

            if (!_network.IsConnected)
            {
                AppendTranscript("Note", "Trials generated but not sent: no HTS connection.", NoteColor, boldLabel: true);
                return;
            }

            // Play the new batch directly — this JSON is MATLAB's canonical output, so
            // no re-serialisation is needed for the generate-and-hear step.
            await StartTappingRunAsync(json);
        }

        private void TrialsGrid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _rows.Count) return;
            if (_rows[e.RowIndex].GeneratorId == _highlightGeneratorId && _highlightGeneratorId != Guid.Empty)
                e.CellStyle.BackColor = NewBatchColor;
        }

        // ----- replay (standalone Run button) -----------------------------------
        // Wire your run button to this:  private async void RunButton_Click(...) => await ReplaySelectedAsync();

        private async Task ReplaySelectedAsync()
        {
            var selected = SelectedEntriesInGridOrder();
            if (selected.Count == 0)
            {
                AppendTranscript("Note", "Select one or more trials in the table to replay.", NoteColor, boldLabel: true);
                return;
            }

            // Derive a pure, ordered trial list from the selection and send it directly.
            string json = TrialListCodec.SerializeSubset(selected.Select(e => e.Trial));
            await StartTappingRunAsync(json);
        }

        private async Task PreviewSelectedAsync()
        {
            var selected = SelectedEntriesInGridOrder();
            if (selected.Count != 1)
            {
                AppendTranscript("Note", "Select just one trial in the table to preview.", NoteColor, boldLabel: true);
                return;
            }
            previewButton.Enabled = false;
            try
            {
                // Derive a pure, ordered trial list from the selection and send it directly.
                string json = TrialListCodec.SerializeSubset(selected.Select(e => e.Trial));
                await Task.Run(() => MATLAB.PreviewTrialList(json));
            }
            catch (Exception ex)
            {
                AppendTranscript("Error", "Preview failed: " + ex.Message, ErrorColor, boldLabel: true);
            }
            previewButton.Enabled = true;
        }

        private List<TrialEntry> SelectedEntriesInGridOrder()
        {
            var result = new List<TrialEntry>();
            for (int i = 0; i < trialsDataGridView.Rows.Count; i++)   // grid order = playback order
            {
                if (trialsDataGridView.Rows[i].Selected && trialsDataGridView.Rows[i].DataBoundItem is TrialRowVM vm)
                    result.Add(vm.Entry);
            }
            return result;
        }

        // ----- delete -----------------------------------------------------------

        private void TrialsGrid_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete) { DeleteSelectedTrials(); e.Handled = true; }
        }

        private void DeleteSelectedTrials()
        {
            var doomed = SelectedEntriesInGridOrder();
            if (doomed.Count == 0) return;

            // Remove by matching VM (BindingList removal updates the grid and fires
            // ListChanged -> Autosave). Iterate a snapshot so we can mutate _rows.
            foreach (var vm in _rows.Where(r => doomed.Contains(r.Entry)).ToList())
                _rows.Remove(vm);
        }

        // ----- tweak a generator in the chat ------------------------------------

        private void TweakSelectedGenerator()
        {
            var sel = SelectedEntriesInGridOrder();
            if (sel.Count == 0)
            {
                AppendTranscript("Note", "Select a trial whose generator you want to tweak.", NoteColor, boldLabel: true);
                return;
            }

            // Use the generator of the first selected row (a batch shares one generator).
            var run = _session.FindGenerator(sel[0].GeneratorId);
            if (run == null) return;

            _tweakTarget = run;
            AppendTranscript("Note",
                $"Tweaking generator \u201c{run.Name}\u201d \u2014 describe your change and send.",
                NoteColor, boldLabel: true);
            inputTextBox.Focus();
        }

        /// <summary>
        /// Applies the armed tweak (if any) to an outgoing message: embeds the target
        /// generator's source so the model has it verbatim, then clears the target.
        /// Call this in SendCurrentInputAsync in place of the raw user text:
        ///     string reply = await _conversation.SendAsync(ComposeOutgoingMessage(userText));
        /// The transcript still shows the plain userText; history holds the full thing —
        /// the same display-vs-history split you already use for generators.
        /// </summary>
        private string ComposeOutgoingMessage(string userText)
        {
            if (_tweakTarget == null) return userText;

            var run = _tweakTarget;
            _tweakTarget = null;   // consume

            return
                $"I'd like to tweak the generator named \"{run.Name}\" from earlier in this session. " +
                "Produce a new GENERATOR reflecting the change I describe, keeping it valid per the contract.\n\n" +
                "```matlab\n" + run.Code + "\n```\n\n" +
                "Change requested: " + userText;
        }

        // ----- sessions: new / load / autosave ----------------------------------

        private void StartNewSession()
        {
            // Persist whatever we had, then begin fresh. Nulling _conversation makes the
            // next send rebuild it via EnsureConversationReady — which reloads the system
            // prompt (handy while the instructions doc is still moving).
            SaveCurrentSession();

            _session = new TappingSession { Subject = _subjectName ?? "" };
            _conversation = null;
            _client = null;

            _suppressAutosave = true;
            _rows.Clear();
            _suppressAutosave = false;

            _highlightGeneratorId = Guid.Empty;
            _pendingGeneratorCode = null;
            transcriptRichTextBox.Clear();
            inputTextBox.Clear();
            trialsDataGridView.Invalidate();
        }

        private void ChatListView_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (chatListView.SelectedItems.Count == 0) return;
            if (chatListView.SelectedItems[0].Tag is SessionSummary summary)
                LoadSession(summary);
        }

        private void LoadSession(SessionSummary summary)
        {
            SaveCurrentSession();   // persist current before swapping

            TappingSession loaded;
            try { loaded = SessionStore.Load(summary.FilePath); }
            catch (Exception ex)
            {
                AppendTranscript("Error", "Couldn't load that session: " + ex.Message, ErrorColor, boldLabel: true);
                return;
            }

            _session = loaded;
            _subjectName = string.IsNullOrEmpty(loaded.Subject) ? _subjectName : loaded.Subject;

            // Rebuild the conversation with the saved history.
            _conversation = null;
            _client = null;
            if (!EnsureConversationReady()) return;   // no key -> can't rebuild; message already shown
            _conversation.LoadHistory(loaded.Conversation.Select(ToChatMessage));

            // Rebuild the grid from the master list.
            _suppressAutosave = true;
            _rows.Clear();
            foreach (var entry in loaded.MasterList)
            {
                string src = loaded.FindGenerator(entry.GeneratorId)?.Name ?? "";
                _rows.Add(new TrialRowVM(entry, src));
            }
            _suppressAutosave = false;

            _highlightGeneratorId = Guid.Empty;
            _pendingGeneratorCode = null;
            RenderHistoryToTranscript();
            trialsDataGridView.ClearSelection();
            trialsDataGridView.Invalidate();
        }

        /// <summary>
        /// Autosave hook. Call after any meaningful mutation: it is already wired to grid
        /// changes (Tag/add/delete). ALSO call it once after each conversation turn — add
        /// `Autosave();` after HandleReply(...) in SendCurrentInputAsync.
        /// </summary>
        private void Autosave()
        {
            if (_suppressAutosave || _session == null) return;
            SaveCurrentSession();
            RefreshSessionList();
        }

        private void SaveCurrentSession()
        {
            if (_session == null) return;

            // Project the live UI state back into the document, then write it.
            _session.Subject = _subjectName ?? "";
            _session.MasterList = _rows.Select(r => r.Entry).ToList();
            if (_conversation != null)
                _session.Conversation = _conversation.History.Select(ToPersisted).ToList();

            //if (_session.Title == "(untitled)")
            //    _session.Title = DeriveTitle();

            // Don't litter the store with a truly empty session.
            if (_session.MasterList.Count == 0 && _session.Conversation.Count == 0) return;

            try { SessionStore.Save(_session); }
            catch (Exception ex) { AppendTranscript("Note", "Autosave failed: " + ex.Message, NoteColor, boldLabel: true); }
        }

        private void RefreshSessionList()
        {
            chatListView.BeginUpdate();
            chatListView.Items.Clear();
            foreach (var s in SessionStore.Enumerate(_subjectName ?? ""))
            {
                var item = new ListViewItem(s.Title) { Tag = s };
                item.SubItems.Add(s.ModifiedUtc.ToLocalTime().ToString("g"));
                item.SubItems.Add(s.TrialCount.ToString());
                chatListView.Items.Add(item);
            }
            chatListView.EndUpdate();
        }

        // ----- helpers ----------------------------------------------------------

        private string DeriveTitle()
        {
            // First user turn, trimmed to a snippet.
            var firstUser = _conversation?.History.FirstOrDefault(m => m.Role == "user" && !IsSessionContext(m));
            //var firstUser = _conversation?.History.FirstOrDefault(m => m.Role == "user");
            string text = firstUser?.Content?.Trim();
            if (string.IsNullOrEmpty(text)) return "Session " + _session.CreatedUtc.ToLocalTime().ToString("g");
            text = text.Replace('\n', ' ').Replace('\r', ' ');
            return text.Length <= 48 ? text : text.Substring(0, 48) + "\u2026";
        }

        private static string ExtractGeneratorName(string code, TappingTrialListDto list)
        {
            if (!string.IsNullOrEmpty(list?.Provenance?.Name)) return list.Provenance.Name;
            if (string.IsNullOrEmpty(code)) return "generator";

            foreach (var raw in code.Split('\n'))
            {
                var line = raw.Trim();
                if (line.Length == 0) continue;
                if (line.StartsWith("%"))                 // header comment names the experiment
                    return line.TrimStart('%', ' ', '\t');
                break;                                    // first non-blank line wasn't a comment
            }
            return "generator";
        }

        private void RenderHistoryToTranscript()
        {
            transcriptRichTextBox.Clear();
            foreach (var m in _conversation.History)
            {
                if (IsSessionContext(m))
                {
                    AppendTranscript("—", "session parameters loaded", NoteColor, boldLabel: false);
                    continue;
                }
                if (m.Role == "assistant" && m.Content == ProfileContextAck)
                    continue;   // synthetic acknowledgment — not shown

                if (m.Role == "user")
                {
                    AppendTranscript(string.IsNullOrEmpty(_subjectName) ? "You" : _subjectName,
                                     m.Content, UserColor, boldLabel: true);
                }
                else
                {
                    // Render assistant turns the same way HandleReply would, but WITHOUT
                    // side effects (no arming the Generate button on reload).
                    var d = MarkerDispatcher.Parse(m.Content);
                    switch (d.Kind)
                    {
                        case ReplyKind.Generator:
                            AppendTranscript("Assistant", $"\u2713 GENERATOR ({CountLinesSafe(d.Body)} lines)", AssistantColor, boldLabel: true);
                            break;
                        case ReplyKind.Question:
                            AppendTranscript("Assistant \u2014 question", d.Body, AssistantColor, boldLabel: true);
                            break;
                        case ReplyKind.Cannot:
                            AppendTranscript("Assistant \u2014 can\u2019t express this", d.Body, NoteColor, boldLabel: true);
                            break;
                        default:
                            AppendTranscript("Assistant", d.Body, WarnColor, boldLabel: true);
                            break;
                    }
                }
            }
        }

        private static int CountLinesSafe(string s)
            => string.IsNullOrEmpty(s) ? 0 : s.Replace("\r\n", "\n").Split('\n').Length;

        private static ChatMessage ToChatMessage(PersistedMessage m)
            => m.Role == "user" ? ChatMessage.User(m.Content) : ChatMessage.Assistant(m.Content);

        private static PersistedMessage ToPersisted(ChatMessage m)
            => new PersistedMessage { Role = m.Role, Content = m.Content };
    }
}
