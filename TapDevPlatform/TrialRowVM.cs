using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;

using TDP.Sessions;
using TDP.Tapping;

namespace TDP.Sessions   // rename to match your project
{
    /// <summary>
    /// One grid row: a view over a TrialEntry. Every column is a computed, read-only
    /// projection of the (frozen) trial numbers EXCEPT Tag, which writes through to the
    /// underlying trial. Tag is not cosmetic — it rides inside the trial to the HTS
    /// (status box + post-hoc grouping), so editing it here changes HTS-side behaviour
    /// and dirties the session. That is exactly why it is the one editable field: it is
    /// the author's own mutable label, while the rest of the row is a view of numbers
    /// you should not be hand-editing.
    /// </summary>
    public sealed class TrialRowVM : INotifyPropertyChanged
    {
        private readonly TrialEntry _entry;

        public TrialRowVM(TrialEntry entry, string sourceName)
        {
            _entry = entry;
            Source = sourceName ?? "";
        }

        public TrialEntry Entry => _entry;
        public Guid GeneratorId => _entry.GeneratorId;
        private TappingTrialDto T => _entry.Trial;

        // --- the one editable column: write-through to the trial ---
        public string Tag
        {
            get => T.Tag ?? "";
            set
            {
                if ((T.Tag ?? "") == (value ?? "")) return;
                T.Tag = value ?? "";
                OnChanged(nameof(Tag));
            }
        }

        // --- everything else: computed, read-only ---
        public string Pacer => T.Pacer;
        public string Response => T.ResponseInstructions;
        public string PacerSummary => SummariseStream(T.PacerIntervals, T.PacerPattern);
        public string DistractorSummary
        {
            get
            {
                var iv = T.DistractorIntervals;
                if (iv == null || iv.Length == 0) return "none";
                return SummariseStream(iv, T.DistractorPattern) + " \u00b7 loops";
            }
        }

        public string LeadIn => Fmt(T.LeadIn);
        public string Offset => Fmt(T.Offset);
        public string Duration =>
            ((T.PacerIntervals == null ? 0.0 : T.PacerIntervals.Sum()) / 1000.0)
                .ToString("0.###", CultureInfo.InvariantCulture) + " s";
        public string Profile => SummariseProfiles();
        public string Source { get; }

        // --- formatting helpers ---

        // A tiled stream shows length × unit-length ("20 × 4"); a non-tiled stream keeps the
        // existing count × interval-value form ("8 × 500" / "8 × 470–530").
        private static string SummariseStream(double[] intervals, double[] pattern)
        {
            if (intervals == null || intervals.Length == 0) return "\u2014";
            if (pattern != null && pattern.Length > 0)
                return $"{intervals.Length} \u00d7 {pattern.Length}";   // stream length × repeating unit
            return SummariseIntervals(intervals);
        }
        private static string Fmt(double v) => v.ToString("0", CultureInfo.InvariantCulture);

        private static string SummariseIntervals(double[] iv)
        {
            if (iv == null || iv.Length == 0) return "—";
            double min = iv.Min(), max = iv.Max();
            string range = (Math.Abs(max - min) < 1e-9)
                ? Fmt(min)
                : $"{Fmt(min)}\u2013{Fmt(max)}";
            return $"{iv.Length} \u00d7 {range} ms";   // e.g. "8 × 500"  or  "8 × 470–530"
        }

        private string SummariseProfiles()
        {
            var profiles = T.ParameterProfiles;
            if (profiles == null || profiles.Count == 0) return "\u2014";

            string One(ParameterProfileDto p)
            {
                // Show the last path segment (e.g. "Frequency_Hz") + a few values.
                string item = p.Item ?? "";
                int dot = item.LastIndexOf('.');
                string shortItem = dot >= 0 && dot < item.Length - 1 ? item.Substring(dot + 1) : item;

                var vals = p.Values ?? new double[0];
                string shown = string.Join("/", vals.Take(3).Select(v => Fmt(v)));
                if (vals.Length > 3) shown += "\u2026";
                return $"{shortItem}: {shown}";
            }

            return profiles.Count == 1 ? One(profiles[0]) : $"{profiles.Count} profiles";
        }

        // --- INotifyPropertyChanged (so BindingList raises ItemChanged -> autosave) ---
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnChanged(string name)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
