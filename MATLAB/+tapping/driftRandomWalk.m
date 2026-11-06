function [intervals, drift] = driftRandomWalk(baseInterval, opts)
%DRIFTRANDOMWALK  Drifting stream whose interval wanders as a bounded random walk.
%   [iv, drift] = tapping.driftRandomWalk(baseInterval, Intervals=n)
%   [iv, drift] = tapping.driftRandomWalk(baseInterval, DurationMs=t)
%   [iv, drift] = tapping.driftRandomWalk(..., StepFrac=0.02, Rho=0.95, ...)
%
%   The interval changes a little on every element, unpredictably, and is pulled
%   back toward BASEINTERVAL over time. In log-interval x = log(I / baseInterval):
%
%       x(1) = 0
%       x(j) = Rho * x(j-1) + StepFrac * randn          (reflected into [-L, L])
%       I(j) = baseInterval * exp(x(j) + JitterFrac * randn)
%
%   with L = log(1 + RangeFrac), so every interval lies in
%   [baseInterval/(1+RangeFrac), baseInterval*(1+RangeFrac)]. The walk itself is
%   REFLECTED at the bounds (an unbounded AR(1) walk with the defaults leaves
%   +/-15% in most 200-interval sequences); jitter, if any, is reflected too.
%   drift.BoundHits counts the intervals on which a reflection happened.
%
%   The stream opens with SteadyIntervals intervals of exactly BASEINTERVAL (the
%   isochronous lead-in), then the walk, whose first interval is also exactly
%   BASEINTERVAL (apart from jitter), so there is no jump at the switch.
%
%   Inputs (all intervals in ms; all "Frac" values are fractions of the base
%   interval, in log-interval, e.g. 0.02 ~ 2%):
%     baseInterval     base interval, ms (> 0)
%   Length - exactly one, both counting the steady lead-in:
%     Intervals        total number of intervals (= elements) in the stream
%     DurationMs       generate until the cumulative duration reaches/passes this
%   Name-Value (default; suggested range):
%     StepFrac         SD of each step (0.02; 0.01-0.025)
%     Rho              pull back toward the base; 1 = pure random walk
%                      (0.95; 0.9-0.97). Time constant ~ 1/(1-Rho) intervals.
%     RangeFrac        hard bound, +/- (0.15; 0.10-0.15)
%     JitterFrac       independent jitter on each drift interval (0; 0-0.02)
%     SteadyIntervals  isochronous lead-in, counted in the length (8)
%
%   Outputs:
%     intervals  1 x n row vector, ms. Non-repeating: leave the pattern field empty.
%     drift      the drift record. Assign the pair together, like tilePattern:
%                  [t.PacerIntervals, t.PacerDrift] = tapping.driftRandomWalk(500, DurationMs=30000);
%                Fields: Method ("RandomWalk"), BaseInterval, SteadyIntervals,
%                Params (StepFrac, Rho, RangeFrac, JitterFrac), BoundHits.
%
%   Long-run SD of the tempo is StepFrac/sqrt(1-Rho^2) (about 6.4% with the
%   defaults). Draws use MATLAB's global rng: call rng(seed) in the generator first.
%   See also tapping.driftMultisine, tapping.driftHoldRamp.

    arguments
        baseInterval (1,1) double {mustBeFinite, mustBePositive}
        opts.Intervals = []
        opts.DurationMs = []
        opts.StepFrac (1,1) double {mustBeFinite, mustBePositive} = 0.02
        opts.Rho (1,1) double {mustBeFinite} = 0.95
        opts.RangeFrac (1,1) double {mustBeFinite, mustBePositive} = 0.15
        opts.JitterFrac (1,1) double {mustBeFinite, mustBeNonnegative} = 0
        opts.SteadyIntervals (1,1) double {mustBeInteger, mustBeNonnegative} = 8
    end

    if opts.Rho < 0 || opts.Rho > 1
        error('tapping:driftRandomWalk:badRho', 'Rho must lie in [0, 1] (got %g).', opts.Rho);
    end
    spec = driftLengthSpec(baseInterval, opts.SteadyIntervals, ...
                           opts.Intervals, opts.DurationMs, 'driftRandomWalk');

    L = log(1 + opts.RangeFrac);
    intervals = repmat(baseInterval, 1, opts.SteadyIntervals);   % isochronous lead-in

    x = 0;  nHits = 0;  first = true;
    while driftNeedMore(intervals, spec)
        hit = false;
        if ~first                                         % x(1) = 0: start at the base
            [x, hit] = driftReflect(opts.Rho * x + opts.StepFrac * randn, L);
        end
        first = false;
        y = x;
        if opts.JitterFrac > 0
            [y, jhit] = driftReflect(x + opts.JitterFrac * randn, L);
            hit = hit || jhit;
        end
        nHits = nHits + hit;
        intervals(end+1) = baseInterval * exp(y);          %#ok<AGROW>
    end

    drift = struct( ...
        'Method',          'RandomWalk', ...
        'BaseInterval',    baseInterval, ...
        'SteadyIntervals', opts.SteadyIntervals, ...
        'Params',          struct('StepFrac',   opts.StepFrac, ...
                                  'Rho',        opts.Rho, ...
                                  'RangeFrac',  opts.RangeFrac, ...
                                  'JitterFrac', opts.JitterFrac), ...
        'BoundHits',       nHits);
end
