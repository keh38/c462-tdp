# Drifting pacers for the tapping task

Reference for adding three tempo-varying ("drifting metronome") pacer primitives to the TDP, and for the TDP assistant when it authors trials that use them. The MATLAB reference implementation is `makePacer.m` in the TapModelSim toolbox. This document defines each method exactly, gives defaults and safe ranges, and explains why the design choices matter for the analysis.

Status (November 2026): methods designed and checked in simulation; very preliminary pilot data look promising. All numbers below marked "simulated" come from TapModelSim with assumed parameters (phase-correction gain α = 0.5, period-correction gain β = 0.1, timer noise σ_T = 15 ms, motor noise σ_M = 5 ms, base interval 500 ms). Replace them with pilot fits when available.

## 1. Why drift the pacer

Pilots with a constant haptic metronome plus a tap-evoked audio distractor suggested that once the tempo is learned, subjects stop attending to the haptic pacer and keep time from the audio of their own taps. With a constant tempo, an internal timekeeper plus self-generated feedback is enough to stay roughly in step.

A pacer whose tempo keeps changing makes the pacer worth attending to. Ignoring it now costs something measurable: the taps drift away from the pacer as its tempo changes. Simulated, weakening correction to 20% of normal roughly doubles the asynchrony SD with a metronome but nearly quadruples it with a random-walk pacer.

The analysis model is linear phase and period correction (Vorberg & Schulze 2002; Schulze, Cordes & Vorberg 2005). With asynchrony A(n) = tap(n) − pacer(n) and pacer interval C(n):

```
P(n)     = P(n-1) - beta  * A(n)                      period correction
A(n+1)   = (1 - alpha) * A(n) + (P(n) + eT(n) - C(n)) + (M(n+1) - M(n))
```

Attention to the pacer shows up as the correction gains α and β. A drifting pacer helps the analysis as well as the task: with a constant metronome, phase and period correction are nearly impossible to tell apart in a fit, and a changing tempo separates them. Simulated, 80-tap segments with a changing tempo halve the spread of the fitted α and reduce the spread of β to about a quarter, compared with a metronome.

## 2. Conventions shared by all three methods

**Canonical output: one interval per beat.** Each method produces a sequence of pacer intervals I(1..N−1) in ms. Onset times are their cumulative sum, starting at 0. Because onsets are a running sum of intervals, the pacer is phase-continuous by construction; no separate phase accumulator is needed.

**Work per beat, not per second.** The tapper corrects once per beat, so holds, ramp speeds and modulation frequencies are defined in beats. A rule written in seconds changes difficulty with tempo (a 3 s hold is 2.5 beats at 50 BPM but 6 beats at 120 BPM).

**Work in log-interval.** Random walk and hold-and-ramp operate on x = log(I / IOI), where IOI is the base interval. Equal steps in x are equal percentage changes. Sum of sines modulates the interval linearly (I = IOI·(1 + m)); for the small modulation depths used, the difference is negligible.

**Bounds.** Keep each trial within about ±10–15% of its base interval. Timer noise grows with the interval and the correction gains likely depend on tempo, so wide excursions within one trial blur the fitted parameters. Treat different base tempos as separate conditions instead.

**Start at the base tempo.** All three methods should begin at exactly IOI, so a drifting segment can follow an isochronous lead-in without a jump. Random walk and hold-and-ramp do this already. Sum of sines does not (see 4.3).

**Reproducibility and logging.** Record, for every trial:
- the method name and all parameter values;
- the random seed;
- the realized interval sequence (or onset times), not just the seed. MATLAB and C# random number generators produce different streams, so the same seed in two languages gives different pacers;
- for hold-and-ramp, the beat indices where each ramp starts and ends, and its direction;
- the pacer onset times as actually delivered by the output device, on the same clock as the taps.

## 3. Method 1: bounded random walk

The tempo changes a little on every beat, unpredictably, and drifts back toward the base over time.

**Definition** (log-interval AR(1) process):

```
x(1) = 0
x(i) = rho * x(i-1) + stepFrac * randn        i = 2 .. N-1
I(i) = IOI * exp(x(i)) * (1 + jitterFrac * randn)
```

| Parameter | Meaning | Default | Suggested range |
| --- | --- | --- | --- |
| `IOI` | base interval, ms | 500 | per condition |
| `stepFrac` | SD of each step in log-interval (≈ fraction of IOI) | 0.02 | 0.01–0.025 |
| `rho` | pull back toward IOI; 1 = pure random walk | 0.95 | 0.9–0.97 |
| `rangeFrac` | hard bound, ± fraction of IOI (not in makePacer.m) | 0.15 | 0.10–0.15 |
| `jitterFrac` | independent jitter on each interval | 0 | 0–0.02 |

**Properties.**
- Long-run SD of the tempo: stepFrac / sqrt(1 − rho²), about 6.4% with the defaults.
- Pull-back time constant: about 1 / (1 − rho) beats, 20 beats with the defaults.
- The AR(1) process has no hard limit, and `makePacer.m` does not clip. With the defaults, excursions past the recommended bounds are common, not rare: simulated, 64% of 200-beat sequences go beyond ±15% at some point, and the largest excursion in an 85-beat segment averages about 14%. A production primitive must enforce the bounds, for example by reflecting the walk at ±log(1 + rangeFrac), and log how often that happened. A smaller step (stepFrac 0.015, long-run SD about 4.8%) also keeps the walk closer to the base.

**Good for:** continuous attention demand on every beat, and precise per-segment fits of α and β.

## 4. Method 2: sum of sines (multisine)

The tempo is modulated by several slow sinusoids with random phases. This is the standard system-identification input: it gives the frequency response of the tapper's tracking loop.

### 4.1 Definition

```
n    = 0 .. N-2                                  (beat index)
amp  = rmsFrac * sqrt(2 / K)                     (K = number of frequencies)
m(n) = sum_k amp * sin(2*pi*f(k)*n + phi(k)),    phi(k) ~ Uniform(0, 2*pi)
I(n) = IOI * (1 + m(n))
```

| Parameter | Meaning | Default | Suggested range |
| --- | --- | --- | --- |
| `IOI` | base interval, ms | 500 | per condition |
| `rmsFrac` | overall RMS modulation as a fraction of IOI | 0.05 | 0.03–0.07 |
| `freqs` | frequencies in cycles per beat | [0.021 0.043 0.087 0.17] | all < 0.5; see 4.2 |

### 4.2 Choosing the frequencies

- The default periods are about 48, 23, 11.5 and 6 beats. Use frequencies that are not integer multiples of each other; harmonic sets repeat and can be learned.
- Keep every frequency below 0.5 cycles per beat (the per-beat Nyquist limit). In practice, components faster than about 0.2 cycles per beat probe phase correction; slower ones probe period correction.
- The worst-case peak modulation is amp·K = rmsFrac·sqrt(2K), about 14% with the defaults, reached only when all components line up. Check realized sequences against the bounds.

### 4.3 Recommended addition (not in makePacer.m)

With random phases, the first interval is generally not IOI, so a drifting segment would start with a jump after an isochronous lead-in. A production primitive should fade the modulation in, for example by multiplying m(n) by a raised-cosine ramp from 0 to 1 over the first 8 beats.

**Good for:** the most precise α estimates per tap in our simulations, and frequency-response analysis (gain and lag of tracking versus modulation frequency).

## 5. Method 3: hold-and-ramp

Plateaus of constant tempo joined by steady ramps to random targets. This is a per-beat version of the PI's "bounded stochastic hold-and-ramp tempo generator".

### 5.1 Definition (per beat, log-interval)

```
L       = log(1 + rangeFrac)            bounds: x in [-L, L]
minStep = log(1 + minStepFrac)
step    = log(1 + rampFrac)
cur = 0
repeat until N-1 intervals are filled:
    hold:   h = holdMin + G,  G ~ Geometric(mean holdExtraMean)
            x = cur for h beats
    target: tgt ~ Uniform(-L, L), redrawn until |tgt - cur| >= minStep
    ramp:   nR = ceil(|tgt - cur| / step)
            x(j) = cur + (tgt - cur) * j / nR,   j = 1 .. nR
    cur = tgt
I = IOI * exp(x)
```

The geometric extra hold is the per-beat counterpart of the shifted exponential in the PI's spec. Its hazard is constant: after `holdMin` beats, a ramp is equally likely to start on every beat, so ramp onsets cannot be anticipated.

| Parameter | Meaning | Default | Suggested range |
| --- | --- | --- | --- |
| `IOI` | base interval, ms | 500 | per condition |
| `rangeFrac` | bound on excursions, ± fraction of IOI | 0.12 | 0.10–0.15 |
| `holdMin` | minimum hold, beats | 6 | 4–8 |
| `holdExtraMean` | mean of the geometric extra hold, beats | 8 | 4–12 |
| `rampFrac` | interval change per beat on a ramp | 0.01 | 0.005–0.02 for modeling |
| `minStepFrac` | minimum change between successive targets | 0.06 | 0.04–0.10 |

### 5.2 Properties and predictions

- Mean hold: holdMin + holdExtraMean beats (14 with the defaults). Ramps last 6–24 beats with the defaults.
- On a long enough ramp, the linear model predicts that the tapper settles at a steady lag of (change per beat in ms) / β behind the pacer: 0.01 × 500 / 0.1 = 50 ms with the defaults. The tapper is late when the tempo speeds up and early when it slows. Simulated, this holds exactly once the ramp is long enough for the loop to settle. A tapper who extrapolates the tempo change lags less than this, so ramps test prediction against reactive correction.
- Ramp onsets and ends are events that can be averaged across (event-locked analysis), and compared with the response predicted from the fitted α and β.

### 5.3 Notes

- **No maximum hold.** Capping hold duration, or truncating the distribution, breaks the constant hazard (the chance of a change rises toward the cap). If the game needs a cap for engagement, accept that trade-off knowingly.
- **Ramp speed.** Ramps steeper than about 2% per beat drive asynchronies far outside normal tapping variability (about 20–35 ms SD). People then probably jump deliberately rather than correct gradually, and the linear model no longer describes the response. Steep ramps may still suit the training game or a study of change detection.
- **Converting from BPM per second.** For a spec written in tempo r (BPM) and slope s (BPM per second), the fractional interval change per beat is about 60·s / r². At 85 BPM, 5 BPM/s is about 4% per beat. Simulated from 75 to 95 BPM with the assumed gains, 5 BPM/s gives peak asynchronies of about 200 ms; 1 BPM/s (about 1% per beat) gives about 67 ms.
- **Log the ramps.** Record each ramp's start beat, end beat and direction from the generator. `findRamps.m` can recover them from onset times, but rounding of logged onsets to 1 ms is close to its detection threshold at a 500 ms base interval.

**Good for:** settle-then-update structure (the PI's aim), discrete events for event-locked analysis, and a direct test of anticipation. During holds the pacer is a metronome again, so the attention demand is concentrated at the changes.

## 6. Choosing a method

| | Random walk | Sum of sines | Hold-and-ramp |
| --- | --- | --- | --- |
| Attention demand | every beat | every beat | mainly at changes |
| Predictability | low | low (non-harmonic frequencies) | onsets unpredictable; ramp continuation predictable |
| Spread of fitted α (simulated, 80 taps) | 0.058 | 0.044 | 0.068 |
| Spread of fitted β (simulated, 80 taps) | 0.014 | 0.011 | 0.009 |
| Largest excursion in 85 beats, defaults (mean) | ~14% (unbounded) | ~13% | 10% |
| Events to average | no | no | yes (ramp onsets and ends) |
| Distinctive analysis | per-segment fits | frequency response | ramp lag vs slope/β; anticipation |

Spreads are SDs of single-segment estimates around true values α = 0.5 and β = 0.1. For comparison, an isochronous metronome gives 0.136 for α and 0.048 for β, and biased estimates (α 0.44, β 0.14). For a pilot that compares distractor conditions, sum of sines gives the most information per tap, with random walk close behind. Hold-and-ramp is the choice when the question is how quickly and accurately people notice and follow a change.

## 7. Trial design guidance

- **Lead-in.** Start each trial with an isochronous lead-in at the base tempo (about 8–10 beats), then switch to the drifting pacer, which starts at the same tempo.
- **Segments.** Analyze continuous stretches of 60–90 taps under one condition, discarding the first 5 taps of each (start-up or transition). Simulated, a 30% drop in both gains is detectable in one subject with about 4 segments of 60 taps per condition, or 2–3 of 90. Smaller effects need more: halving the effect roughly quadruples the segments. Use at least 3–4 segments per condition regardless, so segment-to-segment variability can be estimated.
- **Ordering.** Alternate or counterbalance conditions (ABAB…) across a session.
- **Frozen sequences.** Pre-generate a set of pacer sequences and use the same set in every distractor condition, so the comparison is paired on the same pacer. Use several sequences, so subjects cannot learn one.
- **Hold-and-ramp sessions** need more time for event-locked comparisons: about 25 ramps per condition for the same 30% gain effect (simulated), versus a few minutes of tapping for the fits.
- **Resize after the pilot.** Put pilot-fitted α, β, σ_T and σ_M into TapModelSim `demo5_WanderingPacer.m`, Section 5, and rerun it to set segment counts for the main study.

## 8. Guardrails for the trial-authoring assistant

- Generate intervals per beat; derive onsets by cumulative sum.
- Keep realized intervals within the stated bounds; check every generated sequence before saving it. For random walk this means enforcing the bound in the generator, since the default walk often exceeds ±15%.
- Default to the parameter values in the tables above. Treat values outside the suggested ranges as deliberate choices that should be confirmed.
- Do not exceed about 2% interval change per beat in hold-and-ramp, or 0.5 cycles per beat in sum of sines, unless the trial is explicitly for training or change detection rather than model fitting.
- Use non-harmonic frequency sets for sum of sines, and fade the modulation in after the lead-in.
- Do not cap hold durations unless asked; if asked, note that it makes ramp onsets more predictable.
- Store the seed, the parameters and the realized sequence with every trial, plus ramp event indices for hold-and-ramp.
- Keep the base tempo constant within a trial; vary it across trials or conditions.

## 9. Analysis pointers (TapModelSim)

- `pairTaps.m` matches taps to pacer onsets; `analyzeSegments.m` fits every segment and summarizes by condition; `demo6_AnalyzeSession.m` is the template.
- Fit phase and period correction together (`fitBGLS.m`). A phase-only fit on drifting-pacer data reports roughly α + β.
- With short segments, fitted σ_T reads about 10% low; the bias is the same in every condition, so differences are fair.
- The fits cannot separate sensory noise from timer noise within one condition. A design that varies the pacer's salience (for example vibration amplitude) can.
- For hold-and-ramp, `findRamps.m` and `eventLocked.m` produce ramp-locked averages; `demo7_HoldAndRamp.m` compares them with the response predicted from the fits.
- The fitters are teaching implementations of bGLS (Jacoby et al. 2015). For published analyses, compare with the authors' LIBbGLS package (https://norijacoby.com/bgls.htm).

## 10. Background and references

Sensorimotor synchronization has been modeled as linear error correction for about 50 years. Wing & Kristofferson (1973) separated timer and motor noise in self-paced tapping. Vorberg & Wing (1996) and Vorberg & Schulze (2002) added phase correction toward a pacer, and Schulze, Cordes & Vorberg (2005) added period correction, tested with tempo ramps. Jacoby et al. (2015) gave the standard fitting method (bGLS) and showed why the parameters trade off against each other in short runs.

Tempo-varying pacers have a long history. Michon (1967) used sinusoidal tempo modulation to identify the tapper's transfer function. van der Steen et al. (2015) fitted correction and anticipation models to continuously tempo-changing sequences, finding that synchronization worsens as tempo changes become faster and reverse direction more often. Pecenka & Keller (2011) used tempo-changing sequences to measure whether people anticipate or follow tempo changes. Stephen et al. (2008) and Rankin, Large & Fink (2009) showed that people pick up and exploit long-range structure in irregular pacers.

The constant-hazard hold durations come from the reaction-time literature: an exponential-like foreperiod distribution keeps people from anticipating when the next change will come (Niemi & Näätänen 1981).

- Jacoby, N., Tishby, N., Repp, B. H., Ahissar, M., & Keller, P. E. (2015). [Parameter estimation of linear sensorimotor synchronization models: Phase correction, period correction, and ensemble synchronization](https://elsc.huji.ac.il/?p=10670). *Timing & Time Perception, 3*(1–2), 52–87.
- Michon, J. A. (1967). *Timing in temporal tracking*. Soesterberg: Institute for Perception RVO-TNO.
- Niemi, P., & Näätänen, R. (1981). Foreperiod and simple reaction time. *Psychological Bulletin, 89*, 133–162.
- Pecenka, N., & Keller, P. E. (2011). [The role of temporal prediction abilities in interpersonal sensorimotor synchronization](https://link.springer.com/doi/10.1007/s00221-011-2616-0). *Experimental Brain Research, 211*.
- Rankin, S. K., Large, E. W., & Fink, P. W. (2009). [Fractal tempo fluctuation and pulse prediction](https://labs.sonicfield.org/library/fractal-tempo-fluctuation-and-pulse-prediction). *Music Perception, 26*.
- Repp, B. H. (2005). Sensorimotor synchronization: A review of the tapping literature. *Psychonomic Bulletin & Review, 12*(6), 969–992.
- Repp, B. H., & Su, Y.-H. (2013). [Sensorimotor synchronization: A review of recent research (2006–2012)](https://doi.org/10.3758/s13423-012-0371-2). *Psychonomic Bulletin & Review, 20*(3), 403–452.
- Schulze, H.-H., Cordes, A., & Vorberg, D. (2005). Keeping synchrony while tempo changes: Accelerando and ritardando. *Music Perception, 22*, 461–477.
- Stephen, D. G., Stepp, N., Dixon, J. A., & Turvey, M. T. (2008). [Strong anticipation: Sensitivity to long-range correlations in synchronization behavior](https://ideas.repec.org/a/eee/phsmap/v387y2008i21p5271-5278.html). *Physica A, 387*(21), 5271–5278.
- van der Steen, M. C., Jacoby, N., Fairhurst, M. T., & Keller, P. E. (2015). [Sensorimotor synchronization with tempo-changing auditory sequences](https://fis.tu-dresden.de/portal/de/publications/sensorimotor-synchronization-with-tempochanging-auditory-sequences(f3be4d93-224d-4dbf-b231-9ad09a119643).html). *Brain Research, 1626*.
- Vorberg, D., & Schulze, H.-H. (2002). [Linear phase-correction in synchronization: Predictions, parameter estimation, and simulations](https://www.semanticscholar.org/paper/Linear-phase-correction-in-synchronization:-and-Vorberg-Schulze/b49c98f04450b6d272326ac43ea0da85a895e47d). *Journal of Mathematical Psychology, 46*(1), 56–87.
- Wing, A. M., & Kristofferson, A. B. (1973). [Response delays and the timing of discrete motor responses](https://doi.org/10.3758/BF03198607). *Perception & Psychophysics, 14*, 5–12.
