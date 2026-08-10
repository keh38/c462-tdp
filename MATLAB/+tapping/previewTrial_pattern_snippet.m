% ---------------------------------------------------------------------------
% Add to tapping.previewTrial, where it already reports each stream. This is the
% human check the validator deliberately does NOT do: whether the tiling is the
% one you meant. It surfaces the unit, and the repeat count + remainder against
% the stream length — the drift the preview exists to catch.
% ---------------------------------------------------------------------------

    describeStream(t.PacerIntervals, t.PacerPattern, 'Pacer');
    if ~isempty(t.DistractorIntervals)
        describeStream(t.DistractorIntervals, t.DistractorPattern, 'Distractor');
    else
        fprintf('  Distractor : none (pacer-only)\n');
    end

% --- local helper (place inside previewTrial, or lift to +tapping if reused) ---
function describeStream(intervals, pattern, label)
    nStream = numel(intervals);
    if isempty(pattern)
        fprintf('  %-10s : %d el, non-repeating\n', label, nStream);
        return;
    end

    nUnit = numel(pattern);
    reps  = nStream / nUnit;                       % may be fractional (drift)
    unitStr = strjoin(compose('%g', pattern), ' ');

    if reps == floor(reps)
        fprintf('  %-10s : %d el = unit[%d] x %d  (unit: %s)\n', ...
                label, nStream, nUnit, reps, unitStr);
    else
        whole = floor(reps);
        rem   = nStream - whole * nUnit;
        fprintf('  %-10s : %d el = unit[%d] x %d + %d  DRIFT  (unit: %s)\n', ...
                label, nStream, nUnit, whole, rem, unitStr);
    end
end
