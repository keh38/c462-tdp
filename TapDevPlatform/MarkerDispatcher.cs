using System;

namespace TDP.Api   // rename to match your project
{
    /// <summary>The three protocol shapes, plus Unknown for a reply that breaks protocol.</summary>
    public enum ReplyKind
    {
        Generator,
        Question,
        Cannot,
        Unknown
    }

    /// <summary>The result of dispatching a reply: its kind, the body, and the untouched original.</summary>
    public sealed class DispatchedReply
    {
        /// <summary>Which shape the reply declared on its first line.</summary>
        public ReplyKind Kind { get; }

        /// <summary>
        /// Everything after the marker line. For GENERATOR this is the MATLAB code to
        /// write to the sandbox; for QUESTION/CANNOT it is the prose to show. For
        /// Unknown it is the entire reply, so nothing is hidden.
        /// </summary>
        public string Body { get; }

        /// <summary>The complete reply, exactly as received. This is what goes into history.</summary>
        public string Raw { get; }

        /// <summary>The first-line token as received (trimmed), for diagnostics on Unknown.</summary>
        public string Marker { get; }

        public DispatchedReply(ReplyKind kind, string body, string raw, string marker)
        {
            Kind = kind;
            Body = body ?? string.Empty;
            Raw = raw ?? string.Empty;
            Marker = marker ?? string.Empty;
        }
    }

    /// <summary>
    /// Parses a model reply by its first-line marker. Deterministic: it reads the
    /// marker and switches on it. It does NOT sniff the body for code-like content —
    /// the marker is authoritative, exactly as the response protocol specifies.
    ///
    /// A reply that does not open with one of the three markers is <c>Unknown</c>.
    /// In substage 4b that is a signal about your instructions doc, not a bug to
    /// swallow — show the raw reply and make Unknown visible so you can see the
    /// protocol violation and fix the prompt.
    /// </summary>
    public static class MarkerDispatcher
    {
        public const string GeneratorMarker = "GENERATOR";
        public const string QuestionMarker = "QUESTION";
        public const string CannotMarker = "CANNOT";

        public static DispatchedReply Parse(string reply)
        {
            string raw = reply ?? string.Empty;

            // Tolerate leading blank lines / whitespace before the marker, then take
            // the first line. A stray leading newline should not read as a violation;
            // a wrong or missing marker word still falls through to Unknown.
            string lead = raw.TrimStart('\r', '\n', ' ', '\t');

            int nl = lead.IndexOf('\n');
            string firstLine = nl < 0 ? lead : lead.Substring(0, nl);
            string rest = nl < 0 ? string.Empty : lead.Substring(nl + 1);

            string marker = firstLine.Trim();

            // Ordinal, case-sensitive match against the exact protocol tokens. Strict
            // on purpose: "Generator" or "GENERATOR:" is a protocol slip worth seeing.
            switch (marker)
            {
                case GeneratorMarker:
                    return new DispatchedReply(ReplyKind.Generator, rest.Trim(), raw, marker);
                case QuestionMarker:
                    return new DispatchedReply(ReplyKind.Question, rest.Trim(), raw, marker);
                case CannotMarker:
                    return new DispatchedReply(ReplyKind.Cannot, rest.Trim(), raw, marker);
                default:
                    // Body is the whole reply so the transcript can show everything.
                    return new DispatchedReply(ReplyKind.Unknown, raw, raw, marker);
            }
        }
    }
}
