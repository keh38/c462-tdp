function [intervals, drift] = driftHoldRamp(baseInterval, opts)
%DRIFTHOLDRAMP  Drifting stream of constant-interval plateaus joined by steady ramps.
%   [iv, drift] = tapping.driftHoldRamp(baseInterval, Intervals=n)
%   [iv, drift] = tapping.driftHoldRamp(baseInterval, DurationMs=t)
%   [iv, drift] = tapping.driftHoldRamp(..., RangeFrac=0.12, HoldMin=6, ...)
%
%   A bounded stochastic hold-and-ramp generator, specified per interval, in
%   log-interval x = log(I / baseInterval), bounds x in [-L, L], L = log(1+RangeFrac):
%
%     1. HOLD the current interval for HoldMin + G intervals, G geometric with
%        mean HoldExtraMean. After HoldMin intervals a ramp is equally likely to
%        start on every interval (constant hazard), so ramp onsets cannot be
%        anticipated. There is deliberately no maximum hold: a cap would raise
%        the hazard toward the cap and make onsets predictable.
%     2. Draw a TARGET uniformly in [-L, L], redrawn until it differs from the
%        current value by at least log(1+MinStepFrac).
%     3. RAMP to it in nR = ceil(|target - current| / log(1+RampFrac)) equal log
%        steps (about RampFrac change per interval), then hold again.
%
%   The drift starts with a hold at BASEINTERVAL, after the SteadyIntervals
%   isochronous lead-in, so the stream is steady for SteadyIntervals + the first
%   hold (at least HoldMin) before the first ramp. Every interval lies in
%   [baseInterval/(1+RangeFrac), baseInterval*(1+RangeFrac)] by construction.
%
%   On a long ramp the linear model predicts a steady lag of about
%   RampFrac*baseInterval/beta ms behind the pacer (50 ms at 500 ms, beta = 0.1).
%   Ramps steeper than ~0.02 per interval drive asynchronies outside normal
%   tapping variability; keep RampFrac <= 0.02 for model fitting.
%
%   Inputs (all intervals in ms):
%     baseInterval     base interval, ms (> 0)
%   Length - exactly one, both counting the steady lead-in:
%     Intervals        total number of intervals (= elements) in the stream
%     DurationMs       generate until the cumulative duration reaches/passes this
%   Name-Value (default; suggested range):
%     RangeFrac        bound on excursions, +/- (0.12; 0.10-0.15)
%     HoldMin          minimum hold, intervals (6; 4-8)
%     HoldExtraMean    mean of the geometric extra hold, intervals (8; 4-12)
%     RampFrac         interval change per interval on a ramp (0.01; 0.005-0.02)
%     MinStepFrac      minimum change between successive targets (0.06; 0.04-0.10)
%     SteadyIntervals  isochronous lead-in, counted in the length (8)
%
%   Outputs:
%     intervals  1 x n row vector, ms. Non-repeating: leave the pattern field empty.
%     drift      the drift record. Assign the pair together:
%                  [t.PacerIntervals, t.PacerDrift] = tapping.driftHoldRamp(500, Intervals=120);
%                Fields: Method ("HoldRamp"), BaseInterval, SteadyIntervals,
%                Params (RangeFrac, HoldMin, HoldExtraMean, RampFrac, MinStepFrac),
%                Ramps - one entry per ramp, in order:
%                  Start, End   1-based indices into INTERVALS of the ramp's first
%                               and last interval (the last reaches the target)
%                  Direction    "Faster" (intervals shrink) or "Slower"
%                  Truncated    true if the stream ended before the ramp finished
%
%   Draws use MATLAB's global rng: call rng(seed) in the generator first.
%   See also tapping.driftRandomWalk, tapping.driftMultisine.

    arguments
        baseInterval (1,1) double {mustBeFinite, mustBePositive}
        opts.Intervals = []
        opts.DurationMs = []
        opts.RangeFrac (1,1) double {mustBeFinite, mustBePositive} = 0.12
        opts.HoldMin (1,1) double {mustBeInteger, mustBeNonnegative} = 6
        opts.HoldExtraMean (1,1) double {mustBeFinite, mustBeNonnegative} = 8
        opts.RampFrac (1,1) double {mustBeFinite, mustBePositive} = 0.01
        opts.MinStepFrac (1,1) double {mustBeFinite, mustBePositive} = 0.06
        opts.SteadyIntervals (1,1) double {mustBeInteger, mustBeNonnegative} = 8
    end

    L       = log(1 + opts.RangeFrac);
    minStep = log(1 + opts.MinStepFrac);
    step    = log(1 + opts.RampFrac);
    if minStep >= 2 * L
        error('tapping:driftHoldRamp:badStep', ...
              ['MinStepFrac (%g) is too large for RangeFrac (%g): no target can be ', ...
               'that far from the current interval within the bounds.'], ...
              opts.MinStepFrac, opts.RangeFrac);
    end
    spec = driftLengthSpec(baseInterval, opts.SteadyIntervals, ...
                           opts.Intervals, opts.DurationMs, 'driftHoldRamp');

    intervals = repmat(baseInterval, 1, opts.SteadyIntervals);   % isochronous lead-in
    ramps     = struct('Start', {}, 'End', {}, 'Direction', {}, 'Truncated', {});
    cur       = 0;          % current plateau, log-interval
    queue     = [];         % x values of the segment being played out
    nextHold  = true;       % segments alternate hold, ramp, hold, ...

    while driftNeedMore(intervals, spec)
        while isempty(queue)                 % build the next segment only when needed
            if nextHold
                queue = repmat(cur, 1, opts.HoldMin + geometricExtra(opts.HoldExtraMean));
            else
                tgt = cur;
                while abs(tgt - cur) < minStep
                    tgt = -L + 2 * L * rand;
                end
                nR    = ceil(abs(tgt - cur) / step);
                queue = cur + (tgt - cur) * (1:nR) / nR;
                start = numel(intervals) + 1;
                if tgt < cur, dirn = 'Faster'; else, dirn = 'Slower'; end
                ramps(end+1) = struct('Start', start, 'End', start + nR - 1, ...
                                      'Direction', dirn, 'Truncated', false); %#ok<AGROW>
                cur = tgt;
            end
            nextHold = ~nextHold;
        end
        intervals(end+1) = baseInterval * exp(queue(1));     %#ok<AGROW>
        queue(1) = [];
    end

    % a ramp the stream ended inside is recorded as truncated at the last interval
    n = numel(intervals);
    for r = 1:numel(ramps)
        if ramps(r).End > n
            ramps(r).End       = n;
            ramps(r).Truncated = true;
        end
    end

    drift = struct( ...
        'Method',          'HoldRamp', ...
        'BaseInterval',    baseInterval, ...
        'SteadyIntervals', opts.SteadyIntervals, ...
        'Params',          struct('RangeFrac',     opts.RangeFrac, ...
                                  'HoldMin',       opts.HoldMin, ...
                                  'HoldExtraMean', opts.HoldExtraMean, ...
                                  'RampFrac',      opts.RampFrac, ...
                                  'MinStepFrac',   opts.MinStepFrac), ...
        'Ramps',           ramps);
end


function g = geometricExtra(meanExtra)
% Geometric count on {0, 1, 2, ...} with the given mean (constant hazard).
    if meanExtra == 0
        g = 0;
        return
    end
    p = 1 / (meanExtra + 1);
    g = floor(log(rand) / log(1 - p));
end
