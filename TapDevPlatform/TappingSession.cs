using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using TapDevPlatform;
using TDP.Tapping;

namespace TDP.Sessions   // rename to match your project
{
    // ---------------------------------------------------------------------------
    // The saveable unit is a SESSION: the conversation, the generators it produced,
    // and the master trial list accumulated across runs. All three travel together
    // because the provenance link (a trial -> the generator that made it) is only
    // meaningful if that generator is saved alongside it.
    //
    // Everything here is a plain get/set POCO so System.Text.Json round-trips it
    // without ceremony. The HTS-facing trial type (TappingTrialDto) is wrapped, never
    // extended: provenance lives on the wrapper (TrialEntry), so the executor-facing
    // schema stays frozen.
    // ---------------------------------------------------------------------------

    public sealed class TappingSession
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Title { get; set; } = "(untitled)";
        public string Subject { get; set; } = "";
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
        public DateTime ModifiedUtc { get; set; } = DateTime.UtcNow;
        public bool TitleIsUserSet { get; set; } = false;

        public List<PersistedMessage> Conversation { get; set; } = new List<PersistedMessage>();
        public List<GeneratorRun> Generators { get; set; } = new List<GeneratorRun>();
        public List<TrialEntry> MasterList { get; set; } = new List<TrialEntry>();

        public GeneratorRun FindGenerator(Guid id) => Generators.FirstOrDefault(g => g.Id == id);
    }

    /// <summary>
    /// One successful generator run: the code, its seed, and enough to reproduce or
    /// fork it later. Trials link to this by Id — link to the RUN, not the file,
    /// because (code, seed) together define what a given batch of trials descends from.
    /// </summary>
    public sealed class GeneratorRun
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = "generator";
        public string Code { get; set; } = "";
        public long Seed { get; set; }
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// A trial in the master list, plus the id of the generator run that produced it.
    /// The wrapper is where TDP-only concerns live; Trial stays schema-exact.
    /// </summary>
    public sealed class TrialEntry
    {
        public TappingTrialDto Trial { get; set; }
        public Guid GeneratorId { get; set; }
    }

    /// <summary>Storage shape for a conversation turn (ChatMessage has no parameterless ctor).</summary>
    public sealed class PersistedMessage
    {
        public string Role { get; set; }
        public string Content { get; set; }
    }

    /// <summary>Lightweight row for the sessions ListView — no need to fully load to list.</summary>
    public sealed class SessionSummary
    {
        public string FilePath { get; set; }
        public Guid Id { get; set; }
        public string Title { get; set; }
        public DateTime ModifiedUtc { get; set; }
        public int TrialCount { get; set; }
    }

    /// <summary>
    /// Per-user session files on disk: one JSON file per session, in a per-subject
    /// folder. Save is by session Id (stable filename), so autosave overwrites in place.
    /// </summary>
    public static class SessionStore
    {
        // Seam: set this to wherever TDP keeps its data (e.g. under LocalApplicationData
        // or your SharedResources depot). One subfolder per subject beneath it.
        public static string Root { get; set; } =
            Path.Combine(FileLocations.TdpFolder, "Sessions");

        private static readonly JsonSerializerOptions Options =
            new JsonSerializerOptions { WriteIndented = true };

        private static string FolderFor(string subject)
        {
            string safe = string.IsNullOrWhiteSpace(subject) ? "_" : Sanitize(subject);
            string dir = Path.Combine(Root, safe);
            Directory.CreateDirectory(dir);
            return dir;
        }

        private static string Sanitize(string s)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                s = s.Replace(c, '_');
            return s;
        }

        public static void Save(TappingSession session)
        {
            session.ModifiedUtc = DateTime.UtcNow;
            string path = Path.Combine(FolderFor(session.Subject), session.Id + ".json");
            File.WriteAllText(path, JsonSerializer.Serialize(session, Options));
        }

        public static void Delete(SessionSummary summary)
        {
            if (summary?.FilePath != null && File.Exists(summary.FilePath))
                File.Delete(summary.FilePath);
        }

        public static TappingSession Load(string filePath)
            => JsonSerializer.Deserialize<TappingSession>(File.ReadAllText(filePath), Options);

        public static List<SessionSummary> Enumerate(string subject)
        {
            var dir = FolderFor(subject);
            var list = new List<SessionSummary>();
            foreach (var file in Directory.EnumerateFiles(dir, "*.json"))
            {
                try
                {
                    // Few files in practice; a full parse is fine. Swap for an index if it grows.
                    var s = Load(file);
                    list.Add(new SessionSummary
                    {
                        FilePath = file,
                        Id = s.Id,
                        Title = s.Title,
                        ModifiedUtc = s.ModifiedUtc,
                        TrialCount = s.MasterList.Count
                    });
                }
                catch { /* skip an unreadable/partial file rather than fail the list */ }
            }
            return list.OrderByDescending(x => x.ModifiedUtc).ToList();
        }
    }
}
