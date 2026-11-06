function tf = driftNeedMore(intervals, spec)
%DRIFTNEEDMORE  True while a drift stream is shorter than its length request.
%   SPEC comes from driftLengthSpec. Count mode stops at exactly Value intervals;
%   duration mode stops once the cumulative sum reaches or passes Value.
    if strcmp(spec.Mode, 'count')
        tf = numel(intervals) < spec.Value;
    else
        tf = sum(intervals) < spec.Value;
    end
end
