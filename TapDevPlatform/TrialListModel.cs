using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Tapping;

namespace TDP.Tapping   // rename to match your project
{
    // ---------------------------------------------------------------------------
    // TDP-side trial-list DTOs. These mirror the Tapping Trial-List Contract schema
    // exactly (PascalCase field names, text enums, arrays always arrays). They are
    // the controller-side counterpart to the trial types the HTS deserialises.
    //
    // If you already have a controller-side trial type, use THAT here instead of
    // these — the only requirement is that round-tripping through it reproduces
    // HTS-valid JSON. These exist because the controller previously only ever
    // shuttled the trial list as an opaque string; the master list needs typed,
    // per-trial access (durations, Tag editing, subsetting for replay).
    // ---------------------------------------------------------------------------

    public sealed class TappingTrialDto
    {
        public string Tag { get; set; } = "";
        public string Pacer { get; set; } = "A";                 // "A" | "B" (text is the authored form)
        public string ResponseInstructions { get; set; } = "AllElements"; // "AllElements" | "DownbeatOnly"
        public double LeadIn { get; set; }
        public double Offset { get; set; }
        public double[] PacerIntervals { get; set; } = new double[0];
        public double[] DistractorIntervals { get; set; } = new double[0];  // empty = pacer-only
        public double[] PacerPattern { get; set; } = new double[0];      // empty = pacer-only
        public double[] DistractorPattern { get; set; } = new double[0]; // empty = pacer-only
        public List<ParameterProfileDto> ParameterProfiles { get; set; } = new List<ParameterProfileDto>();
        public int PacerSilentTail { get; set; }
        public int DistractorSilentTail { get; set; }
        public bool TapEvokedAudioEnabled { get; set; }
        public int TapEvokedAudioDelay { get; set; }

        // Optional (Contract §2): absent when the stream is not drifted, so null here
        // and omitted on write (the codec writes no nulls).
        public DriftRecord PacerDrift { get; set; }
        public DriftRecord DistractorDrift { get; set; }
    }

    public sealed class ParameterProfileDto
    {
        public string Item { get; set; } = "";
        public double[] Values { get; set; } = new double[0];
    }

    public sealed class TappingTrialListDto
    {
        public List<TappingTrialDto> Trials { get; set; } = new List<TappingTrialDto>();

        // Provenance is optional and the HTS ignores unknown fields. writeTrialList
        // stamps it, so we read it back (seed) but don't require it.
        public ProvenanceDto Provenance { get; set; }
    }

    public sealed class TableStamp
    {
        public string Name { get; set; }
        public int Rows { get; set; }
        public int Cols { get; set; }
        public string Hash { get; set; }
    }

    public sealed class ProvenanceDto
    {
        public long Seed { get; set; }
        [JsonPropertyName("Created")]       // writeTrialList stamps "Created", not "Timestamp"
        public string Timestamp { get; set; }
        public TableStamp SourceTable { get; set; }
        public string Name { get; set; }
    }

    /// <summary>
    /// The one place trial lists are (de)serialised for the HTS. Centralised so the
    /// schema stays exact and lives in a single, checkable spot.
    ///
    /// CAVEAT — align this with your HTS reader. The options below produce PascalCase
    /// names and text enums (both what writeTrialList emits and what the contract
    /// calls the authored form). The contract also says the HTS reader accepts integer
    /// enums, so this is safe either way — but if your HTS parser is strict about any
    /// field, mirror ITS settings here rather than trusting these defaults.
    /// </summary>
    public static class TrialListCodec
    {
        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = null,     // keep PascalCase — matches the schema
            WriteIndented = false,
            // Enums are already modelled as strings on the DTOs, so no converter is
            // needed. Nulls are not written: optional objects (PacerDrift,
            // DistractorDrift, Provenance) and the members of a DriftRecord that do
            // not apply to its method are omitted, as the contract requires. Every
            // array field is initialised to an empty array, so arrays are never null
            // and are always written - an empty pattern still goes out as [].
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        /// <summary>Parse a validated trial-list JSON string into typed trials.</summary>
        public static TappingTrialListDto Deserialize(string json)
            => JsonSerializer.Deserialize<TappingTrialListDto>(json, Options);

        /// <summary>Serialise a trial list to HTS-valid JSON (used for replaying a subset).</summary>
        public static string Serialize(TappingTrialListDto list)
            => JsonSerializer.Serialize(list, Options);

        /// <summary>Build a replay list from an ordered set of trials (no provenance).</summary>
        public static string SerializeSubset(IEnumerable<TappingTrialDto> trials)
            => Serialize(new TappingTrialListDto { Trials = new List<TappingTrialDto>(trials)});
    }

    /// <summary>TDP DTO -> shared Tapping types (game/HTS). Update this when the schema grows.</summary>
    internal static class TrialDtoMapping
    {
        public static TappingTrial ToTappingTrial(this TappingTrialDto d) => new TappingTrial
        {
            Tag = d.Tag ?? "",
            Pacer = ParseEnum<PacerStimulus>(d.Pacer, nameof(d.Pacer)),
            ResponseInstructions = ParseEnum<ResponseInstructions>(d.ResponseInstructions, nameof(d.ResponseInstructions)),
            LeadIn = (float)d.LeadIn,
            Offset = (float)d.Offset,
            PacerIntervals = ToFloat(d.PacerIntervals),
            DistractorIntervals = ToFloat(d.DistractorIntervals),
            PacerPattern = ToFloat(d.PacerPattern),
            DistractorPattern = ToFloat(d.DistractorPattern),
            ParameterProfiles = d.ParameterProfiles?
                                     .Select(p => new ParameterProfile { Item = p.Item, Values = ToFloat(p.Values) })
                                     .ToList() ?? new List<ParameterProfile>(),
            PacerSilentTail = d.PacerSilentTail,
            DistractorSilentTail = d.DistractorSilentTail,
            TapEvokedAudioEnabled = d.TapEvokedAudioEnabled,
            TapEvokedAudioDelay = d.TapEvokedAudioDelay,
            PacerDrift = d.PacerDrift,        // already the shared type
            DistractorDrift = d.DistractorDrift,
        };

        private static float[] ToFloat(double[] v) =>
            v == null ? new float[0] : Array.ConvertAll(v, x => (float)x);

        private static T ParseEnum<T>(string s, string field) where T : struct, Enum =>
            Enum.TryParse(s, ignoreCase: true, out T v) && Enum.IsDefined(typeof(T), v)
                ? v
                : throw new FormatException($"{field}: '{s}' is not a valid {typeof(T).Name}");
    }
}
