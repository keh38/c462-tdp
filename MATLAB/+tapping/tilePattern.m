function [intervals, pattern] = tilePattern(unit, targetCount)
%TILEPATTERN Tile a pre-drawn interval unit to a target element count.
%   [intervals, pattern] = tapping.tilePattern(unit, targetCount) repeats the
%   row vector UNIT until it fills TARGETCOUNT elements, truncating the final
%   repeat if the tiling does not divide evenly. It returns:
%       intervals - the flattened, played sequence (1 x targetCount)
%       pattern   - the UNIT unchanged, i.e. the repeating unit the intervals
%                   were built from, BEFORE tiling
%
%   The point of returning both is that flattening destroys the unit: once UNIT
%   is tiled into INTERVALS, its length (and, for a drawn unit, its values) are
%   unrecoverable from INTERVALS alone. Capturing the unit here, at the tiling
%   step, is what lets the trial record its authored period. Assign both to the
%   trial as a pair so the pattern field can never be forgotten:
%
%       [t.PacerIntervals, t.PacerPattern] = tapping.tilePattern(unit, nPacer);
%
%   Non-dividing tiling is legal and intentional: a unit that does not divide
%   TARGETCOUNT drifts in phase against the stream, which is a valid design
%   choice (see the Contract). TILEPATTERN truncates to exactly TARGETCOUNT; the
%   trailing partial repeat is expected.
%
%   NON-REPEATING STREAMS DO NOT CALL THIS. A stream authored as literal
%   intervals with no repeat leaves its pattern field at the newTrial() default
%   (empty []), which is the sentinel for "does not repeat; period is the full
%   interval count". Only tilePattern ever sets a pattern field non-empty, so
%   the empty-means-non-repeating convention holds automatically: tiled -> unit
%   recorded; not tiled -> empty.
%
%   Units are milliseconds, matching every interval field.

    % --- validate the unit ---
    if isempty(unit)
        error('tapping:tilePattern:emptyUnit', ...
              'UNIT must be non-empty. A non-repeating stream should not call tilePattern; leave its pattern field empty.');
    end
    if ~isnumeric(unit) || ~isvector(unit)
        error('tapping:tilePattern:badUnit', 'UNIT must be a numeric vector.');
    end
    if any(~isfinite(unit)) || any(unit <= 0)
        error('tapping:tilePattern:badUnitValues', 'UNIT values must be finite and > 0 (ms).');
    end

    % --- validate the target ---
    if ~isscalar(targetCount) || ~isfinite(targetCount) || targetCount < 1 || targetCount ~= floor(targetCount)
        error('tapping:tilePattern:badTarget', 'TARGETCOUNT must be a positive integer.');
    end

    unit = unit(:).';                       % normalise to a row vector
    n    = numel(unit);

    reps      = ceil(targetCount / n);      % enough whole repeats to cover the target
    tiled     = repmat(unit, 1, reps);      % tile...
    intervals = tiled(1:targetCount);       % ...then truncate to exactly targetCount

    pattern = unit;                         % the authored unit, recorded verbatim
end
