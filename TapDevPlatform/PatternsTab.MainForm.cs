using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Tapping;

using TDP.Api;        // AnthropicClient, ConversationManager, MarkerDispatcher, DispatchedReply, ReplyKind
using TDP.Security;   // CredentialStore  (ApiKeyProvisioning is in the global namespace)

namespace TapDevPlatform
{
    /// <summary>
    /// Patterns-tab behaviour for substage 4b: type a description, send it, and route
    /// the reply by its marker. This stops at "generator written, here's where" — it
    /// does NOT run MATLAB, validate, or transfer. Those are 4c, parked behind
    /// generateButton with a labelled seam, so 4b stays a clean prompt-engineering
    /// checkpoint.
    ///
    /// Assumptions (all flagged where they matter):
    ///   * _conversation (ConversationManager) is built LAZILY on first send by
    ///     EnsureConversationReady() — key -> client -> conversation, once; it is
    ///     null until then. The app opens on THIS tab, so we deliberately do not
    ///     build on tab-activation (that would fire the key dialog at launch). The
    ///     first send is the trigger.
    ///   * _subjectName holds the current user's name (they are their own HTS subject).
    ///   * Controls present on the form: inputTextBox, sendButton, transcriptRichTextBox,
    ///     trialsDataGridView, generateButton, chatListBox, newChatButton.
    ///   * inputTextBox.Multiline = true AND AcceptsReturn = true, so Shift+Enter can
    ///     insert a newline while Enter sends (set both in the Designer).
    /// </summary>
    public partial class MainForm : Form
    {
        // ----- state this handler owns -------------------------------------------
        // NOTE: _conversation (ConversationManager) is declared on the form already;
        // it is now built lazily (see EnsureConversationReady) and may be null.
        private AnthropicClient _client;
        private bool _isSending;
        private string _pendingGeneratorCode;   // last GENERATOR body, awaiting a Generate click; null otherwise
        private Font _labelFont;
        private Font _bodyFont;

        // Transcript colours — tweak to taste.
        private static readonly Color UserColor = Color.RoyalBlue;
        private static readonly Color AssistantColor = Color.ForestGreen;
        private static readonly Color NoteColor = Color.DimGray;
        private static readonly Color WarnColor = Color.DarkOrange;
        private static readonly Color ErrorColor = Color.Firebrick;

        /// <summary>
        /// Call this ONCE from your form constructor, AFTER InitializeComponent().
        /// It only wires events; the client and conversation are built lazily on the
        /// first send (see EnsureConversationReady). The system prompt is loaded from
        /// file in BuildSystemPrompt — that is the one spot you own.
        /// </summary>
        private void InitializePatternsTab()
        {
            sendButton.Click += OnSendClicked;
            inputTextBox.KeyDown += OnInputKeyDown;
            generateButton.Click += OnGenerateClicked;
            newChatButton.Click += OnNewChatClicked;

            generateButton.Enabled = false;   // nothing to run until a GENERATOR arrives
        }

        // ----- lazy construction: key -> client -> conversation ------------------

        /// <summary>
        /// Ensures _conversation exists, building it on first need: retrieve the key
        /// (auto-popping the provisioning dialog if absent), construct the client, load
        /// the system prompt, construct the conversation. Idempotent — returns true once
        /// ready. Returns false only when there is no usable key (e.g. the user cancelled
        /// the dialog), so the caller can bail before touching UI or busy-state. May
        /// throw if BuildSystemPrompt fails; the caller surfaces that as an error line.
        /// </summary>
        private bool EnsureConversationReady()
        {
            if (_conversation != null) return true;

            string key = CredentialStore.Load(ApiKeyProvisioning.Target);
            if (string.IsNullOrEmpty(key))
            {
                ApiKeyProvisioning.PromptAndStore(this);          // auto-pop the Set-Key dialog
                key = CredentialStore.Load(ApiKeyProvisioning.Target);
                if (string.IsNullOrEmpty(key)) return false;      // cancelled / still nothing
            }

            _client = new AnthropicClient(key /*, model */);
            _conversation = new ConversationManager(_client, BuildSystemPrompt());
            SeedSessionContext();          // <-- inject the ProfileTarget context as the opening turns
            return true;
        }

        /// <summary>
        /// Builds the system prompt. THIS IS YOURS: load the two context documents (the
        /// Contract + the project-head AI instructions) from file and concatenate them.
        /// Read here (not cached) so that during 4b an edit to the instructions doc
        /// takes effect on the next New Chat — which rebuilds the conversation — without
        /// an app restart.
        /// </summary>
        private string BuildSystemPrompt()
        {
            string contract = File.ReadAllText(FileLocations.ContractPath);
            string instructions = File.ReadAllText(FileLocations.InstructionsPath);
            return contract + "\n\n" + instructions;
        }

        private const string ProfileContextMarker = "[SESSION PROFILE TARGETS]";
        private const string ProfileContextAck =
            "Understood — I'll use these parameters, refer to them by short name, and put the " +
            "exact Item path in any ParameterProfile. I won't add a profile unless asked.";

        /// <summary>
        /// Seeds a freshly-built conversation with the session's profile-target context: one
        /// user turn declaring the available parameters, plus a short assistant acknowledgment
        /// so the turns alternate (some API surfaces reject two user turns in a row). Persisted
        /// with the session; rendered tersely. Runs once per conversation birth. On load,
        /// LoadHistory overwrites these with the saved turns (which carry their own context
        /// pair), so there is no duplication.
        /// </summary>
        private void SeedSessionContext()
        {
            string body = BuildProfileTargetContext(_currentConfig.ProfileTargets);   // your List<ProfileTarget>
            _conversation.LoadHistory(new[]
            {
                ChatMessage.User(ProfileContextMarker + "\n" + body),
                ChatMessage.Assistant(ProfileContextAck)
            });
            AppendTranscript("—", "session parameters loaded", NoteColor, boldLabel: false);
        }

        /// <summary>
        /// Formats the profile targets into the context body. The empty case is stated
        /// explicitly — silence is ambiguous (failed to load, or genuinely none?), and an
        /// explicit "none" reinforces the contract's "a profile is never required" and
        /// "do not guess an Item" rules.
        /// </summary>
        private static string BuildProfileTargetContext(IReadOnlyList<ProfileTarget> targets)
        {
            if (targets == null || targets.Count == 0)
                return "No stimulus parameters are exposed for profiling this session. " +
                       "Do not produce any ParameterProfiles.";

            var sb = new StringBuilder();
            sb.AppendLine("Stimulus parameters available to vary in a ParameterProfile this session.");
            sb.AppendLine("Refer to each by its short name; put the exact Item path in ParameterProfile.Item:");
            foreach (var t in targets)
                sb.AppendLine($"  \u2022 {t.ShortName} \u2192 {t.Item}");
            sb.Append("A profile is optional; omit ParameterProfiles entirely if none is wanted.");
            return sb.ToString();
        }

        private static bool IsSessionContext(ChatMessage m)
            => m.Role == "user" && m.Content != null
               && m.Content.StartsWith(ProfileContextMarker, StringComparison.Ordinal);

        // ----- send: button and Enter --------------------------------------------

        private async void OnSendClicked(object sender, EventArgs e)
            => await SendCurrentInputAsync();

        private async void OnInputKeyDown(object sender, KeyEventArgs e)
        {
            // Enter sends; Shift+Enter inserts a newline (needs Multiline + AcceptsReturn).
            if (e.KeyCode == Keys.Enter && !e.Shift)
            {
                e.SuppressKeyPress = true;   // swallow the keystroke: no ding, no stray newline
                e.Handled = true;
                await SendCurrentInputAsync();
            }
        }

        /// <summary>
        /// The whole 4b turn: show the user's text, send the full conversation, parse
        /// the reply by marker, route it. History and busy-state are handled so a
        /// failure leaves everything consistent and retryable.
        /// </summary>
        private async Task SendCurrentInputAsync()
        {
            if (_isSending) return;

            string userText = inputTextBox.Text.Trim();
            if (userText.Length == 0) return;

            // Lazily build key -> client -> conversation on first send. Do this BEFORE
            // touching busy-state or the transcript so a cancelled key dialog leaves the
            // input box and UI exactly as they were.
            bool ready;
            try
            {
                ready = EnsureConversationReady();
            }
            catch (Exception ex)
            {
                AppendTranscript("Error", ex.Message, ErrorColor, boldLabel: true);
                return;
            }
            if (!ready)
            {
                AppendTranscript("Note", "No API key set \u2014 nothing sent.", NoteColor, boldLabel: true);
                return;
            }

            _pendingGeneratorCode = null;   // any generator from a prior turn is now stale
            SetBusy(true);

            AppendTranscript(string.IsNullOrEmpty(_subjectName) ? "You" : _subjectName,
                             userText, UserColor, boldLabel: true);
            inputTextBox.Clear();

            try
            {
                // ConversationManager appends the user turn, sends the whole history,
                // and stores the RAW reply (marker line and all) before we ever parse.
                string reply = await _conversation.SendAsync(ComposeOutgoingMessage(userText));
                DispatchedReply dispatched = MarkerDispatcher.Parse(reply);
                HandleReply(dispatched);
                Autosave();   // persist the turn -> session appears/updates in the chat list
            }
            catch (Exception ex)
            {
                // On failure ConversationManager leaves history CLEAN (the user turn is
                // committed only on success), so a resend is safe. Put the text back in
                // the box for a one-click retry. (The message already shows above; a
                // retry will show it again — harmless for a self-test tool.)
                AppendTranscript("Error", ex.Message, ErrorColor, boldLabel: true);
                inputTextBox.Text = userText;
            }
            finally
            {
                SetBusy(false);
                inputTextBox.Focus();
            }
        }

        /// <summary>Routes a parsed reply to the transcript and, for GENERATOR, arms the Generate button.</summary>
        private void HandleReply(DispatchedReply d)
        {
            switch (d.Kind)
            {
                case ReplyKind.Generator:
                    // Store the code; SetBusy(false) in the finally will enable generateButton.
                    _pendingGeneratorCode = d.Body;
                    AppendTranscript(
                        "Assistant",
                        $"\u2713 GENERATOR received ({CountLines(d.Body)} lines). " +
                        "Click \u201cGenerate\u201d to write it to the sandbox and run it.",
                        AssistantColor, boldLabel: true);
                    break;

                case ReplyKind.Question:
                    AppendTranscript("Assistant \u2014 question", d.Body, AssistantColor, boldLabel: true);
                    break;

                case ReplyKind.Cannot:
                    AppendTranscript("Assistant \u2014 can\u2019t express this as a flat plan",
                                     d.Body, NoteColor, boldLabel: true);
                    break;

                default: // Unknown — the model didn't open with a marker.
                    // In 4b this is a SIGNAL, not noise: your instructions doc isn't
                    // pinning the protocol. Show it loudly and fix the prompt.
                    AppendTranscript(
                        $"\u26a0 Unmarked reply (protocol violation; first token: \u201c{d.Marker}\u201d)",
                        d.Body, WarnColor, boldLabel: true);
                    break;
            }
        }

        // ----- new chat ----------------------------------------------------------

        private void OnNewChatClicked(object sender, EventArgs e)
        {
            // 4d (deferred): before clearing, persist _conversation.History under
            // _subjectName and add an entry to chatListBox so it can be reloaded.

            // 4b: null the conversation (and client) rather than Reset() so the NEXT
            // send rebuilds via EnsureConversationReady — which reloads the system
            // prompt, picking up edits to the instructions doc without an app restart.
            // The key stays in the credential store, so no dialog reappears. In 4d you
            // may prefer _conversation.Reset() to keep the built client/prompt and just
            // persist + clear the history.
            _conversation = null;
            _client = null;

            transcriptRichTextBox.Clear();

            // If trialsDataGridView is data-bound in 4c, clear the bound list instead
            // (Rows.Clear() throws when a DataSource is set).
            trialsDataGridView.Rows.Clear();

            _pendingGeneratorCode = null;
            generateButton.Enabled = false;
            inputTextBox.Clear();
            inputTextBox.Focus();
        }

        // ----- helpers -----------------------------------------------------------

        private void SetBusy(bool busy)
        {
            _isSending = busy;
            sendButton.Enabled = !busy;
            inputTextBox.Enabled = !busy;
            newChatButton.Enabled = !busy;
            // Generate is available only when idle AND a generator is pending.
            generateButton.Enabled = !busy && _pendingGeneratorCode != null;

//            sendButton.Text = busy ? "\u2026" : "Send";
            sendButton.Text = busy ? "Thinking..." : "Send";
            UseWaitCursor = busy;
        }

        private void AppendTranscript(string speaker, string message, Color labelColor, bool boldLabel)
        {
            if (_labelFont == null)
            {
                // Cached so we don't leak a GDI Font per line over a long session.
                // (Dispose both in the form's Dispose override if you want to be tidy.)
                _labelFont = new Font(transcriptRichTextBox.Font, FontStyle.Bold);
                _bodyFont = new Font(transcriptRichTextBox.Font, FontStyle.Regular);
            }

            transcriptRichTextBox.SelectionStart = transcriptRichTextBox.TextLength;
            transcriptRichTextBox.SelectionLength = 0;

            transcriptRichTextBox.SelectionColor = labelColor;
            transcriptRichTextBox.SelectionFont = boldLabel ? _labelFont : _bodyFont;
            transcriptRichTextBox.AppendText(speaker + Environment.NewLine);

            transcriptRichTextBox.SelectionColor = transcriptRichTextBox.ForeColor;
            transcriptRichTextBox.SelectionFont = _bodyFont;
            transcriptRichTextBox.AppendText(message + Environment.NewLine + Environment.NewLine);

            transcriptRichTextBox.SelectionStart = transcriptRichTextBox.TextLength;
            transcriptRichTextBox.ScrollToCaret();
        }

        private static int CountLines(string s)
            => string.IsNullOrEmpty(s) ? 0 : s.Replace("\r\n", "\n").Split('\n').Length;
    }
}
