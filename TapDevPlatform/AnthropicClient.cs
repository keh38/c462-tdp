using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace TDP.Api   // rename to match your project
{
    /// <summary>
    /// Minimal Anthropic Messages API client for substage 1: one user message in,
    /// plain text out. No conversation history, no system prompt, no marker parsing —
    /// this exists only to prove auth, the request shape, and response parsing in
    /// isolation. Substage 2 adds the message-history object and the system prompt.
    ///
    /// Request shape (verified against docs.claude.com):
    ///   POST https://api.anthropic.com/v1/messages
    ///   headers: x-api-key, anthropic-version: 2023-06-01, content-type: application/json
    ///   body:    { model, max_tokens, messages: [ { role, content } ] }
    ///   reply:   { content: [ { type: "text", text: "..." }, ... ], usage: {...}, ... }
    ///
    /// The system prompt (added in substage 2) is a TOP-LEVEL "system" field on the
    /// body, never a message with role "system" — that returns 400.
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
        /// Confirm the exact current string against the models page. For a bare
        /// round-trip the model barely matters; any current model proves the seam.
        /// </param>
        public AnthropicClient(string apiKey, string model = "claude-sonnet-5")
        {
            _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
            _model = model;
        }

        /// <summary>
        /// Sends a single user message and returns the concatenated text of the reply.
        /// Throws on transport failure or a non-success API response, with the API's
        /// own error body in the exception message (invaluable while proving the seam).
        /// </summary>
        public async Task<string> SendAsync(string userText, int maxTokens = 1024, CancellationToken ct = default)
        {
            var body = new
            {
                model = _model,
                max_tokens = maxTokens,
                messages = new[]
                {
                    new { role = "user", content = userText }
                }
            };

            string json = JsonSerializer.Serialize(body);

            using var req = new HttpRequestMessage(HttpMethod.Post, Endpoint);
            req.Headers.Add("x-api-key", _apiKey);            // custom header — not validated, will not throw
            req.Headers.Add("anthropic-version", ApiVersion);
            req.Content = new StringContent(json, Encoding.UTF8, "application/json");  // sets Content-Type

            using HttpResponseMessage resp = await _http.SendAsync(req, ct).ConfigureAwait(false);
            string responseJson = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (!resp.IsSuccessStatusCode)
            {
                // Note: this echoes the RESPONSE body, never the request, so the key
                // cannot leak into your debug box.
                throw new HttpRequestException(
                    $"Anthropic API returned {(int)resp.StatusCode} {resp.StatusCode}:\n{responseJson}");
            }

            return ExtractText(responseJson);
        }

        /// <summary>
        /// Concatenates the text of every "text" block in the response's content array,
        /// ignoring any non-text block types.
        /// </summary>
        private static string ExtractText(string responseJson)
        {
            using JsonDocument doc = JsonDocument.Parse(responseJson);

            if (!doc.RootElement.TryGetProperty("content", out JsonElement content)
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
    }
}
