using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TDP.Api   // rename to match your project
{
    /// <summary>
    /// Owns one conversation: the system prompt and the ordered message history.
    /// It is the stateful counterpart to the stateless <see cref="AnthropicClient"/>.
    ///
    /// The whole cycle is: append the user turn, send the FULL history (the API is
    /// stateless — every call re-sends everything), append the assistant reply,
    /// return it. That single method is also the substage-4c repair loop: feeding a
    /// MATLAB or validation error back to the model is just another user turn —
    /// call <see cref="SendAsync"/> with the error text. No separate retry path,
    /// no special state, because the history is always re-sent in full.
    ///
    /// What is stored here is the RAW assistant reply (marker line and all). The
    /// terse "generator received ✓" you show in the transcript is a display
    /// transform applied elsewhere — never store the terse form, or the repair loop
    /// goes blind: the model must see what it actually produced to correct it.
    ///
    /// This history is also the persistence unit for substage 4d: serialise
    /// <see cref="History"/> per user and you have the prior-chats list for free.
    /// </summary>
    public sealed class ConversationManager
    {
        private readonly AnthropicClient _client;
        private readonly string _systemPrompt;
        private readonly int _maxTokens;
        private readonly List<ChatMessage> _history = new List<ChatMessage>();

        /// <param name="client">The transport client.</param>
        /// <param name="systemPrompt">
        /// Contract + project-head instructions, concatenated. Load this from FILES,
        /// not string constants — substage 4b is the loop where you iterate on the
        /// instructions doc, and you do not want a recompile between edits.
        /// </param>
        /// <param name="maxTokens">
        /// Give a full generator real headroom. A truncated generator that fails to
        /// parse looks like a model problem when it is a budget problem.
        /// </param>
        public ConversationManager(AnthropicClient client, string systemPrompt, int maxTokens = 4096)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _systemPrompt = systemPrompt ?? string.Empty;
            _maxTokens = maxTokens;
        }

        /// <summary>The conversation so far, in order. Read-only; use for display and persistence.</summary>
        public IReadOnlyList<ChatMessage> History => _history;

        public string SystemPrompt => _systemPrompt;

        /// <summary>
        /// Appends <paramref name="userText"/> as a user turn, sends the full
        /// conversation, appends and returns the assistant reply.
        ///
        /// On failure the history is left UNTOUCHED: the new user turn is committed
        /// only after a successful reply, so a transport/API error leaves no dangling
        /// turn and the user can simply retry. (This is why we build a local
        /// <c>pending</c> list rather than mutating <c>_history</c> before the call.)
        /// </summary>
        public async Task<string> SendAsync(string userText, CancellationToken ct = default)
        {
            if (userText == null) throw new ArgumentNullException(nameof(userText));

            var userTurn = ChatMessage.User(userText);

            // Send history + the new turn without committing the turn yet.
            var pending = new List<ChatMessage>(_history) { userTurn };

            string reply = await _client
                .SendAsync(_systemPrompt, pending, _maxTokens, ct)
                .ConfigureAwait(false);

            // Success — commit both turns, in order.
            _history.Add(userTurn);
            _history.Add(ChatMessage.Assistant(reply));
            return reply;
        }

        /// <summary>
        /// Replaces the current history wholesale — for loading a saved conversation
        /// (substage 4d). The system prompt is fixed at construction and is not part
        /// of the saved history.
        /// </summary>
        public void LoadHistory(IEnumerable<ChatMessage> messages)
        {
            if (messages == null) throw new ArgumentNullException(nameof(messages));
            _history.Clear();
            _history.AddRange(messages);
        }

        /// <summary>Clears the history to start a fresh conversation with the same system prompt.</summary>
        public void Reset() => _history.Clear();
    }
}
