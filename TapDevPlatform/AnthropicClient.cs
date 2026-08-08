using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Serilog;

namespace TDP.Api   // rename to match your project
{
    /// <summary>
    /// Anthropic Messages API client. Pure transport: it serialises a request,
    /// posts it, and returns the concatenated text of the reply. It holds NO
    /// conversation state — every call sends everything it is given. History lives
    /// in <see cref="ConversationManager"/>; marker parsing lives in
    /// <see cref="MarkerDispatcher"/>. Keeping those out of here is deliberate.
    ///
    /// Request shape (verified against docs.claude.com):
    ///   POST https://api.anthropic.com/v1/messages
    ///   headers: x-api-key, anthropic-version: 2023-06-01, content-type: application/json
    ///   body:    { model, max_tokens, [system], messages: [ { role, content }, ... ] }
    ///   reply:   { content: [ { type: "text", text: "..." }, ... ], usage: {...}, ... }
    ///
    /// The system prompt is a TOP-LEVEL "system" field on the body, never a message
    /// with role "system" — that returns 400.
    /// </summary>
    public sealed class AnthropicClient
    {
        private const string Endpoint = "https://api.anthropic.com/v1/messages";
        private const string ApiVersion = "2023-06-01";

        // One HttpClient for the whole process. Never one-per-call (socket exhaustion).
        private static readonly HttpClient _http = new HttpClient();

        private readonly string _apiKey;
        private readonly string _model;

        /// <param name="model">
        /// Confirm the exact current string against the models page. Sonnet is the
        /// default for the rapid loop; escalate to an Opus model for harder new-rule
        /// requests. Keep this a config value so switching is trivial.
        /// </param>
        public AnthropicClient(string apiKey, string model = "claude-sonnet-5")
        {
            _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
            _model = model;
        }

        /// <summary>
        /// Substage-1 isolation harness: one user message in, plain text out. No
        /// system prompt, no history. Still handy as a bare connectivity ping.
        /// </summary>
        public Task<string> SendAsync(string userText, int maxTokens = 1024, CancellationToken ct = default)
        {
            var payload = new Dictionary<string, object>
            {
                ["model"] = _model,
                ["max_tokens"] = maxTokens,
                ["messages"] = new[] { new { role = "user", content = userText } }
            };
            return PostAsync(payload, ct);
        }

        /// <summary>
        /// Substage-2 conversation send: a system prompt plus the full message
        /// history. The caller (ConversationManager) owns and re-sends the history;
        /// this method is stateless. Returns the concatenated assistant text.
        /// </summary>
        /// <param name="system">
        /// The system prompt (Contract + project-head instructions). Omitted from the
        /// body when null/empty. A plain string here; the cache_control block form is
        /// a 4d concern.
        /// </param>
        /// <param name="messages">Full conversation so far, in order, ending on a user turn.</param>
        public Task<string> SendAsync(
            string system,
            IReadOnlyList<ChatMessage> messages,
            int maxTokens = 4096,
            CancellationToken ct = default)
        {
            if (messages == null) throw new ArgumentNullException(nameof(messages));

            var payload = new Dictionary<string, object>
            {
                ["model"] = _model,
                ["max_tokens"] = maxTokens,
                ["messages"] = messages
                    .Select(m => new { role = m.Role, content = m.Content })
                    .ToArray()
            };

            // Conditional inclusion is why the body is a Dictionary rather than an
            // anonymous type: send "system" only when there is one.
            if (!string.IsNullOrEmpty(system))
            {
                payload["system"] = new object[]
                {
                    new
                    {
                        type = "text",
                        text = system,
                        cache_control = new { type = "ephemeral" }
                    }
                };
            }
            return PostAsync(payload, ct);
        }

        /// <summary>
        /// Serialises the body, posts it, and returns the reply text. Throws on
        /// transport failure or a non-success API response, with the API's own error
        /// body in the message (invaluable while proving a seam). Echoes the RESPONSE
        /// body only, never the request, so the key cannot leak into a debug box.
        /// </summary>
        private async Task<string> PostAsync(IDictionary<string, object> payload, CancellationToken ct)
        {
            string json = JsonSerializer.Serialize(payload);

            using var req = new HttpRequestMessage(HttpMethod.Post, Endpoint);
            req.Headers.Add("x-api-key", _apiKey);            // custom header — not validated, will not throw
            req.Headers.Add("anthropic-version", ApiVersion);
            req.Content = new StringContent(json, Encoding.UTF8, "application/json");  // sets Content-Type

            using HttpResponseMessage resp = await _http.SendAsync(req, ct).ConfigureAwait(false);
            string responseJson = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (!resp.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    $"Anthropic API returned {(int)resp.StatusCode} {resp.StatusCode}:\n{responseJson}");
            }

            return ExtractText(responseJson);
        }

        /// <summary>
        /// Concatenates the text of every "text" block in the response's content
        /// array, ignoring any non-text block types.
        /// </summary>
        //private static string ExtractText(string responseJson)
        //{
        //    using JsonDocument doc = JsonDocument.Parse(responseJson);

        //    if (!doc.RootElement.TryGetProperty("content", out JsonElement content)
        //        || content.ValueKind != JsonValueKind.Array)
        //    {
        //        return "(no content array in response)\n" + responseJson;
        //    }

        //    var sb = new StringBuilder();
        //    foreach (JsonElement block in content.EnumerateArray())
        //    {
        //        if (block.TryGetProperty("type", out JsonElement type)
        //            && type.GetString() == "text"
        //            && block.TryGetProperty("text", out JsonElement text))
        //        {
        //            sb.Append(text.GetString());
        //        }
        //    }

        //    return sb.Length > 0 ? sb.ToString() : "(no text blocks in response)\n" + responseJson;
        //}
        private static string ExtractText(string responseJson)
        {
            using JsonDocument doc = JsonDocument.Parse(responseJson);
            JsonElement root = doc.RootElement;

            // --- cache diagnostics: read usage if present ---
            if (root.TryGetProperty("usage", out JsonElement usage))
            {
                long input = GetLong(usage, "input_tokens");
                long output = GetLong(usage, "output_tokens");
                long created = GetLong(usage, "cache_creation_input_tokens");
                long read = GetLong(usage, "cache_read_input_tokens");

                Log.Information(
                    "Anthropic usage — input:{Input} output:{Output} cacheWrite:{Write} cacheRead:{Read}",
                    input, output, created, read);
            }

            // --- text extraction (unchanged) ---
            if (!root.TryGetProperty("content", out JsonElement content)
                || content.ValueKind != JsonValueKind.Array)
            {
                return "(no content array in response)\n" + responseJson;
            }

            var sb = new StringBuilder();
            foreach (JsonElement block in content.EnumerateArray())
            {
                if (block.TryGetProperty("type", out JsonElement type)
                    && type.GetString() == "text"
                    && block.TryGetProperty("text", out JsonElement text))
                {
                    sb.Append(text.GetString());
                }
            }

            return sb.Length > 0 ? sb.ToString() : "(no text blocks in response)\n" + responseJson;
        }

        // Reads a numeric property, returning 0 if absent or not a number.
        private static long GetLong(JsonElement obj, string name)
            => obj.TryGetProperty(name, out JsonElement v) && v.ValueKind == JsonValueKind.Number
                ? v.GetInt64()
                : 0;
    }
}
