function spec = driftLengthSpec(baseInterval, steadyIntervals, nIntervals, durationMs, caller)
%DRIFTLENGTHSPEC  Resolve and check the length request shared by the drift primitives.
%   spec = driftLengthSpec(baseInterval, steadyIntervals, nIntervals, durationMs, caller)
%
%   Exactly one of NINTERVALS / DURATIONMS must be given (the other empty). Both
%   measure the WHOLE stream, steady lead-in included:
%     Intervals   the stream has exactly this many intervals (= elements);
%     DurationMs  intervals are generated until their cumulative sum reaches or
%                 passes DURATIONMS (the drawToDuration convention), so the
%                 count is an output and the stream runs slightly past the target.
%   Either way the request must leave room for at least one drifting interval
%   after the steady lead-in.
%
%   Private to +tapping: the drift primitives call it unqualified.

    hasN = ~isempty(nIntervals);
    hasD = ~isempty(durationMs);
    id   = @(s) sprintf('tapping:%s:%s', caller, s);

    if hasN == hasD
        error(id('badLength'), ...
              '%s: give exactly one of Intervals or DurationMs.', caller);
    end

    if hasN
        n = nIntervals;
        if ~isnumeric(n) || ~isscalar(n) || ~isfinite(n) || n < 1 || n ~= floor(n)
            error(id('badLength'), '%s: Intervals must be a positive integer.', caller);
        end
        if n <= steadyIntervals
            error(id('badLength'), ...
                  ['%s: Intervals (%d) must exceed SteadyIntervals (%d), or the stream ', ...
                   'has no drift.'], caller, n, steadyIntervals);
        end
        spec = struct('Mode', 'count', 'Value', double(n));
    else
        d = durationMs;
        if ~isnumeric(d) || ~isscalar(d) || ~isfinite(d) || d <= 0
            error(id('badLength'), '%s: DurationMs must be a finite number > 0.', caller);
        end
        if d <= steadyIntervals * baseInterval
            error(id('badLength'), ...
                  ['%s: DurationMs (%g) must exceed the steady lead-in ', ...
                   '(%d x %g ms = %g ms), or the stream has no drift.'], ...
                  caller, d, steadyIntervals, baseInterval, steadyIntervals * baseInterval);
        end
        spec = struct('Mode', 'duration', 'Value', double(d));
    end
end
