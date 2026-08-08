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

### Parameter profiles

A **ParameterProfile** varies one stimulus parameter across elements — the
motivating case is varying the Sound's frequency element by element. `Item` names
the parameter (a path string); `Values` gives the per-element values, looped or
broadcast over the elements exactly like the distractor.

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
| `DistractorIntervals` | number[] | ms | May be empty (a pacer-only trial). If present, every value finite and > 0. Loops over the pacer; length is free. |
| `DistractorPattern` | number[] | ms | **May be empty.** Empty when the distractor is *absent*, or present but *non-repeating*. If present, the repeating **unit** `DistractorIntervals` was tiled from; every value finite and > 0. "Absent" vs "present, non-repeating" is told by `DistractorIntervals`, not by this field. |
| `ParameterProfiles` | ParameterProfile[] | — | May be empty. Each profile varies one parameter across elements. |

### ParameterProfile

| Field | JSON type | Rules |
|---|---|---|
| `Item` | string | Parameter path. **Controlled vocabulary is deferred** — see below. Today's one known value is `Sound.Tone.Frequency_Hz`. |
| `Values` | number[] | Non-empty, all finite. Per-element values, looped/broadcast over the elements. `[1000 2000]` alternates; `[440]` broadcasts. Units are whatever the parameter uses (Hz for frequency). |

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
  units.
- **Filename is `Tapping.<name>.json`** — capital-T `Tapping.` prefix. This is
  the HTS config-file naming contract; it is deliberately *not* the same as the
  lowercase `+tapping` MATLAB package name. Do not lowercase it.

### The deferred `Item` vocabulary

`Item` is a free string naming a stimulus parameter path, and the mechanism
behind it is fully general — varying AM rate or bandwidth instead of frequency
needs no change to the HTS, only a different `Item`. The cost of that generality
is that *deriving* the correct string is out of scope for this document. To
obtain an `Item` other than the known `Sound.Tone.Frequency_Hz`, read it from the
A/B stimulus configuration in the HTSController, or ask Ken. Do not guess it.

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

### Trial construction

- `tapping.newTrial()` — a blank trial struct with every field defaulted
  (mirrors the C# constructor). Both pattern fields default to empty; that
  default *is* the non-repeating sentinel.
- `tapping.makeProfile(item, values)` — one ParameterProfile. For no parameter profile, 
  assign an empty array — `t.ParameterProfiles = []` — never tapping.makeProfile.empty 
  or any other form.

### Output, gate, inspection

- `tapping.writeTrialList(trials, name, seed [, folder])` — encode and write
  `Tapping.<name>.json`. Owns the filename contract, the provenance stamp, and
  the array-wrapping discipline, so **generators do not call `num2cell`** and
  assign plain numeric vectors.
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
non-repeating case. The gate deliberately does **not** judge whether the
experiment is the one you intended — a well-formed file that implements the wrong
idea passes, and in particular it does not judge whether a unit tiles the way you
meant. Run it on every file before the HTS sees it.

**The previews — is it the experiment you meant?** Human judgment. `previewList`
shows the run's composition (order, balance, durations); `previewTrial` shows one
trial's structure (onset times, jitter, phase, profiles) — and, for a tiled
stream, the repeating unit with its repeat count and remainder against the
stream, flagging a non-dividing drift. This is where valid-but-wrong is caught —
a looping distractor that drifts, an A/B imbalance, a frequency ramp that isn't
what you pictured, a pattern unit that doesn't tile as intended. Nothing is
malformed, so only a human looking can catch it.

Neither is ground truth. Both model the HTS's *reading* of the file. The only
ground truth for timing is the recorded WAV and its loopback fiducial.

---

## 5. What this approach can and cannot express

The HTS consumes only the *output vocabulary* — interval vectors, delays, a role
binding, profile values. It has no knowledge of how those numbers were produced.
So a change is **free** (no HTS change, no plumbing) exactly when it can be
expressed as *different numbers in the existing fields*: new pattern rules, new
constraints, new distributions, jitter, balancing, ordering, and new `Item`
parameters all live entirely in the authoring layer.

The **boundary** is reached when an experiment needs the HTS to *do something new
with the numbers* rather than to play different numbers. The clearest case is a
**closed-loop** condition — where an interval depends on the subject's response
to a previous one. Those numbers do not exist until runtime, so they cannot be
pre-computed into a flat plan. That is not a new recipe; it is a new HTS
capability, and it is outside what this contract can express.

The test to apply to any request:

> *Can this be written as pre-computed numbers in the existing fields, or does it
> need the HTS to behave differently?*

If the former — it is an authoring task, and belongs here. If the latter —
recognize it, and say so, rather than producing a plausible flat plan that
silently cannot implement the intent.
