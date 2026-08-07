using System;

namespace TDP.Api   // rename to match your project
{
    /// <summary>
    /// One turn in a conversation: a role ("user" or "assistant") and its text.
    /// This is the unit the <see cref="ConversationManager"/> holds and the
    /// <see cref="AnthropicClient"/> serialises into the request's "messages" array.
    ///
    /// Content is a plain string for now. The array-of-blocks form (needed only when
    /// you attach cache_control in substage 4d) is a later concern — keeping it a
    /// string here keeps substage 4b's request shape as small as possible.
    ///
    /// Deliberately a plain immutable class (not a C# 9 record) so it compiles on
    /// .NET Framework 4.x at the same language level as the rest of the client.
    /// </summary>
    public sealed class ChatMessage
    {
        public string Role { get; }
        public string Content { get; }

        public ChatMessage(string role, string content)
        {
            Role = role ?? throw new ArgumentNullException(nameof(role));
            Content = content ?? throw new ArgumentNullException(nameof(content));
        }

        public static ChatMessage User(string content) => new ChatMessage("user", content);
        public static ChatMessage Assistant(string content) => new ChatMessage("assistant", content);
    }
}
