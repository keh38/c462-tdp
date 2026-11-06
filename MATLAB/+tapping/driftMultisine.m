function [intervals, drift] = driftMultisine(baseInterval, opts)
%DRIFTMULTISINE  Drifting stream whose interval is modulated by a sum of sines.
%   [iv, drift] = tapping.driftMultisine(baseInterval, Intervals=n)
%   [iv, drift] = tapping.driftMultisine(baseInterval, DurationMs=t)
%   [iv, drift] = tapping.driftMultisine(..., RmsFrac=0.05, Freqs=[...], ...)
%
%   The interval is modulated by several slow sinusoids with random phases - the
%   standard system-identification input, giving the frequency response of the
%   tapper's tracking loop. With n = 0, 1, 2, ... counting the drift intervals
%   (n = 0 is the first interval after the steady lead-in) and K = numel(Freqs):
%
%       amp  = RmsFrac * sqrt(2/K)                  (overall RMS = RmsFrac)
%       m(n) = sum_k amp * sin(2*pi*Freqs(k)*n + Phases(k)),  Phases ~ U(0, 2*pi)
%       w(n) = 0.5*(1 - cos(pi*n/FadeIntervals))  for n < FadeIntervals, else 1
%       I(n) = baseInterval * exp(w(n) * m(n))
%
%   The modulation is applied in LOG-interval, like the other drift primitives,
%   so the bound is symmetric in ratio. (For these depths the difference from
%   the linear form baseInterval*(1 + m) is negligible.) The raised-cosine fade
%   makes the first drift interval exactly BASEINTERVAL and brings the
%   modulation in gradually, so there is no jump after the steady lead-in.
%
%   BOUNDS ARE CHECKED, NOT ENFORCED. Every realized interval must lie in
%   [baseInterval/(1+RangeFrac), baseInterval*(1+RangeFrac)]; if one does not,
%   this function errors rather than distort the sinusoids. The worst-case peak
%   is RmsFrac*sqrt(2K) (about 14% with the defaults, only when every component
%   lines up). On an error: lower RmsFrac, raise RangeFrac, or draw again.
%
%   Inputs (all intervals in ms):
%     baseInterval     base interval, ms (> 0)
%   Length - exactly one, both counting the steady lead-in:
%     Intervals        total number of intervals (= elements) in the stream
%     DurationMs       generate until the cumulative duration reaches/passes this
%   Name-Value (default; suggested range):
%     RmsFrac          overall RMS modulation, fraction of the base (0.05; 0.03-0.07)
%     Freqs            frequencies in cycles per interval, each in (0, 0.5)
%                      ([0.021 0.043 0.087 0.17], periods ~48, 23, 11.5, 6
%                      intervals). Use non-harmonic sets; harmonic sets repeat
%                      and can be learned. Components above ~0.2 probe phase
%                      correction, slower ones period correction.
%     RangeFrac        bound, +/- (0.15; 0.10-0.15)
%     FadeIntervals    raised-cosine fade-in length (8; 0 = no fade)
%     SteadyIntervals  isochronous lead-in, counted in the length (8)
%
%   Outputs:
%     intervals  1 x n row vector, ms. Non-repeating: leave the pattern field empty.
%     drift      the drift record. Assign the pair together:
%                  [t.PacerIntervals, t.PacerDrift] = tapping.driftMultisine(500, Intervals=90);
%                Fields: Method ("Multisine"), BaseInterval, SteadyIntervals,
%                Params (RmsFrac, Freqs, RangeFrac, FadeIntervals), Phases
%                (radians, one per frequency; n counted from the first drift
%                interval), so the modulation can be reconstructed exactly.
%
%   Draws use MATLAB's global rng (the K phases, drawn first): call rng(seed) in
%   the generator first. See also tapping.driftRandomWalk, tapping.driftHoldRamp.

    arguments
        baseInterval (1,1) double {mustBeFinite, mustBePositive}
        opts.Intervals = []
        opts.DurationMs = []
        opts.RmsFrac (1,1) double {mustBeFinite, mustBePositive} = 0.05
        opts.Freqs double {mustBeVector, mustBeFinite} = [0.021 0.043 0.087 0.17]
        opts.RangeFrac (1,1) double {mustBeFinite, mustBePositive} = 0.15
        opts.FadeIntervals (1,1) double {mustBeInteger, mustBeNonnegative} = 8
        opts.SteadyIntervals (1,1) double {mustBeInteger, mustBeNonnegative} = 8
    end

    freqs = double(opts.Freqs(:)).';
    if isempty(freqs) || any(freqs <= 0) || any(freqs >= 0.5)
        error('tapping:driftMultisine:badFreqs', ...
              ['Freqs must be non-empty, in cycles per interval, each in (0, 0.5) ', ...
               '(the per-interval Nyquist limit).']);
    end
    spec = driftLengthSpec(baseInterval, opts.SteadyIntervals, ...
                           opts.Intervals, opts.DurationMs, 'driftMultisine');

    K      = numel(freqs);
    amp    = opts.RmsFrac * sqrt(2 / K);
    phases = 2 * pi * rand(1, K);
    F      = opts.FadeIntervals;
    L      = log(1 + opts.RangeFrac);

    intervals = repmat(baseInterval, 1, opts.SteadyIntervals);   % isochronous lead-in
    n = 0;  peak = 0;  peakAt = 0;
    while driftNeedMore(intervals, spec)
        if n < F
            w = 0.5 * (1 - cos(pi * n / F));
        else
            w = 1;
        end
        x = w * amp * sum(sin(2 * pi * freqs * n + phases));
        intervals(end+1) = baseInterval * exp(x);          %#ok<AGROW>
        if abs(x) > peak, peak = abs(x); peakAt = numel(intervals); end
        n = n + 1;
    end

    if peak > L * (1 + 1e-12)
        error('tapping:driftMultisine:outOfBounds', ...
              ['realized intervals span [%.1f, %.1f] ms; the bounds for RangeFrac = %g ', ...
               'are [%.1f, %.1f] ms (largest excursion at interval %d). Lower RmsFrac, ', ...
               'raise RangeFrac, or draw again.'], ...
              min(intervals), max(intervals), opts.RangeFrac, ...
              baseInterval / (1 + opts.RangeFrac), baseInterval * (1 + opts.RangeFrac), peakAt);
    end

    drift = struct( ...
        'Method',          'Multisine', ...
        'BaseInterval',    baseInterval, ...
        'SteadyIntervals', opts.SteadyIntervals, ...
        'Params',          struct('RmsFrac',       opts.RmsFrac, ...
                                  'Freqs',         freqs, ...
                                  'RangeFrac',     opts.RangeFrac, ...
                                  'FadeIntervals', opts.FadeIntervals), ...
        'Phases',          phases);
end
