# Tapping Trial-List Contract

The shared reference for authoring tapping trial lists for the HTS. It defines
what a trial *is*, the exact schema an author must produce, the `+tapping`
library to build it with, and the boundary of what this approach can express.

Every authoring path — writing MATLAB by hand, being taught the ladder, or
describing an experiment to an AI — produces the same artifact against this same
contract: a validated `Tapping.<name>.json` file the HTS plays. This document is
the part they all share.

---

## 1. Concept

### Trials, pacers, and distractors

A **trial** presents two rhythmic stimuli at once:

- a **pacer** — a steady reference the subject taps along to, and
- a **distractor** — a second stream the subject is meant to ignore.

Each stream is a sequence of **elements** (pulses) whose timing is given by an
**interval vector**. "Pacer" and "distractor" are *roles*, not fixed stimuli.
Each trial binds the roles to two physical stimuli, **A** and **B**, via the
`Pacer` field: `Pacer = "A"` means stimulus A is the pacer this trial and B is
the distractor. In practice A and B are configured (in the HTSController, in a
separate config file) as something like "Sound" and "Vibration" — but that
binding is a config concern, not part of the trial. A trial only says which
*role* each of A and B plays.

There is also a third, separate stimulus for the tap-evoked pathway
(`TapEvokedStimulus`); it is *not* part of the A/B role system — see *The
tap-evoked pathway*, below.

### The interval convention

An interval vector of length **N** describes **N elements**. `intervals[i]` is
the gap that follows element *i*, so there is a trailing silent interval after
the final pulse. Element *i* of the pacer sounds at

```
t_i = sum(PacerIntervals[1..i-1])         (element 1 at t = 0)
```

and the pacer's total duration is `sum(PacerIntervals)` — the trailing silence
included. **The pacer defines the trial**: it starts at t = 0 and the trial ends
when the pacer is exhausted.

### LeadIn and Offset (both belong to the distractor)

The pacer starts immediately at t = 0. The **distractor** is delayed, and its
delay is authored as two separate numbers that sum into one:

- **LeadIn** — a stretch during which the subject hears the pacer *alone*, before
  the distractor enters. "Let them settle onto the beat first."
- **Offset** — the distractor's phase relative to the pacer once it does enter.

The distractor's first element sounds at `t = LeadIn + Offset`. Under the hood
these combine into a single distractor start delay; they are kept separate
because they express two different intentions. Neither affects the pacer.

### Looping: the pacer length is authoritative

The pacer length sets the trial's duration. The **distractor** and each
**parameter profile** simply *loop* (or broadcast) over that duration:

- a distractor vector shorter than the pacer repeats until the pacer ends;
- a distractor vector longer than the pacer is truncated at the pacer's end;
- a single value like `[500]` broadcasts to every element.

Lengths need not divide evenly. A non-dividing loop is *legal* — it makes the
distractor drift in phase against the pacer. That is a design choice, not an
error, so the validator does not flag it; the **preview** is where you see
whether a loop does what you meant (see §4).

### The repeating unit is recorded (`PacerPattern`, `DistractorPattern`)

Tiling is lossy. When a stream is built by drawing a short **unit** — say four
intervals — and repeating it to fill the pacer, what lands in `PacerIntervals`
is the flattened result; the unit's length (and, for a *drawn* unit, its values)
cannot be recovered from the flattened vector afterward. Because analysis often
needs the period a stream was built from, each stream records its pre-tile
**unit** alongside its flattened intervals:

- `PacerPattern` — the repeating unit `PacerIntervals` was tiled from.
- `DistractorPattern` — the same, for the distractor.

**Empty means the stream does not repeat.** A stream authored as literal
intervals with no repeat leaves its pattern field empty, and its period is simply
the full interval count. An empty pattern is therefore not missing information —
it is the explicit statement "no shorter repeating unit," whose length is
`numel(...Intervals)`. This is *authored intent captured at tiling time*: it is
not derivable after the fact, which is exactly why it is recorded and not
computed.

The pattern is the **unit you drew, not the sequence you produced** — never set
it by copying or re-measuring the flattened intervals. In the library this is
automatic: `tapping.tilePattern` returns the flattened intervals and the unit
*together* (§3), and `tapping.newTrial` defaults both pattern fields to empty, so
a stream that is never tiled stays correctly non-repeating with no decision
anywhere. A non-dividing unit is legal for the same reason a non-dividing loop
is: it tiles and truncates at the stream's end, drifting in phase — the drift the
preview surfaces (§4).

### Silent tails (`PacerSilentTail`, `DistractorSilentTail`)

A stream's *audibility* and its role in *timing* are separable. Normally the pacer
is audible for its whole length, and that length also defines the trial. A
**silent tail** keeps the length — and the timing — while muting the end: the last
`PacerSilentTail` intervals of the pacer are emitted **silently**. The clock runs
through them unchanged, so the trial is exactly as long as before; only the sound
stops early.

The motivating case is **continuation**: the pacer falls silent while the trial
runs on, measuring how well the subject maintains the beat unaided. Because the
pacer still runs the clock, "the pacer defines the trial" (above) is untouched — a
silent tail simply means part of the pacer makes no sound.

`DistractorSilentTail` does the same for the distractor, and the two are
**independent**: giving the pacer a tail while leaving the distractor's at 0 lets
the distractor *carry on past* the pacer's silence (or the reverse).

Both are **integer counts of intervals**, not times — the number of trailing
intervals to emit silently — and both default to **0** (no tail; fully audible),
so every trial without them is unchanged. The count is over the *flattened*
intervals, so it composes with tiling without interacting: a tail and a
`PacerPattern` are independent quantities. When an experiment is described in
**cycles** ("the last two pattern cycles silent") or as a **duration**, convert it
to an interval count before authoring — one cycle is one pattern-unit length
(`numel(PacerPattern)`, or the full stream length if it does not repeat), and a
duration is however many trailing intervals sum to it.

### The tap-evoked pathway (`TapEvokedStimulus`)

Alongside the pacer/distractor pathway (stimuli A and B) there is a separate
**tap-evoked** pathway with its own stimulus, `TapEvokedStimulus`, configured like
A and B in the Elements config (again a config concern, not part of the trial). It
is switched on per trial by the boolean `TapEvokedAudioEnabled`.

When enabled, it plays a sound **in response to each of the subject's taps** — the
*trigger* is the tap, not a pre-listed time. This is the one place a stream's
*timing* is reactive rather than pre-computed. Everything else about it, though,
**is** pre-computed: which sound, at what level, after what delay — all authored
ahead of time. Only "which tap, when" is decided at runtime.

It is governed by the trial clock like everything else — it responds to taps for
the length of the trial — and, for now, it plays **throughout**, unaffected by
`PacerSilentTail`/`DistractorSilentTail`: during a silent continuation tail the
tap-evoked sound keeps responding, which is often exactly when it matters most.

The tap-evoked sound's properties — including its **delay** (the gap from tap to
sound) — are ordinary stimulus properties of `TapEvokedStimulus`. Set a property
to a fixed value for a constant setting, or vary it tap-by-tap with a
ParameterProfile, exactly as for A and B. **The delay is not special**: a fixed
delay is the default gap; a profile on the delay varies it per tap (jitter is
simply drawn into the profile's values, offline, like any other varying property).
Nothing about latency needs special authoring — it is one more property of the
tap-evoked stimulus.

### Parameter profiles

A **ParameterProfile** varies one stimulus parameter across a stream's
presentations. `Item` names the parameter (a path string) and, through that path,
the **stimulus** it belongs to; `Values` gives the successive values, looped or
broadcast just like the distractor.

Which stimulus the `Item` names decides how the values are **sequenced**, with no
extra flag:

- an `Item` on **A or B** is sequenced **per element** — value *i* applies to
  element *i*, looping over the pacer's elements (the motivating case: varying the
  sound's frequency element by element);
- an `Item` on **`TapEvokedStimulus`** is sequenced **per tap** — value *k*
  applies to the *k*-th tap-evoked sound, looping over the subject's taps.

Same profile shape either way; the target stimulus decides the index. A
tap-indexed profile's length is a **cycle**, not a total: the number of taps is
not known ahead of time, so the values loop over however many taps occur. Size the
cycle to the expected tap count so it reads well; the runtime just loops. All
profiles live in the single `ParameterProfiles` list — routing is purely by
`Item`.

### Interval tables (a pre-computed draw-source)

Researchers may curate an **interval table** — a set of interval rows built
offline to satisfy some criterion — and make it available to a session. It is a
*source of interval vectors*, nothing more: a generator can draw whole rows from
it and feed them to **either** the pacer or the distractor. Drawing happens in
seeded MATLAB via `tapping.loadIntervalTable` / `tapping.drawTableRows` (§3), so
the draw is reproducible and the table's values never pass through the authoring
conversation — the table is announced to the author by *name and shape* only.
Because the drawn numbers land in `PacerIntervals`/`DistractorIntervals` like any
others, a table adds a *source*, not new trial structure: it composes with tiling
(a drawn row can be the repeating unit) and with everything else unchanged.

### Run order

Trials play in the order they appear in the list. **List position is playback
order** — the executor runs top to bottom with no sequencing logic. Any
repetition, blocking, or reordering is expressed by *emitting the trials in the
intended order*, not by a separate instruction.

### A small mental model

A 4-element pacer at 500 ms with a distractor `[500]` and `Offset = 250`,
`LeadIn = 1000`: the pacer ticks at 0, 500, 1000, 1500 ms; the subject hears it
alone for 1000 ms; the distractor then enters at 1250 ms and ticks every 500 ms,
sitting exactly halfway between pacer beats (a steady off-beat). Change the
distractor to `[470]` and it drifts earlier each beat — legal, and visible in the
preview as a sliding phase.

---

## 2. The schema

Top level is a **TappingTrialList**: `{ "Trials": [ ...TappingTrial... ] }`, in
run order. A `Provenance` field (seed, timestamp) may accompany it; the HTS
ignores unknown fields, so it is safe to include and is written automatically.

### TappingTrial

| Field | JSON type | Units | Rules |
|---|---|---|---|
| `Tag` | string | — | Free, **non-unique**, optional. Author's own meaning: shown in the HTSController status box and used to group trials in post-hoc analysis. |
| `Pacer` | string enum | — | `"A"` or `"B"` — which stimulus is the pacer this trial. Authored as **text**, not an integer. |
| `ResponseInstructions` | string enum | — | `"AllElements"` or `"DownbeatOnly"`. Authored as text. |
| `LeadIn` | number | ms | ≥ 0. Pacer-alone stretch before the distractor enters. Applies to the distractor only. |
| `Offset` | number | ms | Distractor phase. `LeadIn + Offset` must be ≥ 0 (the distractor cannot start before t = 0). Applies to the distractor only. |
| `PacerIntervals` | number[] | ms | **Non-empty.** Every value finite and > 0. Length is **authoritative** — it defines the trial. |
| `PacerPattern` | number[] | ms | **May be empty.** Empty = the pacer does not repeat (its period is the `PacerIntervals` length). If present, it is the repeating **unit** `PacerIntervals` was tiled from; every value finite and > 0. Set only by tiling — it is *never* a copy or re-measurement of the flattened `PacerIntervals`. |
| `PacerSilentTail` | integer | count | **≥ 0; default 0.** Number of *trailing* pacer intervals emitted **silently**. The clock still runs through them, so trial length is unchanged — the pacer just stops sounding early (a continuation trial). Counts the *flattened* intervals. `0` = fully audible. |
| `DistractorIntervals` | number[] | ms | May be empty (a pacer-only trial). If present, every value finite and > 0. Loops over the pacer; length is free. |
| `DistractorPattern` | number[] | ms | **May be empty.** Empty when the distractor is *absent*, or present but *non-repeating*. If present, the repeating **unit** `DistractorIntervals` was tiled from; every value finite and > 0. "Absent" vs "present, non-repeating" is told by `DistractorIntervals`, not by this field. |
| `DistractorSilentTail` | integer | count | **≥ 0; default 0.** Number of trailing distractor intervals emitted silently (after looping/truncation to the pacer). Independent of `PacerSilentTail` — lets the distractor stop before, or carry on after, the pacer. `0` = fully audible. |
| `TapEvokedAudioEnabled` | boolean | — | Default **false**. When true, the tap-evoked pathway plays a sound in response to each tap, using `TapEvokedStimulus` (configured in Elements, like A/B). Its properties — including delay — are set or varied by `ParameterProfiles` whose `Item` targets `TapEvokedStimulus`. Plays throughout the trial, unaffected by the silent tails. |
| `ParameterProfiles` | ParameterProfile[] | — | May be empty. Each varies one parameter across a stream's presentations — **per element** for an A/B stimulus, **per tap** for `TapEvokedStimulus`. Routing is by `Item` (see §1). |

### ParameterProfile

| Field | JSON type | Rules |
|---|---|---|
| `Item` | string | Parameter path; it also identifies the **stimulus** it targets (A/B or `TapEvokedStimulus`), which sets the sequencing regime (per element vs per tap). **Controlled vocabulary is deferred** — see below. One known value is `Sound.Tone.Frequency_Hz`. |
| `Values` | number[] | Non-empty, all finite. Successive per-presentation values, looped/broadcast over the elements (A/B) or taps (`TapEvokedStimulus`). `[1000 2000]` alternates; `[440]` broadcasts. Units are whatever the parameter uses (Hz for frequency, ms for a delay). |

### Encoding rules

- **Enums are text** (`"A"`, `"AllElements"`). The reader accepts integers too,
  but text is the authored form — it is self-documenting.
- **Every array field must be a JSON array**, even at length 1: `[500]`, not
  `500`. `writeTrialList` guarantees this; a hand-edited file must preserve the
  brackets. (A collapsed scalar fails loudly in the HTS loader, so it will not
  pass silently — but do not rely on that.) This includes the pattern fields: an
  empty pattern is the empty array `[]`, and it must survive the wire as `[]`
  (not omitted, not `null`) — the empty-means-non-repeating convention rides on
  that.
- **Units are milliseconds** for every interval field (LeadIn, Offset, Pacer,
  Distractor, and both Pattern fields). Profile `Values` use the parameter's own
  units. **Not everything is ms:** the `*SilentTail` fields are integer *interval
  counts*, and `TapEvokedAudioEnabled` is a boolean.
- **Filename is `Tapping.<name>.json`** — capital-T `Tapping.` prefix. This is
  the HTS config-file naming contract; it is deliberately *not* the same as the
  lowercase `+tapping` MATLAB package name. Do not lowercase it.

### The deferred `Item` vocabulary

`Item` is a free string naming a stimulus parameter path, and the mechanism
behind it is fully general — varying AM rate or bandwidth instead of frequency
needs no change to the HTS, only a different `Item`. The cost of that generality
is that *deriving* the correct string is out of scope for this document. To
obtain an `Item` other than the known `Sound.Tone.Frequency_Hz` — including any
property of `TapEvokedStimulus`, such as its delay — read it from the A/B or
`TapEvokedStimulus` stimulus configuration in the HTSController, or ask Ken. Do
not guess it.

---

## 3. The `+tapping` library

Build trial lists by **composing** the `+tapping` primitives. Add the folder
*containing* `+tapping` to the MATLAB path (not `+tapping` itself), then call
functions as `tapping.<name>(...)`.

### Vector primitives — produce or transform interval vectors

- `tapping.drawFromSet(set, n, replace, weights)` — draw `n` values from a set.
  `replace` (default true) and `weights` (default uniform) are optional
  *properties of the draw*.
- `tapping.drawToDuration(set, targetMs, replace, weights)` — draw values until
  their cumulative duration exceeds `targetMs`. The count is an output.
- `tapping.drawSumConstrained(set, n, targetSum, exclude)` — draw `n` values
  summing exactly to `targetSum`, optionally ≠ `exclude`.
- `tapping.tilePattern(unit, targetCount)` — tile a **pre-drawn** `unit` to
  `targetCount` elements, returning **both** the flattened intervals and the
  unit: `[iv, pat] = tapping.tilePattern(unit, n)`. Assign them as a pair
  (`[t.PacerIntervals, t.PacerPattern] = tapping.tilePattern(unit, nPacer)`) so
  the pattern field can never be forgotten. Non-dividing tiling is legal — it
  truncates at `targetCount`, drifting in phase. This is the **only** thing that
  sets a pattern field non-empty.
- `tapping.loadIntervalTable(name)` — load a curated **interval table** (a
  researcher-supplied set of interval rows) available this session, returning an
  `R × C` numeric matrix (`R` rows, each a row of `C` intervals in ms). A second
  output, `[T, info] = ...`, returns identity metadata (`Name`, `Rows`, `Cols`,
  `Hash`) for the provenance stamp — pass it to `writeTrialList` (below). The
  table itself is a session resource; the assistant is told its name and shape in
  the session context and must **not** transcribe its values — this function reads
  it. Which table (if any) is available is a session fact, like the profile
  targets.
- `tapping.drawTableRows(T, n, replace)` — draw `n` whole rows from a table `T`,
  returning an `n × C` matrix in draw order. `replace` (default true) is a
  *property of the draw*; `replace = false` gives `n` **distinct** rows (errors if
  `n` exceeds the table's row count). Seeded like every draw — `rng(seed)` first.
  A drawn row is **role-agnostic**: the *generator* decides whether the result
  feeds the pacer or the distractor. Composition stays in the generator — one row
  played once (`t.PacerIntervals = row`), one row tiled (`tilePattern(row, n)`),
  or `N` rows concatenated (`reshape(rows.', 1, [])`).

### Trial construction

- `tapping.newTrial()` — a blank trial struct with every field defaulted
  (mirrors the C# constructor). Both pattern fields default to empty; both
  silent-tail fields default to 0; `TapEvokedAudioEnabled` defaults to false.
- `tapping.makeProfile(item, values)` — one ParameterProfile. For no parameter profile, 
  assign an empty array — `t.ParameterProfiles = []` — never tapping.makeProfile.empty 
  or any other form.

### Output, gate, inspection

- `tapping.writeTrialList(trials, name, seed [, folder] [, TableInfo=info])` —
  encode and write `Tapping.<name>.json`. Owns the filename contract, the
  provenance stamp, and the array-wrapping discipline, so **generators do not call
  `num2cell`** and assign plain numeric vectors. When a generator drew from an
  interval table, pass the `info` from `loadIntervalTable` as `TableInfo=info`; it
  is stamped into provenance so the record is reproducible against the exact table
  drawn from (same seed on a changed table is a different draw).
- `tapping.validateTrialList(jsonPath)` — structural/sanity gate over a written
  file. Returns `report.ok` and prints every issue.
- `tapping.previewList(src)` — run-scale table, one row per trial (order,
  A/B balance, durations).
- `tapping.previewTrial(trial)` — per-trial view (onset times, jitter, profiles,
  and each stream's repeating unit with its repeat/remainder against the stream).
- `tapping.patternLength(pattern, intervals)` — the period of a stream under the
  empty-means-non-repeating convention: `numel(pattern)` if present, else
  `numel(intervals)`. **Analysis must call this**, not `numel(pattern)` directly
  (which returns 0 for a non-repeating stream and misreads it as length zero).
  This is the one place the empty→full rule lives.

### Authoring principles

**Compose primitives; do not reimplement them.** A generator is a thin *recipe*:
a parameters block, then assembly that calls the library. It should not hand-roll
sampling, encoding, or file writing that the library already provides.

**Rules go in the library; wiring goes in the generator.** The library holds
primitives that *produce or transform a vector*. Relationships *between fields* —
choosing a stream's repeating unit and tiling it to length (via
`tapping.tilePattern`), balancing A/B across trials, setting the distractor equal
to the pacer (`t.DistractorIntervals = pacerIv`), choosing run order — are
composition, and live in the generator. The mechanical tiling is a primitive;
what stays in the generator is *choosing* the unit and the target length and
assigning the returned pair.

**A new need is a new function, not a bent old one.** When an experiment needs
something the primitives do not express, write a *new, clearly-named* rule
function rather than adding a mode flag that switches an existing one's behavior.
The discriminator:

- a new **property of one objective** (with replacement, weighted) → a
  *parameter* on the existing function;
- a new **objective, contract, or failure mode** (draw to a duration, draw with a
  sum guarantee) → a *new function*.

A generator that introduces a new rule function is the normal way the library
grows: the rule is born local, proves itself, and is promoted to `+tapping` after
review.

**Seed before you draw; record the seed.** Call `rng(seed)` before any random
draw, and pass the same `seed` to `writeTrialList`. The generator plus its seed
*is* the reproducible record — the JSON is one draw from it.

**Record the unit you drew, not the sequence you produced.** When a stream is
drawn-and-tiled, capture its repeating unit with `tapping.tilePattern`, which
returns the flattened intervals and the unit together — assign both. When a
stream is authored as literal intervals with no repeat, leave its pattern field
empty (`newTrial` already defaults it so). **Never** set a pattern by copying or
re-measuring the flattened intervals: the pattern is the *unit*, and after tiling
the unit's length is unrecoverable from the flattened vector. This mirrors the
seed discipline — the number that defines the structure is recorded by the step
that knows it, at the moment it knows it.

---

## 4. Two kinds of checking

Two instruments answer two different questions. Keep them distinct.

**`validateTrialList` — is the file well-formed?** A binary gate. It checks the
structural and sanity invariants: pacer present and positive, no NaN/Inf, enums
legal, LeadIn ≥ 0, and so on. A non-empty pattern is checked like any interval
vector (finite, > 0); an empty pattern is always legal — it *is* the
non-repeating case. A silent-tail field is a non-negative integer not exceeding
its stream's length (a tail equal to the full length is legal but silences the
stream entirely — the preview shows it); `TapEvokedAudioEnabled` is a boolean. The
gate deliberately does **not** judge whether the experiment is the one you
intended — a well-formed file that implements the wrong idea passes, and in
particular it does not judge whether a unit tiles the way you meant. Run it on
every file before the HTS sees it.

**The previews — is it the experiment you meant?** Human judgment. `previewList`
shows the run's composition (order, balance, durations); `previewTrial` shows one
trial's structure (onset times, jitter, phase, profiles) — and, for a tiled
stream, the repeating unit with its repeat count and remainder against the
stream, flagging a non-dividing drift; the audible span and any silent tail; and
whether the tap-evoked pathway is enabled and which profiles are tap-indexed. This
is where valid-but-wrong is caught — a looping distractor that drifts, an A/B
imbalance, a frequency ramp that isn't what you pictured, a silent tail longer or
shorter than intended. Nothing is malformed, so only a human looking can catch it.

Neither is ground truth. Both model the HTS's *reading* of the file. The only
ground truth for timing is the recorded WAV and its loopback fiducial.

---

## 5. What this approach can and cannot express

The HTS consumes only the *output vocabulary* — interval vectors, delays, a role
binding, profile values, an enable flag. It has no knowledge of how those numbers
were produced. So a change is **free** (no HTS change, no plumbing) exactly when it
can be expressed as *different numbers in the existing fields*: new pattern rules,
new constraints, new distributions, jitter, balancing, ordering, silent tails,
draws from a curated interval table, and new `Item` parameters all live entirely
in the authoring layer.

**A reactive *trigger* is inside the boundary.** The tap-evoked pathway fires a
sound *in response to* each tap, so its timing is decided at runtime — yet its
content and its delay are pre-drawn values, authored ahead of time. The lesson: a
stream whose *trigger* is the subject, but whose *values* are all pre-computed, is
expressible — enable `TapEvokedAudioEnabled` and author its profiles like any
other.

**The boundary is reached when a pre-computed *value* would have to depend on the
subject's behavior** — a **closed-loop** condition, where an interval or a
property is a *function of the subject's response* to a previous event (e.g. "make
each interval 10% longer than the subject's last tap gap," or "raise the level
until the subject synchronizes"). Those numbers do not exist until runtime and no
generator can produce them, so they cannot be pre-computed into a flat plan. That
is not a new recipe; it is a new HTS capability, and it is outside what this
contract can express.

The distinction is **trigger vs. value**: a reactive *trigger* playing pre-drawn
values is fine (tap-evoked); a *value computed from a response* is not.

The test to apply to any request:

> *Can this be written as pre-computed numbers in the existing fields, or does it
> need the HTS to behave differently? And if something is reactive, is it only the
> trigger — or does a value depend on the response?*

If it is pre-computable (a reactive trigger counts) — it is an authoring task, and
belongs here. If a value depends on the response — recognize it, and say so, rather
than producing a plausible flat plan that silently cannot implement the intent.
