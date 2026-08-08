# The Patterns Tab

The Patterns tab is where you create and run tapping patterns. You describe the
experiment you want in plain language, and an AI assistant writes the code that
generates the trials — you run it, hear it, and refine. Where the Elements tab
defines *what* the two stimuli are, the Patterns tab arranges them into trials
and plays them.

The whole tab is built around a short loop: **describe → run → hear → adjust.**

## Describing an experiment (the chat)

Type what you want in plain language — "a 500 ms pacer with a distractor a bit
faster, offset halfway between beats" — and press Send (or Enter). You never
write code yourself; the assistant does that.

The assistant answers in one of three ways:

- **It builds a generator.** You'll see a brief "generator received" — the code
  itself isn't shown because you don't need to read it. It's ready to run.
- **It asks a question.** If something needed is missing or ambiguous, it asks
  rather than guessing.
- **It tells you it can't.** Some requests need the system to behave differently
  at run time — most importantly, changing a stimulus based on the subject's
  response *during* a trial. The assistant will say so plainly instead of
  producing something that looks right but isn't (see *What you can ask for*).

The chat is a conversation: it remembers what you've said within this session, so
you can refine without starting over — "make the pacer faster," "now vary the
frequency," "give the subject longer to settle in first."

## Running a pattern (Generate)

When a generator is ready, click **Generate**. This runs it, checks that the
result is well-formed, and plays it on the HTS so you can hear it.

If something goes wrong — the code errors, or the result fails its check — the
assistant is automatically told what went wrong and corrects it, retrying a few
times. You'll see that back-and-forth in the chat. If it can't fix it after a few
tries, it stops and says so, and you can steer it by hand.

## The trials table

Each successful run adds its trials to the table — one row per trial — and the
newly added rows are highlighted and selected so you can see what just arrived.
The table is your accumulating **master list**: everything you've created in this
session, in order.

The columns summarize each trial — which stimulus is the pacer, the distractor,
how long the trial runs, and which generator produced it. A repeating (tiled)
pattern shows compactly as, for example, "20 × 4" — twenty elements built from a
repeating unit of four.

**Tag** is the one column you can edit. It's your own label for a trial; it shows
in the HTS during a run and is used to group trials when you analyze the data.
You can also delete rows you don't want (right-click, or select and press
Delete).

## Replaying trials (Run)

To play trials you've already made, select one or more rows in the table and
click **Run** — no need to regenerate them. They play in table order. This is how
you re-hear an earlier trial, or play a chosen subset.

## Pacer and distractor

Which stimulus is the **pacer** (the beat the subject taps along to) and which is
the **distractor** is decided *per pattern*, not on the Elements tab. You say it
when you describe the experiment — "make the sound the pacer" — and if you don't,
the assistant will ask. The Elements tab defines what the two stimuli are; the
pattern decides which one leads.

## Varying a property across elements

If you exposed **Profile Targets** on the Elements tab, you can ask the assistant
to vary that property from element to element — "sweep the frequency: 1000, 2000,
3000." Refer to it by the short name you gave it on the Elements tab; the
assistant knows which one you mean. If you didn't expose any, that's fine — a
pattern never requires one.

## Tweaking an earlier generator

Right-click a trial and choose **Tweak this generator** to bring the recipe that
made it back into the conversation, so you can ask for a variation of it rather
than describing a new one from scratch — "like this, but slower."

## Your name and saved chats

Enter your **name** so your work is organized by person (you're also the HTS
subject for your own tests). Past chats are listed alongside the conversation.
Each one remembers both its conversation *and* the trials it produced, so you can:

- **click one to reload it** — conversation and master list come back together;
- **rename it** (press F2, or right-click) — the automatic title is just the
  first thing you said, so a clearer name helps;
- **delete it** (right-click).

**New Chat** starts a fresh session.

## Analysis

Choose a MATLAB **analysis function** from the dropdown. When a run finishes, it
runs automatically on the recorded tapping data, so you get your result without a
separate step.

## What you can — and can't — ask for

Anything that can be written as fixed numbers in the trials is fair game:
patterns, random draws from a set, jitter, varying a property across elements,
balancing, ordering. All of that lives in the generator the assistant writes, and
none of it needs any change to the HTS.

The limit is reached when a request needs the system to *react* during a trial —
above all, **closed-loop** designs, where an interval depends on how the subject
responded to the previous one. Those values don't exist until the moment of
testing, so they can't be written into a pattern ahead of time. The assistant
will recognize this and tell you, rather than hand you a plausible pattern that
quietly doesn't do what you asked.

## How it fits with the Elements tab

The two tabs share one idea: the **Elements** tab is the *vocabulary* — what the
two stimuli are — and the **Patterns** tab is the *composition* — how they're
sequenced into trials and played. Set up Elements first; then come here to build
and run patterns against them.
