# The Elements Tab

The Elements tab defines the two stimuli a tapping pattern is built from. Every
element (pulse) in a pattern is one of these two stimuli; the Patterns tab then
arranges them in time. Think of the Elements tab as answering *what the two
stimuli are*, and the Patterns tab as answering *how they are played*.

## A and B: the two stimuli

There are two stimuli, **A** and **B** — for example a sound and a vibration.
You configure each of them here.

Which stimulus is the **pacer** (the steady reference the subject taps along to)
and which is the **distractor** (the stream to ignore) is **not** set on this
tab. That is a property of each individual pattern, chosen on the Patterns tab,
because the same pair of stimuli can swap roles from one trial to the next. Here
you only say what A and B *are*; a pattern later says which one leads.

## Name

Each stimulus has a **Name**. The HTS uses it to tell the subject which stimulus
to attend to during a run — so give each a clear, subject-facing label (e.g.
"Sound", "Vibration") rather than an internal code.

## Property Bindings

A **Property Binding** ties one property of a stimulus — its level, its
frequency, and so on — to a **mathematical expression** rather than a fixed
number, so its value can be *computed* instead of typed in. The expression may
reference **subject-specific metrics** such as a threshold or a best frequency,
and is **evaluated at run time, in the HTS, for the current subject**.

This is what lets a single Elements configuration adapt to each person: the
config holds no subject's numbers, only the rules for computing them, so the same
setup presents a level relative to *this* subject's threshold, or a tone at
*that* subject's best frequency, without editing the config per person.

You don't need to worry about writing those expressions here — the point on this
tab is simply to know the mechanism exists and to choose which property it acts
on. You pick that property from a **list of the stimulus's available
properties**; you never type its name. The entries are named descriptively, so
you can recognize the one you want and select it.

## Profile Targets

A **Profile Target** exposes a stimulus property that the **AI assistant** is
allowed to vary across elements — that is, to build a *parameter profile*, such
as sweeping frequency element by element within a trial.

Each target has two fields:

- **Item** — the property as the **HTS** knows it. You choose it from the **same
  offered list** of available properties; you do not construct the name
  yourself. The entries are descriptive enough to recognize what you're after
  (a frequency, a level, and so on), and because the Item comes straight from the
  list, it is always the exact name the HTS expects — there is no way to get it
  wrong.
- **ShortName** — a short, convenient handle you give the property for **talking
  to the assistant** (e.g. `frequency`). Once you've picked the property from the
  list, the ShortName is all you need to remember: you say "vary frequency as
  follows," and the assistant maps that short name back to the correct Item for
  you.

Profile Targets are used **only** by the assistant, and they are **optional**. If
you expose none, the assistant simply won't create parameter profiles — a pattern
never requires one. Expose a property here only when you want the option to vary
it across elements.

## Bindings vs. Profile Targets

These two mechanisms both concern stimulus properties but do different jobs, and
it's worth keeping them straight:

- a **Binding** sets a property's *value* (possibly per subject) at run time;
- a **Profile Target** exposes a property so the assistant can *vary it across
  elements* within a trial.

They are independent: a property can be exposed as a Profile Target whether or
not it also has a Binding. In both cases you select the property the same way —
by recognizing and picking it from the offered list — so you never have to know
or type the underlying HTS name.

## Configuration files

The open / save / new controls read and write the **same configuration files the
HTS tapping scene uses**. Defining A and B here and saving the configuration is
what makes those stimuli available for a run; the Patterns tab and the HTS both
work against the configuration you set up on this tab.

## Where this fits

A typical order of work: configure A and B on the **Elements** tab first (names,
bindings, and any profile targets), then move to the **Patterns** tab to author
trials that reference those stimuli by role. The two tabs share one idea — the
Elements tab is the *vocabulary* (what the stimuli are), the Patterns tab is the
*composition* (how they're sequenced into a trial).
